# build-release.ps1 - Builds Release binaries, Portable ZIP, and Inno Setup Installer
param (
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Building MyPad Release v$Version...     " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$projectDir = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProj = Join-Path $projectDir "src\MyPad.App\MyPad.App.csproj"
$publishDir = Join-Path $projectDir "publish\win-x64"
$distDir = Join-Path $projectDir "dist"
$issPath = Join-Path $projectDir "installer\MyPadSetup.iss"

# 1. Ensure dist directory exists
if (!(Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

# 2. Publish self-contained x64 Release
Write-Host "Publishing MyPad.App (Release, win-x64, self-contained)..." -ForegroundColor Yellow
dotnet publish $appProj -c Release -r win-x64 --self-contained -o $publishDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed!"
    exit 1
}

# 3. Create Portable ZIP
$zipPath = Join-Path $distDir "MyPad-v$Version-win-x64-portable.zip"
Write-Host "Creating Portable ZIP: $zipPath..." -ForegroundColor Yellow
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

# 4. Find Inno Setup compiler
$isccCandidates = @(
    "C:\Users\$env:USERNAME\AppData\Local\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($cand in $isccCandidates) {
    if (Test-Path $cand) {
        $isccPath = $cand
        break
    }
}

if ($isccPath) {
    Write-Host "Compiling Inno Setup Installer using $isccPath..." -ForegroundColor Yellow
    & $isccPath $issPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup compilation failed!"
        exit 1
    }
    Write-Host "Installer compiled successfully in $distDir!" -ForegroundColor Green
} else {
    Write-Warning "Inno Setup (ISCC.exe) not found on path. Skipping local installer compilation."
}

Write-Host "==========================================" -ForegroundColor Green
Write-Host " Release artifacts ready in: $distDir" -ForegroundColor Green
Get-ChildItem -Path $distDir | Select-Object Name, @{Name="Size (MB)";Expression={[math]::Round($_.Length / 1MB, 2)}} | Format-Table -AutoSize
Write-Host "==========================================" -ForegroundColor Green
