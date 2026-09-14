[CmdletBinding()]
param(
  [ValidateSet('BootstrapToken','IssueStaging','IssueProduction','ShowPaths','SelfTest')]
  [string]$Action = 'ShowPaths',

  [ValidateSet('BETA','STABLE')]
  [string]$TargetEnvironment = 'BETA',

  [string]$AccountId = '1b1695e4f2a3abfe08dc475b352c7f42',

  [string]$ZoneId = '880b887071f771f5be3caf5726d5df11',

  [string]$AcmeEmail,

  [string]$CertificateDataRoot,

  [switch]$Force,

  [switch]$ConfirmProduction
)

$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
  throw 'VHDCHY LAN certificate host manager requires Windows.'
}

function Write-AtomicText {
  param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Value
  )

  $directory = Split-Path -Parent $Path
  New-Item -ItemType Directory -Force -Path $directory | Out-Null
  $temporary = "$Path.tmp.$([Guid]::NewGuid().ToString('N'))"
  try {
    Set-Content -LiteralPath $temporary -Value $Value -NoNewline -Encoding utf8
    Move-Item -LiteralPath $temporary -Destination $Path -Force
  }
  finally {
    Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
  }
}

function Save-DnsToken {
  param(
    [Parameter(Mandatory=$true)][System.Security.SecureString]$Token,
    [Parameter(Mandatory=$true)][string]$Path
  )

  # On Windows, ConvertFrom-SecureString without -Key uses DPAPI CurrentUser.
  $protected = ConvertFrom-SecureString -SecureString $Token
  if ([string]::IsNullOrWhiteSpace($protected)) {
    throw 'Failed to protect DNS token for the current Windows user.'
  }
  Write-AtomicText -Path $Path -Value $protected
}

function Load-DnsTokenSecure {
  param([Parameter(Mandatory=$true)][string]$Path)

  if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Protected Cloudflare DNS token not found: $Path"
  }
  $protected = (Get-Content -LiteralPath $Path -Raw).Trim()
  if ([string]::IsNullOrWhiteSpace($protected)) {
    throw 'Protected Cloudflare DNS token file is empty.'
  }
  try {
    return ConvertTo-SecureString -String $protected
  }
  catch {
    throw 'Protected Cloudflare DNS token cannot be decrypted by the current Windows user.'
  }
}

