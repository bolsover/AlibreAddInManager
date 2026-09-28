#Requires -Version 5.1
<#
.SYNOPSIS
    Removes the Alibre Add-On Manager itself: its registry value and its folder
    under %ProgramData%\Alibre AddOns. Add-ons it installed are left in place and
    stay registered (they keep working without the manager).
#>
param(
    # Set by the self-elevating relaunch so the new window stays open to be read.
    [switch]$Elevated
)

$ErrorActionPreference = 'Stop'

$AddOnGuid  = '{925B370F-9858-4416-A7D9-1DD8FAAC16A0}'
$FolderName = 'AlibreAddInManager'

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'Elevation required - relaunching as administrator...'
    $proc = Start-Process -FilePath 'powershell.exe' -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Elevated" -Verb RunAs -Wait -PassThru
    exit $proc.ExitCode
}

try {
    if (Get-Process -Name 'Alibre Design' -ErrorAction SilentlyContinue) {
        throw 'Alibre Design is running. Save your work, close it, and run this script again.'
    }

    $hklm = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
    $key  = $hklm.OpenSubKey('SOFTWARE\Alibre Design Add-Ons', $true)
    if ($key) {
        if ($null -ne $key.GetValue($AddOnGuid)) { $key.DeleteValue($AddOnGuid); Write-Host "Unregistered $AddOnGuid" }
        $key.Close()
    }
    $hklm.Close()

    $targetDir = Join-Path (Join-Path $env:ProgramData 'Alibre AddOns') $FolderName
    if (Test-Path -LiteralPath $targetDir) {
        Remove-Item -LiteralPath $targetDir -Recurse -Force
        Write-Host "Removed $targetDir"
    }

    Write-Host 'The add-on manager has been removed. Add-ons it installed are still registered.'
    Write-Host 'Their bookkeeping stays in %ProgramData%\Alibre AddOns\.manager\state.json for a later reinstall.'
    if ($Elevated) { Read-Host 'Press Enter to close' | Out-Null }
}
catch {
    Write-Host "Uninstall failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($Elevated) { Read-Host 'Press Enter to close' | Out-Null }
    exit 1
}
