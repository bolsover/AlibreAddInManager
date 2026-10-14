#Requires -Version 5.1
<#
.SYNOPSIS
    Installs the Custom Property Explorer add-on: copies the build output to
    %ProgramData%\Alibre AddOns\CustomPropertyExplorer and registers it.
.DESCRIPTION
    Re-launches itself elevated if necessary. Close Alibre Design first: a
    loaded add-on's DLL is locked. Alibre reads add-ons only at startup.
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

$AddOnGuid  = '{8D6B7599-BBA9-4DC4-9A36-97D568C0A90E}'
$FolderName = 'CustomPropertyExplorer'
$Files      = @(
    'CustomPropertyExplorer.dll',
    'CustomPropertyExplorer.adc',
    'CustomPropertyExplorer.ico',
    'addon.json'
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

    $sourceDir = Join-Path $PSScriptRoot "..\bin\$Configuration"
    $sourceDir = [System.IO.Path]::GetFullPath($sourceDir)
    foreach ($f in $Files) {
        if (-not (Test-Path -LiteralPath (Join-Path $sourceDir $f))) {
            throw "Missing $f in $sourceDir - build the project ($Configuration) first."
        }
    }

    $root      = Join-Path $env:ProgramData 'Alibre AddOns'
    $targetDir = Join-Path $root $FolderName
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    foreach ($f in $Files) {
        Copy-Item -LiteralPath (Join-Path $sourceDir $f) -Destination $targetDir -Force
    }
    $pdb = Join-Path $sourceDir 'CustomPropertyExplorer.pdb'
    if ($Configuration -eq 'Debug' -and (Test-Path -LiteralPath $pdb)) { Copy-Item -LiteralPath $pdb -Destination $targetDir -Force }

    # .NET registry API, explicit 64-bit view. Not New-Item -Force: that opens
    # HKLM\SOFTWARE itself for write and fails even elevated.
    $hklm = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
    $key  = $hklm.CreateSubKey('SOFTWARE\Alibre Design Add-Ons')
    $key.SetValue($AddOnGuid, $targetDir, [Microsoft.Win32.RegistryValueKind]::String)
    $key.Close(); $hklm.Close()

    Write-Host "Installed to $targetDir"
    Write-Host "Registered $AddOnGuid"
    Write-Host 'Start Alibre Design and open a part or assembly; the commands are under the Add-Ons ribbon: Property Explorer > Property Explorer / Custom Property Explorer.'
    if ($Elevated) { Read-Host 'Press Enter to close' | Out-Null }
}
catch {
    Write-Host "Install failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($Elevated) { Read-Host 'Press Enter to close' | Out-Null }
    exit 1
}
