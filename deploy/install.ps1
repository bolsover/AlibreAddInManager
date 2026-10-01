#Requires -Version 5.1
<#
.SYNOPSIS
    Bootstraps the Alibre Add-On Manager: copies the built add-on to
    %ProgramData%\Alibre AddOns\AlibreAddOnManager and registers it.
.DESCRIPTION
    Needed once — after that the manager updates itself from the catalog or a
    package. Re-launches itself elevated if necessary. Close Alibre Design first:
    a loaded add-on's DLL is locked.
.PARAMETER Configuration
    Build configuration to deploy (Release or Debug). Default: Release.
#>
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    # Set by the self-elevating relaunch so the new window stays open to be read.
    [switch]$Elevated
)

$ErrorActionPreference = 'Stop'

$AddOnGuid  = '{925B370F-9858-4416-A7D9-1DD8FAAC16A0}'
$FolderName = 'AlibreAddOnManager'
$Files      = @(
    'AlibreAddOnManager.dll',
    'AlibreAddOnManager.adc',
    'AlibreAddOnManager.ico',
    'addon.json',
    'AlibreAddOnManager.Helper.exe',
    'AlibreAddOnManager.Helper.exe.config'
)

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'Elevation required - relaunching as administrator...'
    $argList = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Configuration $Configuration -Elevated"
    $proc = Start-Process -FilePath 'powershell.exe' -ArgumentList $argList -Verb RunAs -Wait -PassThru
    exit $proc.ExitCode
}

try {
    if (Get-Process -Name 'Alibre Design' -ErrorAction SilentlyContinue) {
        throw 'Alibre Design is running. Save your work, close it, and run this script again.'
    }

    $sourceDir = Join-Path $PSScriptRoot "..\src\AlibreAddOnManager\bin\$Configuration"
    $sourceDir = [System.IO.Path]::GetFullPath($sourceDir)
    foreach ($f in $Files) {
        if (-not (Test-Path -LiteralPath (Join-Path $sourceDir $f))) {
            throw "Missing $f in $sourceDir - build the solution ($Configuration) first."
        }
    }

    $root      = Join-Path $env:ProgramData 'Alibre AddOns'
    $targetDir = Join-Path $root $FolderName
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    foreach ($f in $Files) {
        Copy-Item -LiteralPath (Join-Path $sourceDir $f) -Destination $targetDir -Force
    }
    $pdb = Join-Path $sourceDir 'AlibreAddOnManager.pdb'
    if ($Configuration -eq 'Debug' -and (Test-Path -LiteralPath $pdb)) { Copy-Item -LiteralPath $pdb -Destination $targetDir -Force }

    # .NET registry API, explicit 64-bit view. Not New-Item -Force: that opens
    # HKLM\SOFTWARE itself for write and fails even elevated.
    $hklm = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
    $key  = $hklm.CreateSubKey('SOFTWARE\Alibre Design Add-Ons')
    $key.SetValue($AddOnGuid, $targetDir, [Microsoft.Win32.RegistryValueKind]::String)
    $key.Close(); $hklm.Close()

    Write-Host "Installed to $targetDir"
    Write-Host "Registered $AddOnGuid"
    Write-Host 'Start Alibre Design; the manager is under the Add-Ons ribbon: Add-On Manager > Manage Add-Ons.'
    if ($Elevated) { Read-Host 'Press Enter to close' | Out-Null }
}
catch {
    Write-Host "Install failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($Elevated) { Read-Host 'Press Enter to close' | Out-Null }
    exit 1
}
