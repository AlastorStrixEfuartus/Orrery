param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dllPath = Join-Path $projectRoot "WindowsShellExtension_Orrery\bin\$Configuration\WindowsShellExtension_Orrery.dll"
$regasm = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell prompt so RegAsm can write the shell extension registry keys.'
}

if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Build the $Configuration configuration first. Missing: $dllPath"
}

& $regasm $dllPath /codebase /tlb
Write-Host 'Registered Orrery thumbnail provider for .blp, .dds, and .ico files.'
Write-Host 'Restart Explorer or sign out/sign in if thumbnails do not appear immediately.'
pause
