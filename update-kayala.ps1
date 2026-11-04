#Requires -Version 5.1
<#
.SYNOPSIS
    Points the Kayala solution at a CellReport package version.

.DESCRIPTION
    Rewrites the Version attribute of every <PackageReference Include="KCellReport*" />
    item in the Kayala solution (KCellReport, KCellReport.WinForms, KCellReport.Avalonia).

    The version defaults to the one in this repository's Directory.Build.props, so after
    pack-local.ps1 has bumped and packed a release this script moves Kayala onto it.

    Rewrites are idempotent: a file that already references the requested version is left
    untouched (byte for byte, including its BOM and line endings). Projects under
    bin\, obj\ and packages\ are ignored.

    The Kayala folder is resolved in this order: -KayalaRoot, $env:KAYALA_ROOT,
    <CellReport>\..\Kayala-soft\Kayala, D:\Projects\Kayala-soft\Kayala.

.PARAMETER Version
    CellReport package version to apply, for example 0.14.7.
    Defaults to the Directory.Build.props version (the same value pack-local.ps1 bumps).

.PARAMETER KayalaRoot
    Kayala solution folder (the one with Kayala2.sln). Auto-detected by default.

.PARAMETER DryRun
    Print the planned changes without touching any file.

.PARAMETER PackagesPath
    Folder with locally packed .nupkg files, checked only to warn when the requested
    version was never packed. Defaults to artifacts\packages.

.PARAMETER IfPresent
    Exit quietly when the Kayala solution cannot be found instead of failing.
    pack-local.ps1 uses this so a CellReport-only checkout still packs.

.EXAMPLE
    .\update-kayala.ps1 -DryRun

    Show what would change in the auto-detected Kayala checkout.

.EXAMPLE
    .\update-kayala.ps1

    Move Kayala to the version from Directory.Build.props.

.EXAMPLE
    .\update-kayala.ps1 -Version 0.14.7

.EXAMPLE
    .\update-kayala.cmd -KayalaRoot D:\work\Kayala -DryRun

    Use the .cmd wrapper when the execution policy blocks .ps1 files.
#>
param(
    [string]$Version = '',

    [string]$KayalaRoot = '',

    [switch]$DryRun,

    [string]$PackagesPath = '',

    [switch]$IfPresent
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$PropsPath = Join-Path $Root 'Directory.Build.props'

$versionPattern = '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$'
$referencePattern = [regex]'<PackageReference\b[^>]*\bInclude\s*=\s*"(?<id>KCellReport(?:\.[A-Za-z0-9_.-]+)?)"[^>]*>'
$versionAttributePattern = [regex]'(?<prefix>\bVersion\s*=\s*")(?<version>[^"]*)(?<suffix>")'

function Get-CellReportVersion {
    param([string]$Explicit)

    if (-not [string]::IsNullOrWhiteSpace($Explicit)) {
        return $Explicit.Trim()
    }

    $text = [IO.File]::ReadAllText($PropsPath)
    $match = [regex]::Match($text, '<Version\s+Condition="[^"]*">\s*(?<version>[^<\s]+)\s*</Version>')
    if (-not $match.Success) {
        throw "Version element not found in $PropsPath"
    }
    return $match.Groups['version'].Value
}

function Resolve-KayalaRoot {
    param([string]$Explicit)

    if (-not [string]::IsNullOrWhiteSpace($Explicit)) {
        if (-not (Test-Path -LiteralPath $Explicit -PathType Container)) {
            throw "Kayala folder not found: $Explicit"
        }
        return (Resolve-Path -LiteralPath $Explicit).Path
    }

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($env:KAYALA_ROOT)) {
        $candidates += $env:KAYALA_ROOT
    }
    $candidates += (Join-Path (Split-Path -Parent $Root) 'Kayala-soft\Kayala')
    $candidates += 'D:\Projects\Kayala-soft\Kayala'

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    return ''
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [IO.Path]::GetFullPath($Path)
    if (-not $pathFull.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull
    }
    return $pathFull.Substring($rootFull.Length).TrimStart('\', '/')
}

# ---------------------------------------------------------------------------
# 1. Resolve version and the Kayala checkout
# ---------------------------------------------------------------------------
$Version = Get-CellReportVersion -Explicit $Version
if ($Version -notmatch $versionPattern) {
    throw "Invalid package version '$Version'. Expected a NuGet version such as 0.14.7 or 1.0.0-beta.1."
}

$KayalaRoot = Resolve-KayalaRoot -Explicit $KayalaRoot
if ([string]::IsNullOrWhiteSpace($KayalaRoot)) {
    $message = "Kayala solution not found. Pass -KayalaRoot, set KAYALA_ROOT, or clone it next to this repo."
    if ($IfPresent) {
        Write-Warning "$message Skipping."
        return
    }
    throw $message
}

$solution = Get-ChildItem -LiteralPath $KayalaRoot -Filter '*.sln' -File -ErrorAction SilentlyContinue |
    Sort-Object Name |
    Select-Object -First 1
if ($null -eq $solution) {
    $message = "No .sln found in '$KayalaRoot'. -KayalaRoot must point at the Kayala solution folder."
    if ($IfPresent) {
        Write-Warning "$message Skipping."
        return
    }
    throw $message
}

$projects = @(
    Get-ChildItem -LiteralPath $KayalaRoot -Recurse -Filter '*.csproj' -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|packages)\\' } |
        Sort-Object FullName
)

