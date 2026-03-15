# BeautyCons Extension Packaging Script
# Creates a .pext package for Playnite installation
#
# Usage: .\package_extension.ps1 [-Configuration Release|Debug]
#
# Note: This script packages an already-built project. Build first with:
#   dotnet build -c Release

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BeautyCons Extension Packaging" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host ""

# Get project root (one level up from scripts/)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
Set-Location $projectRoot

# Read version from version.txt (single source of truth)
$versionFile = Join-Path $projectRoot "version.txt"
if (-not (Test-Path $versionFile)) {
    Write-Host "ERROR: version.txt not found. Please create it with the version number (e.g., 1.0.0)" -ForegroundColor Red
    exit 1
}
$versionFull = (Get-Content $versionFile -Raw).Trim()
# Convert version format: 1.0.0 -> 1_0_0 for filename
$version = $versionFull -replace '\.', '_'

# Update AssemblyInfo.cs with version from version.txt
$assemblyInfoPath = Join-Path $projectRoot "src\AssemblyInfo.cs"
if (Test-Path $assemblyInfoPath) {
    $assemblyInfoContent = Get-Content $assemblyInfoPath -Raw
    if ($assemblyInfoContent -match '\[assembly:\s*AssemblyVersion\("[\d\.]+"\)\]') {
        $assemblyInfoContent = $assemblyInfoContent -replace '\[assembly:\s*AssemblyVersion\("[\d\.]+"\)\]', "[assembly: AssemblyVersion(`"$versionFull`")]"
    }
    if ($assemblyInfoContent -match '\[assembly:\s*AssemblyFileVersion\("[\d\.]+"\)\]') {
        $assemblyInfoContent = $assemblyInfoContent -replace '\[assembly:\s*AssemblyFileVersion\("[\d\.]+"\)\]', "[assembly: AssemblyFileVersion(`"$versionFull`")]"
    }
    if ($assemblyInfoContent -match '\[assembly:\s*AssemblyInformationalVersion\("[\d\.]+"\)\]') {
        $assemblyInfoContent = $assemblyInfoContent -replace '\[assembly:\s*AssemblyInformationalVersion\("[\d\.]+"\)\]', "[assembly: AssemblyInformationalVersion(`"$versionFull`")]"
    }
    Set-Content -Path $assemblyInfoPath -Value $assemblyInfoContent -NoNewline
    Write-Host "Updated AssemblyInfo.cs with version $versionFull" -ForegroundColor Gray
    Write-Host ""
}

# Build paths
$outputDir = "src\bin\$Configuration\net4.6.2"
$packageDir = "package"
$extensionName = "BeautyCons"
$extensionId = "eb7017af-c0ee-4416-aec5-27d516530af7"

# Verify DLL exists and show details
$dllPath = Join-Path $outputDir "BeautyCons.dll"
if (-not (Test-Path $dllPath)) {
    Write-Host "ERROR: BeautyCons.dll not found in $outputDir" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please build the project first:" -ForegroundColor Yellow
    Write-Host "  dotnet build -c $Configuration" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Show DLL info to verify it's fresh
$dllInfo = Get-Item $dllPath
Write-Host "Found DLL: $($dllInfo.Name)" -ForegroundColor Green
Write-Host "  Size: $([math]::Round($dllInfo.Length/1KB, 2)) KB" -ForegroundColor Gray
Write-Host "  Modified: $($dllInfo.LastWriteTime)" -ForegroundColor Gray
Write-Host ""

# Clean previous package
Write-Host "Preparing package directory..." -ForegroundColor Yellow
if (Test-Path $packageDir) {
    Remove-Item -Path $packageDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Cleaned existing package directory" -ForegroundColor Gray
}
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

Write-Host "Copying extension files..." -ForegroundColor Yellow

# Update extension.yaml with current version before copying
$extensionYamlPath = Join-Path $projectRoot "extension.yaml"
if (Test-Path $extensionYamlPath) {
    $yamlContent = Get-Content $extensionYamlPath -Raw
    if ($yamlContent -match "Version:\s*[\d\.]+") {
        $yamlContent = $yamlContent -replace "Version:\s*[\d\.]+", "Version: $versionFull"
        Set-Content -Path $extensionYamlPath -Value $yamlContent -NoNewline
        Write-Host "  Updated extension.yaml with version $versionFull" -ForegroundColor Gray
    }
}

# Copy core files
$coreFiles = @(
    "extension.yaml",
    "icon.png",
    "LICENSE"
)

foreach ($file in $coreFiles) {
    if (Test-Path $file) {
        Copy-Item $file -Destination $packageDir -Force
        Write-Host "  Copied file: $file" -ForegroundColor Gray
    } else {
        Write-Host "  WARNING: $file not found (optional)" -ForegroundColor Yellow
    }
}

# Copy main DLL
Copy-Item $dllPath -Destination $packageDir -Force
Write-Host "  Copied: BeautyCons.dll" -ForegroundColor Gray

# Copy dependencies from build output (exclude system/Playnite DLLs)
Write-Host "Copying dependencies..." -ForegroundColor Yellow
$additionalDlls = Get-ChildItem -Path $outputDir -Filter "*.dll" | Where-Object {
    $_.Name -ne "BeautyCons.dll" -and
    $_.Name -ne "Playnite.SDK.dll" -and
    $_.Name -notlike "System.*" -and
    $_.Name -notlike "WindowsBase.dll" -and
    $_.Name -notlike "PresentationCore.dll" -and
    $_.Name -notlike "PresentationFramework.dll"
}

if ($additionalDlls) {
    foreach ($dll in $additionalDlls) {
        $destPath = Join-Path $packageDir $dll.Name
        if (-not (Test-Path $destPath)) {
            Copy-Item $dll.FullName -Destination $destPath -Force
            Write-Host "  Copied: $($dll.Name)" -ForegroundColor Gray
        }
    }
}

# Copy SkiaSharp native DLLs (required for icon glow gaussian blur)
Write-Host "Copying SkiaSharp native DLLs..." -ForegroundColor Yellow
$skiaSubDirs = @("x86", "x64")
foreach ($subDir in $skiaSubDirs) {
    $skiaSrcDir = Join-Path $outputDir $subDir
    if (Test-Path $skiaSrcDir) {
        $skiaDestDir = Join-Path $packageDir $subDir
        if (-not (Test-Path $skiaDestDir)) {
            New-Item -ItemType Directory -Path $skiaDestDir -Force | Out-Null
        }
        Copy-Item (Join-Path $skiaSrcDir "*") -Destination $skiaDestDir -Force
        Write-Host "  Copied: $subDir\libSkiaSharp.dll" -ForegroundColor Gray
    }
}

# Copy System.Memory.dll (SkiaSharp dependency, excluded by System.* filter above)
$sysMemDll = Join-Path $outputDir "System.Memory.dll"
if (Test-Path $sysMemDll) {
    $destPath = Join-Path $packageDir "System.Memory.dll"
    if (-not (Test-Path $destPath)) {
        Copy-Item $sysMemDll -Destination $destPath -Force
        Write-Host "  Copied: System.Memory.dll (SkiaSharp dependency)" -ForegroundColor Gray
    }
}

# Create .pext file (ZIP with different extension)
Write-Host "Creating .pext package..." -ForegroundColor Yellow

# Create pext output folder if it doesn't exist
$pextOutputDir = Join-Path $projectRoot "pext"
if (-not (Test-Path $pextOutputDir)) {
    New-Item -ItemType Directory -Path $pextOutputDir -Force | Out-Null
    Write-Host "  Created pext output folder" -ForegroundColor Gray
}

$pextFileName = "$extensionName.$extensionId`_$version.pext"
$pextFilePath = Join-Path $pextOutputDir $pextFileName
$zipFilePath = Join-Path $pextOutputDir "$extensionName.$extensionId`_$version.zip"

# Remove old package if exists
if (Test-Path $pextFilePath) {
    Remove-Item $pextFilePath -Force -ErrorAction SilentlyContinue
}
if (Test-Path $zipFilePath) {
    Remove-Item $zipFilePath -Force -ErrorAction SilentlyContinue
}

# Verify package contents before creating archive
Write-Host "Verifying package contents..." -ForegroundColor Yellow
$packageFiles = Get-ChildItem -Path $packageDir -File -Recurse
$requiredFiles = @("BeautyCons.dll", "extension.yaml")
$missingFiles = @()

foreach ($required in $requiredFiles) {
    if (-not ($packageFiles | Where-Object { $_.Name -eq $required })) {
        $missingFiles += $required
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "ERROR: Missing required files in package:" -ForegroundColor Red
    foreach ($file in $missingFiles) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    exit 1
}

Write-Host "  Package contains $($packageFiles.Count) files" -ForegroundColor Gray
Write-Host ""

# Create ZIP first (Compress-Archive limitation)
Write-Host "Creating .pext archive..." -ForegroundColor Yellow
try {
    Compress-Archive -Path "$packageDir\*" -DestinationPath $zipFilePath -Force

    # Rename to .pext
    Rename-Item -Path $zipFilePath -NewName $pextFileName -Force

    $packageInfo = Get-Item $pextFilePath

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  PACKAGE CREATED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Package Details:" -ForegroundColor Cyan
    Write-Host "  File: $($packageInfo.Name)" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($packageInfo.Length/1KB, 2)) KB" -ForegroundColor White
    Write-Host "  Location: $($packageInfo.FullName)" -ForegroundColor White
    Write-Host "  Version: $versionFull" -ForegroundColor White
    Write-Host ""
    Write-Host "Package Contents:" -ForegroundColor Cyan
    foreach ($file in $packageFiles | Sort-Object Name) {
        Write-Host "  - $($file.Name) ($([math]::Round($file.Length/1KB, 2)) KB)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "To install in Playnite:" -ForegroundColor Cyan
    Write-Host "  1. Open Playnite" -ForegroundColor White
    Write-Host "  2. Go to Add-ons -> Extensions" -ForegroundColor White
    Write-Host "  3. Click 'Add extension' and select the .pext file" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ERROR: Failed to create package" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host $_.Exception.InnerException.Message -ForegroundColor Red
    }
    Write-Host ""
    exit 1
}
