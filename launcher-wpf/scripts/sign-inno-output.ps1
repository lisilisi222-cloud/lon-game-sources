# Invoked only by the protected signed-release workflow, once for each Inno Setup output.
# SignTool uses a temporary CurrentUser certificate store entry on an ephemeral Windows runner.
param(
  [Parameter(Mandatory = $true, Position = 0)]
  [ValidateNotNullOrEmpty()]
  [string] $File
)

$ErrorActionPreference = 'Stop'
$signTool = $env:L0N_SIGNTOOL_PATH
$thumb = $env:L0N_SIGN_THUMBPRINT

if (-not $signTool -or -not (Test-Path -LiteralPath $signTool)) {
  throw 'The Windows SDK SignTool executable is unavailable.'
}
if ($thumb -notmatch '^[0-9A-Fa-f]{40}$') {
  throw 'Signing certificate thumbprint is missing or invalid.'
}
if (-not (Test-Path -LiteralPath $File -PathType Leaf)) {
  throw 'Inno Setup did not supply an existing file to sign.'
}

& $signTool sign /s My /sha1 $thumb /fd SHA256 /tr 'http://timestamp.digicert.com' /td SHA256 $File
if ($LASTEXITCODE -ne 0) {
  throw "Inno Setup output signing failed (exit code $LASTEXITCODE)."
}

& $signTool verify /pa /all /tw $File
if ($LASTEXITCODE -ne 0) {
  throw "Inno Setup output signature verification failed (exit code $LASTEXITCODE)."
}