Write-Host "CellReport: $Version"
Write-Host "Kayala:     $KayalaRoot ($($solution.Name), $($projects.Count) project(s))"

# Warn when the requested version was never packed locally: Kayala's restore would fail
# with NU1102 instead of a message that points at the missing pack step.
$packagesPath = if ([string]::IsNullOrWhiteSpace($PackagesPath)) {
    Join-Path $Root 'artifacts\packages'
}
else {
    $PackagesPath
}
if (Test-Path -LiteralPath $packagesPath) {
    foreach ($package in @('KCellReport', 'KCellReport.WinForms', 'KCellReport.Avalonia')) {
        $nupkg = Join-Path $packagesPath "$package.$Version.nupkg"
        if (-not (Test-Path -LiteralPath $nupkg)) {
            Write-Warning "$package $Version is not in $packagesPath - run pack-local.ps1 first."
        }
    }
}

if (Test-Path -LiteralPath (Join-Path $KayalaRoot 'Directory.Packages.props')) {
    Write-Warning 'Directory.Packages.props exists; central PackageVersion entries are not updated by this script.'
}

# ---------------------------------------------------------------------------
# 2. Rewrite PackageReference versions
# ---------------------------------------------------------------------------
$totalChanges = 0
$changedFiles = 0
$referencesSeen = 0
$missingVersion = New-Object System.Collections.Generic.List[string]

foreach ($project in $projects) {
    $relative = Get-RelativePath -Root $KayalaRoot -Path $project.FullName
    $bytes = [IO.File]::ReadAllBytes($project.FullName)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $text = [IO.File]::ReadAllText($project.FullName)

    $matches = $referencePattern.Matches($text)
    if ($matches.Count -eq 0) {
        continue
    }

    $builder = New-Object System.Text.StringBuilder
    $cursor = 0
    $fileChanges = 0

    foreach ($match in $matches) {
        [void]$builder.Append($text.Substring($cursor, $match.Index - $cursor))
        $cursor = $match.Index + $match.Length
        $referencesSeen++

        $packageId = $match.Groups['id'].Value
        $tag = $match.Value
        $versionMatch = $versionAttributePattern.Match($tag)
        if (-not $versionMatch.Success) {
            $missingVersion.Add("$relative ($packageId)")
            [void]$builder.Append($tag)
            continue
        }

        $oldVersion = $versionMatch.Groups['version'].Value
        if ($oldVersion -eq $Version) {
            [void]$builder.Append($tag)
            continue
        }

        $newTag = $tag.Substring(0, $versionMatch.Index) +
            $versionMatch.Groups['prefix'].Value + $Version + $versionMatch.Groups['suffix'].Value +
            $tag.Substring($versionMatch.Index + $versionMatch.Length)
        [void]$builder.Append($newTag)

        $totalChanges++
        $fileChanges++
        $prefix = if ($DryRun) { '[dry-run] ' } else { '' }
        Write-Host ("  {0}{1}: {2} {3} -> {4}" -f $prefix, $relative, $packageId, $oldVersion, $Version)
    }

    [void]$builder.Append($text.Substring($cursor))

    if ($fileChanges -gt 0) {
        $changedFiles++
        if (-not $DryRun) {
            $encoding = New-Object System.Text.UTF8Encoding($hasBom)
            [IO.File]::WriteAllText($project.FullName, $builder.ToString(), $encoding)
        }
    }
}

if ($missingVersion.Count -gt 0) {
    Write-Warning ("PackageReference without a Version attribute (central package management?) in: " +
        (($missingVersion | Sort-Object) -join ', '))
}

# ---------------------------------------------------------------------------
# 3. Summary
# ---------------------------------------------------------------------------
Write-Host ''
if ($referencesSeen -eq 0) {
    throw "No KCellReport PackageReference found under '$KayalaRoot'. Is this the right Kayala solution?"
}
if ($totalChanges -eq 0) {
    Write-Host "Kayala already references KCellReport $Version ($referencesSeen reference(s) checked)."
}
elseif ($DryRun) {
    Write-Host "Dry run: $totalChanges reference(s) in $changedFiles file(s) would be set to $Version. Nothing written."
}
else {
    Write-Host "Updated $totalChanges reference(s) in $changedFiles file(s) to KCellReport $Version."
    Write-Host 'Rebuild the Kayala solution so the new assemblies are picked up.'
}
