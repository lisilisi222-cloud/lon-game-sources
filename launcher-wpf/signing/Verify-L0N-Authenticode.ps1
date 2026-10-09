# Fail-closed Authenticode verification for L0N Windows releases.
# Requires a publicly trusted RSA Code Signing certificate, a valid RFC3161
# timestamp, and the EXACT pre-approved publisher thumbprint.
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string[]]$Path,

  [Parameter(Mandatory = $true)]
  [ValidatePattern('^[A-Fa-f0-9]{40}$')]
  [string]$SignerThumbprint,

  [Parameter(Mandatory = $true)]
  [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
  [string]$SignToolPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$expected = $SignerThumbprint.ToUpperInvariant()

foreach ($item in $Path) {
  $file = (Resolve-Path -LiteralPath $item -ErrorAction Stop).Path
  if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
    throw "No signed file found at $item"
  }

  # /tw emits only a warning for missing timestamps. It is NOT sufficient.
  & $SignToolPath verify /pa /all /v /tw $file
  if ($LASTEXITCODE -ne 0) {
    throw "Windows Authenticode trust verification failed: $file"
  }

  $signature = Get-AuthenticodeSignature -LiteralPath $file
  if ($signature.Status -ne 'Valid' -or -not $signature.SignerCertificate) {
    throw "Missing or invalid trusted Authenticode signature on $file (status: $($signature.Status))"
  }

  $signer = $signature.SignerCertificate
  if ($signer.Thumbprint.ToUpperInvariant() -ne $expected) {
    throw "Wrong publisher certificate on $file. Expected the approved certificate thumbprint."
  }
  if ($signer.PublicKey.Oid.Value -ne '1.2.840.113549.1.1.1') {
    throw "Smart App Control requires an RSA publisher certificate: $file"
  }
  $eku = @($signer.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq '1.3.6.1.5.5.7.3.3' })
  if ($eku.Count -eq 0) {
    throw "Publisher certificate does not have Code Signing EKU: $file"
  }
  if (-not $signature.TimeStamperCertificate) {
    throw "Trusted timestamp is absent: $file"
  }
  Write-Output ("VERIFIED: " + $file)
}
