$ErrorActionPreference = 'Stop'

$packageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$dllPath = Join-Path $packageRoot 'bin\WindowsShellExtension_Orrery.dll'
$regAsm = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    throw 'Run this script from an elevated PowerShell prompt, or use Uninstall-OrreryThumbnails.cmd.'
}

if (-not (Test-Path -LiteralPath $regAsm)) {
    throw "RegAsm was not found: $regAsm. Install/enable Microsoft .NET Framework 4.x."
}

if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Missing shell extension DLL: $dllPath"
}

Write-Host 'Unregistering Orrery Explorer thumbnail provider...'
& $regAsm $dllPath /unregister | Out-Host

Write-Host ''
Write-Host 'Unregistered Orrery thumbnail provider.'
Write-Host 'If Explorer still shows cached thumbnails, restart Explorer, sign out/in, or clear the thumbnail cache.'