function ConvertTo-PlainTextTransient {
  param([Parameter(Mandatory=$true)][System.Security.SecureString]$SecureValue)

  $pointer = [IntPtr]::Zero
  try {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
  }
  finally {
    if ($pointer -ne [IntPtr]::Zero) {
      [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
  }
}

$environmentName = $TargetEnvironment.ToUpperInvariant()
if ([string]::IsNullOrWhiteSpace($CertificateDataRoot)) {
  $CertificateDataRoot = Join-Path $env:LOCALAPPDATA "VHDCHY\LanCertificate\$environmentName"
}
$CertificateDataRoot = [System.IO.Path]::GetFullPath($CertificateDataRoot)
New-Item -ItemType Directory -Force -Path $CertificateDataRoot | Out-Null

$tokenPath = Join-Path $CertificateDataRoot 'cloudflare-dns-token.current-user.dpapi'
$protectedPfxPath = Join-Path $CertificateDataRoot 'lan-tls.pfx.dpapi'
$canonicalHost = if ($environmentName -eq 'BETA') { 'lan-beta.supra.cc.cd' } else { 'lan.supra.cc.cd' }

if ($Action -eq 'ShowPaths') {
  Write-Host "environment=$environmentName"
  Write-Host "canonicalHost=$canonicalHost"
  Write-Host "certificateDataRoot=$CertificateDataRoot"
  Write-Host "protectedDnsToken=$tokenPath"
  Write-Host "protectedPfx=$protectedPfxPath"
  exit 0
}

if ($Action -eq 'SelfTest') {
  $testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "vhdchy-lan-cert-host-$([Guid]::NewGuid().ToString('N'))"
  $testTokenPath = Join-Path $testRoot 'token.dpapi'
  $plainProbe = 'vhdchy-self-test-token-not-a-secret'
  $secureProbe = ConvertTo-SecureString -String $plainProbe -AsPlainText -Force
  try {
    Save-DnsToken -Token $secureProbe -Path $testTokenPath
    $disk = Get-Content -LiteralPath $testTokenPath -Raw
    if ($disk.Contains($plainProbe)) { throw 'DPAPI token store leaked plaintext.' }
    $roundTripSecure = Load-DnsTokenSecure -Path $testTokenPath
    try {
      $roundTrip = ConvertTo-PlainTextTransient -SecureValue $roundTripSecure
      if ($roundTrip -ne $plainProbe) { throw 'DPAPI token round-trip mismatch.' }
    }
    finally {
      $roundTrip = $null
      $roundTripSecure.Dispose()
    }
    Write-Host 'LAN_CERT_HOST_SELF_TEST_PASS'
    Write-Host 'dnsTokenDpapiCurrentUser=PASS'
    Write-Host 'dnsTokenPlaintextDiskLeak=PASS'
  }
  finally {
    $secureProbe.Dispose()
    Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
  }
  exit 0
}

if ($Action -eq 'BootstrapToken') {
  Write-Host 'Enter the dedicated Cloudflare DNS token for this Windows user.'
  Write-Host 'The value is not echoed and will be stored using Windows DPAPI CurrentUser.'
  $secureToken = Read-Host 'Cloudflare DNS token' -AsSecureString
  try {
    if ($secureToken.Length -lt 16) {
      throw 'DNS token input is too short.'
    }
    Save-DnsToken -Token $secureToken -Path $tokenPath
    Write-Host 'DNS_TOKEN_STORED_LOCAL_CURRENT_USER=YES'
    Write-Host 'DNS_TOKEN_PROVIDER_PERMISSION_VERIFIED=NO'
    Write-Host 'Run IssueStaging next; the certificate manager will live-verify the exact account/zone before any ACME DNS mutation.'
  }
  finally {
    $secureToken.Dispose()
  }
  exit 0
}

if ($Action -eq 'IssueProduction' -and -not $ConfirmProduction) {
  throw 'IssueProduction requires explicit -ConfirmProduction.'
}

if ([string]::IsNullOrWhiteSpace($AcmeEmail)) {
  $AcmeEmail = $env:VHDCHY_ACME_EMAIL
}
if ([string]::IsNullOrWhiteSpace($AcmeEmail) -or -not $AcmeEmail.Contains('@')) {
  throw 'AcmeEmail is required for certificate issuance.'
}

if ($AccountId -notmatch '^[a-fA-F0-9]{32}$') { throw 'AccountId must be a 32-character hexadecimal Cloudflare account ID.' }
if ($ZoneId -notmatch '^[a-fA-F0-9]{32}$') { throw 'ZoneId must be a 32-character hexadecimal Cloudflare zone ID.' }

$packageRoot = Split-Path -Parent $PSScriptRoot
$managerExe = Join-Path $packageRoot 'certificate\VHDCHY.LanCertificateManager.exe'
if (-not (Test-Path -LiteralPath $managerExe -PathType Leaf)) {
  throw "LAN certificate manager executable not found: $managerExe"
}

$secureDnsToken = Load-DnsTokenSecure -Path $tokenPath
$dnsToken = $null
$previous = @{
  VHDCHY_ENV = $env:VHDCHY_ENV
  VHDCHY_CLOUDFLARE_ACCOUNT_ID = $env:VHDCHY_CLOUDFLARE_ACCOUNT_ID
  VHDCHY_CLOUDFLARE_ZONE_ID = $env:VHDCHY_CLOUDFLARE_ZONE_ID
  VHDCHY_CLOUDFLARE_DNS_TOKEN = $env:VHDCHY_CLOUDFLARE_DNS_TOKEN
  VHDCHY_ACME_EMAIL = $env:VHDCHY_ACME_EMAIL
  VHDCHY_LAN_CERT_DATA_ROOT = $env:VHDCHY_LAN_CERT_DATA_ROOT
  VHDCHY_ACME_ALLOW_PRODUCTION = $env:VHDCHY_ACME_ALLOW_PRODUCTION
}

try {
  $dnsToken = ConvertTo-PlainTextTransient -SecureValue $secureDnsToken
  if ([string]::IsNullOrWhiteSpace($dnsToken)) { throw 'Decrypted DNS token is empty.' }

  $env:VHDCHY_ENV = $environmentName
  $env:VHDCHY_CLOUDFLARE_ACCOUNT_ID = $AccountId
  $env:VHDCHY_CLOUDFLARE_ZONE_ID = $ZoneId
  $env:VHDCHY_CLOUDFLARE_DNS_TOKEN = $dnsToken
  $env:VHDCHY_ACME_EMAIL = $AcmeEmail.Trim()
  $env:VHDCHY_LAN_CERT_DATA_ROOT = $CertificateDataRoot

  $managerArgs = @()
  if ($Force) { $managerArgs += '--force' }
  if ($Action -eq 'IssueProduction') {
    $env:VHDCHY_ACME_ALLOW_PRODUCTION = 'YES'
    $managerArgs += '--production'
    Write-Host "VHDCHY LAN certificate: PRODUCTION issuance requested for $canonicalHost"
  }
  else {
    Remove-Item Env:VHDCHY_ACME_ALLOW_PRODUCTION -ErrorAction SilentlyContinue
    Write-Host "VHDCHY LAN certificate: STAGING issuance requested for $canonicalHost"
  }

  & $managerExe @managerArgs
  $exitCode = $LASTEXITCODE
  if ($exitCode -ne 0) {
    throw "LAN certificate manager failed with exit code $exitCode."
  }
  if (-not (Test-Path -LiteralPath $protectedPfxPath -PathType Leaf)) {
    throw 'Certificate manager completed without producing the protected PFX.'
  }

  Write-Host 'LAN_CERTIFICATE_HOST_ACTION_PASS'
  Write-Host "protectedPfx=$protectedPfxPath"
  Write-Host 'Restart LAN Service to load the updated certificate.'
  exit 0
}
finally {
  $dnsToken = $null
  $secureDnsToken.Dispose()
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
