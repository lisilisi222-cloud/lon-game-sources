# Authenticode signing for the L0N Windows build.
# Only runs on CI, never inside the installed launcher.
# The PFX file/password must be trusted publisher credentials stored as GitHub Actions secrets.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Sign", "Verify")]
    [string]$Operation,

    [Parameter(Mandatory = $true)]
    [string]$Target
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$resolvedFile = (Resolve-Path -LiteralPath $Target -ErrorAction Stop).Path
if (-not [System.IO.File]::Exists($resolvedFile)) {
    throw "File is missing: $Target"
}
if ([System.IO.Path]::GetExtension($resolvedFile).ToLowerInvariant() -ne ".exe") {
    throw "Only executable files may be signed by this script"
}

# Do not assume the exact Windows SDK version.
$programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
$sdkRoot = Join-Path $programFilesX86 "Windows Kits\10\bin"
if (-not (Test-Path -LiteralPath $sdkRoot)) {
    throw "Windows SDK signing tools directory is unavailable"
}
$signtool = Get-ChildItem -Path $sdkRoot -Recurse -Filter signtool.exe -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if ($null -eq $signtool) {
    throw "x64 SignTool.exe was not found in the installed Windows SDK"
}
$tool = $signtool.FullName

function Assert-SignedExecutable {
    param([string]$File)
    & $tool verify /pa /all /v $File
    if ($LASTEXITCODE -ne 0) {
        throw "SignTool did not validate the certificate chain/signature for $File"
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $File
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Authenticode status is $($signature.Status); refusing to publish as SIGNED"
    }
    if ($null -eq $signature.SignerCertificate) {
        throw "The executable has no signing certificate"
    }

    # Optional safeguard against an unexpected replacement certificate.
    if (-not [string]::IsNullOrWhiteSpace($env:L0N_EXPECTED_SIGNER_SUBJECT)) {
        $subject = $signature.SignerCertificate.Subject
        if (-not $subject.Contains($env:L0N_EXPECTED_SIGNER_SUBJECT.Trim())) {
            throw "Signer identity does not match expected publisher"
        }
    }
    Write-Output ("PASS Authenticode: " + [System.IO.Path]::GetFileName($File))
    Write-Output ("Publisher: " + $signature.SignerCertificate.Subject)
    Write-Output ("Certificate thumbprint: " + $signature.SignerCertificate.Thumbprint)
}

if ($Operation -eq "Verify") {
    Assert-SignedExecutable $resolvedFile
    exit 0
}

if ([string]::IsNullOrWhiteSpace($env:L0N_CERT_PFX_BASE64) -or
    [string]::IsNullOrWhiteSpace($env:L0N_CERT_PASSWORD)) {
    throw "Signing is not configured: both GitHub certificate secrets are required"
}

$pfxTempPath = Join-Path $env:RUNNER_TEMP ("l0n-sign-" + [Guid]::NewGuid().ToString("N") + ".pfx")
[byte[]]$decoded = @()
try {
    try {
        $decoded = [System.Convert]::FromBase64String($env:L0N_CERT_PFX_BASE64.Trim())
    }
    catch {
        throw "GitHub secret L0N_CERT_PFX_BASE64 is not valid base64"
    }
    if ($decoded.Length -lt 100) {
        throw "The supplied certificate file is unexpectedly small"
    }
    [System.IO.File]::WriteAllBytes($pfxTempPath, $decoded)

    # RFC3161 timestamp persists Authenticode validity past certificate expiry.
    # Never write the PFX file or password into build artifacts.
    & $tool sign /f $pfxTempPath /p $env:L0N_CERT_PASSWORD /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 $resolvedFile
    if ($LASTEXITCODE -ne 0) {
        throw "SignTool sign failed; inspect certificate validity and timestamp service"
    }
}
finally {
    if (Test-Path -LiteralPath $pfxTempPath) {
        Remove-Item -LiteralPath $pfxTempPath -Force
    }
    if ($decoded.Length -gt 0) {
        [System.Array]::Clear($decoded, 0, $decoded.Length)
    }
}

Assert-SignedExecutable $resolvedFile
