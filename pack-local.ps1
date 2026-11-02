#Requires -Version 5.1
<#
.SYNOPSIS
    Bumps the CellReport patch version, packs all NuGet packages and refreshes
    the local NuGet global-packages cache.

.DESCRIPTION
    Version scheme: the patch part is incremented automatically. When the next
    patch would reach 10, the minor part is incremented and patch resets to 0
    (0.14.9 -> 0.15.0, 0.15.0 -> 0.15.1, ...).

    After packing, a tiny probe project is restored into the NuGet
    global-packages folder ($env:NUGET_PACKAGES or %USERPROFILE%\.nuget\packages),
    so a consuming solution (Kayala) can reference the new version immediately,
    even before the packages are published to nuget.org.

.PARAMETER Configuration
    Build configuration. Release by default.

.PARAMETER OutputPath
    Folder for the .nupkg files. Defaults to artifacts\packages.

.PARAMETER SkipCache
    Only bump the version and pack; do not touch the NuGet global-packages folder.

.PARAMETER SkipPack
    Only bump the version; do not pack or refresh the cache.

.EXAMPLE
    .\pack-local.ps1

.EXAMPLE
    .\pack-local.cmd

    Use the .cmd wrapper when the PowerShell execution policy blocks .ps1 files
    (it runs the script with -ExecutionPolicy Bypass).

.EXAMPLE
    .\pack-local.ps1 -Configuration Debug -SkipCache
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$OutputPath = '',

    [switch]$SkipCache,

    [switch]$SkipPack
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$PropsPath = Join-Path $Root 'Directory.Build.props'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $Root 'artifacts\packages'
}

function Update-GlobalPackageCache {
    param(
        [Parameter(Mandatory = $true)][string]$Version,
        [Parameter(Mandatory = $true)][string]$PackagesPath
    )

    $globalPackages = $env:NUGET_PACKAGES
    if ([string]::IsNullOrWhiteSpace($globalPackages)) {
        $globalPackages = Join-Path $env:USERPROFILE '.nuget\packages'
    }

    if (-not (Test-Path $PackagesPath)) {
        throw "Packages folder not found: $PackagesPath"
    }
    if (-not (Test-Path $globalPackages)) {
        New-Item -ItemType Directory -Force -Path $globalPackages | Out-Null
    }

    $probeRoot = Join-Path ([IO.Path]::GetTempPath()) ('cellreport-cache-' + [Guid]::NewGuid().ToString('N'))
    try {
        # KCellReport.WinForms targets net48 only and KCellReport.Avalonia net10.0,
        # so restore two probes and let NuGet extract everything into the global cache.
        $targets = @(
            @{ Tfm = 'net10.0'; Packages = @('KCellReport', 'KCellReport.Avalonia') },
            @{ Tfm = 'net48'; Packages = @('KCellReport', 'KCellReport.WinForms') }
        )

        foreach ($target in $targets) {
            $probeDir = Join-Path $probeRoot $target.Tfm
            New-Item -ItemType Directory -Force -Path $probeDir | Out-Null

            $references = ($target.Packages | ForEach-Object {
                "    <PackageReference Include=`"$_`" Version=`"$Version`" />"
            }) -join [Environment]::NewLine

            $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$($target.Tfm)</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
$references
  </ItemGroup>
</Project>
"@

            $projectPath = Join-Path $probeDir 'cache-probe.csproj'
            [IO.File]::WriteAllText($projectPath, $project)

            & dotnet restore $projectPath `
                --packages $globalPackages `
                --source $PackagesPath `
                --ignore-failed-sources `
                --nologo
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet restore failed for $($target.Tfm). Transitive dependencies must be present in $globalPackages."
            }
        }
    }
    finally {
        if (Test-Path $probeRoot) {
            Remove-Item $probeRoot -Recurse -Force
        }
    }

    foreach ($package in @('kcellreport', 'kcellreport.avalonia', 'kcellreport.winforms')) {
        $cachedPath = Join-Path (Join-Path $globalPackages $package) $Version
        if (-not (Test-Path $cachedPath)) {
            throw "Package $package $Version was not added to $globalPackages"
        }
    }

    Write-Host "NuGet cache updated: $globalPackages"
}

# ---------------------------------------------------------------------------
# 1. Bump patch (carry into minor at 10)
# ---------------------------------------------------------------------------
$versionPattern = '(?<=<Version\s+Condition="[^"]*">)(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?=</Version>)'
$text = [IO.File]::ReadAllText($PropsPath)
$match = [regex]::Match($text, $versionPattern)
if (-not $match.Success) {
    throw "Version element not found in $PropsPath"
}

$major = [int]$match.Groups['major'].Value
$minor = [int]$match.Groups['minor'].Value
$patch = [int]$match.Groups['patch'].Value

$patch++
if ($patch -ge 10) {
    $minor++
    $patch = 0
}

$newVersion = "$major.$minor.$patch"
$text = [regex]::Replace($text, $versionPattern, $newVersion)
[IO.File]::WriteAllText($PropsPath, $text)
Write-Host "CellReport version: $($match.Value) -> $newVersion"

# ---------------------------------------------------------------------------
# 2. Pack
# ---------------------------------------------------------------------------
if ($SkipPack) {
    Write-Host 'Packing skipped (-SkipPack).'
}
else {
    & (Join-Path $Root 'pack.ps1') -Configuration $Configuration -OutputPath $OutputPath -Version $newVersion
    if ($LASTEXITCODE -ne 0) {
        throw 'pack.ps1 failed.'
    }
}

# ---------------------------------------------------------------------------
# 3. Refresh the local NuGet global-packages cache
# ---------------------------------------------------------------------------
if ($SkipCache) {
    Write-Host 'NuGet cache update skipped (-SkipCache).'
}
else {
    Update-GlobalPackageCache -Version $newVersion -PackagesPath $OutputPath
}

Write-Host ''
Write-Host "Done: CellReport $newVersion"
Write-Host "Packages: $OutputPath"
