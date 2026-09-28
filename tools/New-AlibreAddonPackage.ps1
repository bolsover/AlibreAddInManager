#Requires -Version 5.1
<#
.SYNOPSIS
    Packs an add-on folder into a .alibreaddon package and prints the catalog
    "versions" entry (sha256, size) to paste into catalog/addons/{GUID}.json.
.DESCRIPTION
    The folder must contain addon.json and the add-on's .adc, DLL and icon — the
    package root becomes the deploy folder. *.pdb files are left out unless
    -IncludePdb is given.
.EXAMPLE
    .\New-AlibreAddonPackage.ps1 -SourceDir ..\AlibreAddInManager\bin\Release -OutDir ..\catalog-sample\packages
#>
param(
    [Parameter(Mandatory)] [string]$SourceDir,
    [Parameter(Mandatory)] [string]$OutDir,
    [string]$PackageUrl,
    [switch]$IncludePdb
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$SourceDir = [System.IO.Path]::GetFullPath($SourceDir)
$manifestPath = Join-Path $SourceDir 'addon.json'
if (-not (Test-Path -LiteralPath $manifestPath)) { throw "No addon.json in $SourceDir" }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

$adc = Get-ChildItem -LiteralPath $SourceDir -Filter *.adc | Where-Object {
    ([xml](Get-Content -LiteralPath $_.FullName -Raw)).AlibreDesignAddOn.Property |
        Where-Object { $_.name -eq 'Identifier' -and $_.value -eq $manifest.id }
} | Select-Object -First 1
if (-not $adc) { throw "No .adc in $SourceDir has Identifier $($manifest.id)" }

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$OutDir = [System.IO.Path]::GetFullPath($OutDir)
$packageName = "$($manifest.folderName)-$($manifest.version).alibreaddon"
$packagePath = Join-Path $OutDir $packageName
if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }

$zip = [System.IO.Compression.ZipFile]::Open($packagePath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -LiteralPath $SourceDir -Recurse -File |
        Where-Object { $IncludePdb -or $_.Extension -ne '.pdb' } |
        Where-Object { $_.Extension -ne '.alibreaddon' } |
        ForEach-Object {
            $relative = $_.FullName.Substring($SourceDir.TrimEnd('\').Length + 1).Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $relative, [System.IO.Compression.CompressionLevel]::Optimal)
        }
}
finally {
    $zip.Dispose()
}

$sha = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
$size = (Get-Item -LiteralPath $packagePath).Length
if (-not $PackageUrl) { $PackageUrl = "packages/$packageName" }

$entry = [ordered]@{
    version        = $manifest.version
    released       = (Get-Date -Format 'yyyy-MM-dd')
    kind           = 'integrated'
    dllType        = $manifest.dllType
    arch           = $manifest.arch
    minAlibreBuild = $manifest.minAlibreBuild
    maxAlibreBuild = $manifest.maxAlibreBuild
    packageUrl     = $PackageUrl
    sha256         = $sha
    size           = $size
}

Write-Host "Package: $packagePath"
$entry | ConvertTo-Json
