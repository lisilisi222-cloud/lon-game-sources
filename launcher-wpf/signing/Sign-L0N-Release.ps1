# L0N PERSONAL code signing - Windows developer tool
# Requires an independently issued, publicly trusted Code Signing certificate.
# Certificate private key stays on your token or cloud signing provider.
# Neither the key nor the PIN is uploaded to this GitHub repository.
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [ValidatePattern('^[A-Fa-f0-9]{40}$')]
  [string]$CertificateThumbprint,
  [ValidateSet('CurrentUser','LocalMachine')]
  [string]$CertificateStore='CurrentUser',
  [string]$TimestampServer='http://timestamp.digicert.com'
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

function Find-Exe([string]$name,[string[]]$paths){
  $tool=Get-Command $name -ErrorAction SilentlyContinue
  if($tool){return $tool.Source}
  foreach($path in $paths){if($path -and (Test-Path -LiteralPath $path -PathType Leaf)){return $path}}
  throw "Required tool $name was not found. Install the required Windows developer tool first."
}
function Check-Exit([string]$stage){
  if($LASTEXITCODE -ne 0){throw "$stage failed with exit code $LASTEXITCODE. Signed release was not produced."}
}
if([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT){throw 'Use Windows to sign L0N.'}
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publish=Join-Path $root 'publish'
$app=Join-Path $publish 'L0N.GameLauncher.exe'
$setup=Join-Path $root 'dist\L0N_Game_Launcher_Setup.exe'
$thumb=$CertificateThumbprint.ToUpperInvariant()
$cert=Get-Item -LiteralPath ("Cert:\"+$CertificateStore+"\My\"+$thumb) -ErrorAction Stop
if(-not $cert.HasPrivateKey){throw 'The signing private key is not accessible. Connect your token or cloud provider.'}
if($cert.Subject -eq $cert.Issuer){throw 'Self-signed certificates cannot establish Smart App Control public trust.'}
if([DateTime]::UtcNow -lt $cert.NotBefore.ToUniversalTime() -or [DateTime]::UtcNow -gt $cert.NotAfter.ToUniversalTime()){
  throw 'The certificate is outside its validity dates.'
}
$eku=@($cert.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq '1.3.6.1.5.5.7.3.3' })
if($eku.Count -eq 0){throw 'A Code Signing certificate (EKU 1.3.6.1.5.5.7.3.3) is required.'}
$chain=[Security.Cryptography.X509Certificates.X509Chain]::new()
try{
  $chain.ChainPolicy.RevocationMode=[Security.Cryptography.X509Certificates.X509RevocationMode]::Online
  if(-not $chain.Build($cert)){
    $reasons=($chain.ChainStatus | ForEach-Object {$_.StatusInformation.Trim()}) -join '; '
    throw "Certificate chain validation failed: $reasons"
  }
}finally{$chain.Dispose()}

$programFilesX86=[Environment]::GetFolderPath('ProgramFilesX86')
$sdkRoot=Join-Path $programFilesX86 'Windows Kits\10\bin'
$candidates=@()
if(Test-Path -LiteralPath $sdkRoot){
  $candidates=@(Get-ChildItem -Path $sdkRoot -Filter signtool.exe -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object {$_.DirectoryName -match '\\x64$'} | Sort-Object FullName -Descending |
    Select-Object -ExpandProperty FullName)
}
$dotnet=Find-Exe 'dotnet' @()
$signtool=Find-Exe 'signtool.exe' $candidates
$iscc=Find-Exe 'ISCC.exe' @(
  (Join-Path $programFilesX86 'Inno Setup 6\ISCC.exe'),
  (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
)

Write-Host 'Building native L0N Windows x64 application...'
$buildArgs=@('publish',(Join-Path $root 'L0N.Launcher\L0N.Launcher.csproj'),'--configuration','Release','--runtime','win-x64','--self-contained','true','-p:PublishSingleFile=true','-p:IncludeNativeLibrariesForSelfExtract=true','-p:DebugType=None','--output',$publish)
& $dotnet @buildArgs
Check-Exit 'dotnet publish'
if(-not (Test-Path -LiteralPath $app -PathType Leaf)){throw "Missing published executable: $app"}

function Sign-Verified([string]$file){
  if(-not (Test-Path -LiteralPath $file -PathType Leaf)){throw "Missing file: $file"}
  $argsSign=@('sign','/sha1',$thumb,'/s','My')
  if($CertificateStore -eq 'LocalMachine'){$argsSign+=@('/sm')}
  $argsSign+=@('/fd','SHA256','/tr',$TimestampServer,'/td','SHA256','/v',$file)
  & $signtool @argsSign
  Check-Exit "Signing $file"
  & $signtool verify /pa /all /v $file
  Check-Exit "Verifying $file"
  $verified=Get-AuthenticodeSignature -LiteralPath $file
  if($verified.Status -ne 'Valid' -or -not $verified.SignerCertificate){
    throw "Authenticode invalid on $file. Status: $($verified.Status)"
  }
  if($verified.SignerCertificate.Thumbprint.ToUpperInvariant() -ne $thumb){
    throw "Unexpected certificate signed $file"
  }
  if(-not $verified.TimeStamperCertificate){throw "Missing trusted timestamp on $file"}
  Write-Host "VERIFIED: $file"
}

# Correct order: sign app -> package in Inno Setup -> sign installer.
Sign-Verified $app
& $iscc (Join-Path $root 'installer\L0N.iss')
Check-Exit 'Inno Setup'
Sign-Verified $setup

Write-Host 'SUCCESS: L0N app and setup are signed and verified with SHA256 and timestamp.'
Get-FileHash -Algorithm SHA256 -Path @($app,$setup) | Format-Table Path,Hash -AutoSize
Write-Host 'Publisher trust and employer IT execution policies are separate security checks.'
