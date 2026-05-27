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
    throw 'Run this script from an elevated PowerShell prompt, or use Install-OrreryThumbnails.cmd.'
}

if (-not [Environment]::Is64BitOperatingSystem) {
    throw 'Orrery thumbnails require 64-bit Windows Explorer.'
}

if (-not (Test-Path -LiteralPath $regAsm)) {
    throw "RegAsm was not found: $regAsm. Install/enable Microsoft .NET Framework 4.x."
}

if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Missing shell extension DLL: $dllPath"
}

Write-Host 'Registering Orrery Explorer thumbnail provider...'
& $regAsm $dllPath /unregister | Out-Host
& $regAsm $dllPath /codebase /tlb | Out-Host

Write-Host ''
Write-Host 'Registered thumbnail handlers for .blp, .dds, and .ico files.'
Write-Host 'If Explorer still shows old thumbnails, restart Explorer, sign out/in, or clear the thumbnail cache.'

