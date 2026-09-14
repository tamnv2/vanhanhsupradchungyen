[CmdletBinding()]
param(
  [ValidateSet('BETA','STABLE')]
  [string]$TargetEnvironment = 'BETA',

  [ValidateRange(1025,65535)]
  [int]$Port = 17901,

  [string]$ClusterId = 'PICK_PACK_1291',

  [string]$ServiceDataRoot,

  [string]$CertificateDataRoot
)

$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
  throw 'VHDCHY LAN host launcher requires Windows.'
}

$packageRoot = Split-Path -Parent $PSScriptRoot
$serviceExe = Join-Path $packageRoot 'service\VHDCHY.LanService.exe'
if (-not (Test-Path -LiteralPath $serviceExe -PathType Leaf)) {
  throw "LAN Service executable not found: $serviceExe"
}

$environmentName = $TargetEnvironment.ToUpperInvariant()
if ([string]::IsNullOrWhiteSpace($ServiceDataRoot)) {
  $ServiceDataRoot = Join-Path $env:LOCALAPPDATA "VHDCHY\LanService\$environmentName"
}
if ([string]::IsNullOrWhiteSpace($CertificateDataRoot)) {
  $CertificateDataRoot = Join-Path $env:LOCALAPPDATA "VHDCHY\LanCertificate\$environmentName"
}

$ServiceDataRoot = [System.IO.Path]::GetFullPath($ServiceDataRoot)
$CertificateDataRoot = [System.IO.Path]::GetFullPath($CertificateDataRoot)
$protectedPfx = Join-Path $CertificateDataRoot 'lan-tls.pfx.dpapi'

New-Item -ItemType Directory -Force -Path $ServiceDataRoot | Out-Null
New-Item -ItemType Directory -Force -Path $CertificateDataRoot | Out-Null

$previous = @{
  VHDCHY_ENV = $env:VHDCHY_ENV
  VHDCHY_CLUSTER_ID = $env:VHDCHY_CLUSTER_ID
  VHDCHY_LAN_PORT = $env:VHDCHY_LAN_PORT
  VHDCHY_LAN_DATA_ROOT = $env:VHDCHY_LAN_DATA_ROOT
  VHDCHY_LAN_TLS_PROTECTED_PFX_PATH = $env:VHDCHY_LAN_TLS_PROTECTED_PFX_PATH
  VHDCHY_LAN_TLS_PFX_PATH = $env:VHDCHY_LAN_TLS_PFX_PATH
  VHDCHY_LAN_TLS_PFX_PASSWORD = $env:VHDCHY_LAN_TLS_PFX_PASSWORD
}

try {
  $env:VHDCHY_ENV = $environmentName
  $env:VHDCHY_CLUSTER_ID = $ClusterId
  $env:VHDCHY_LAN_PORT = [string]$Port
  $env:VHDCHY_LAN_DATA_ROOT = $ServiceDataRoot

  # Protected DPAPI PFX is the reviewed Windows production path. Plain-PFX
  # compatibility variables are removed so a stale developer setting cannot win.
  Remove-Item Env:VHDCHY_LAN_TLS_PFX_PATH -ErrorAction SilentlyContinue
  Remove-Item Env:VHDCHY_LAN_TLS_PFX_PASSWORD -ErrorAction SilentlyContinue

  if (Test-Path -LiteralPath $protectedPfx -PathType Leaf) {
    $env:VHDCHY_LAN_TLS_PROTECTED_PFX_PATH = $protectedPfx
    Write-Host "VHDCHY LAN: HTTPS certificate source = Windows DPAPI CurrentUser"
  }
  else {
    Remove-Item Env:VHDCHY_LAN_TLS_PROTECTED_PFX_PATH -ErrorAction SilentlyContinue
    Write-Host "VHDCHY LAN: no protected certificate found; runtime will remain HTTP_READ_ONLY"
  }

  Write-Host "VHDCHY LAN: environment=$environmentName port=$Port"
  Write-Host "VHDCHY LAN: data=$ServiceDataRoot"
  & $serviceExe
  exit $LASTEXITCODE
}
finally {
  foreach ($name in $previous.Keys) {
    $value = $previous[$name]
    if ($null -eq $value) {
      Remove-Item "Env:$name" -ErrorAction SilentlyContinue
    }
    else {
      Set-Item "Env:$name" $value
    }
  }
}
