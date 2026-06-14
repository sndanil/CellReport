#Requires -Version 5.1
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$OutputPath = '',

    [string]$Version = '',

    [switch]$Push
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $Root 'artifacts\packages'
}

$packArgs = @(
    'msbuild', (Join-Path $Root 'pack.proj')
    '-t:PackAll'
    "/p:Configuration=$Configuration"
    "/p:PackageOutputPath=$OutputPath"
)

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $packArgs += "/p:Version=$Version"
    $packArgs += "/p:AssemblyVersion=$Version"
    $packArgs += "/p:FileVersion=$Version"
}

Write-Host "Packing CellReport NuGet packages ($Configuration) -> $OutputPath"
dotnet @packArgs

$packages = Get-ChildItem -Path $OutputPath -Filter '*.nupkg' | Sort-Object Name
if ($packages.Count -eq 0) {
    throw 'No .nupkg files were produced.'
}

Write-Host ''
Write-Host 'Created packages:'
foreach ($package in $packages) {
    Write-Host "  $($package.Name)"
}

if ($Push) {
    foreach ($package in $packages) {
        Write-Host "Pushing $($package.Name)..."
        dotnet nuget push $package.FullName --source https://api.nuget.org/v3/index.json
    }
}
