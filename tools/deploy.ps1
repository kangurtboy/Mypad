# deploy.ps1 - Installs/updates MyPad on the local PC and launches it
param (
    [switch]$NoLaunch
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Building and Deploying MyPad to PC...   " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$projectDir = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProj = Join-Path $projectDir "src\MyPad.App\MyPad.App.csproj"
$buildOut = Join-Path $projectDir "src\MyPad.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64"

# 1. Stop running MyPad instances
Write-Host "Stopping running MyPad processes..." -ForegroundColor Yellow
Get-Process -Name "MyPad.App" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

# 2. Build the app
Write-Host "Building MyPad.App (Self-contained)..." -ForegroundColor Yellow
dotnet build $appProj -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# 3. Target install directory in %LocalAppData%\Programs\MyPad
$installDir = Join-Path $env:LOCALAPPDATA "Programs\MyPad"
Write-Host "Installing to: $installDir" -ForegroundColor Yellow

if (!(Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

# Copy all output files into installation directory
Copy-Item -Path "$buildOut\*" -Destination $installDir -Recurse -Force

$installedExe = Join-Path $installDir "MyPad.App.exe"

# 4. Create/update Start Menu Shortcut
$startMenuDir = [Environment]::GetFolderPath([Environment+SpecialFolder]::Programs)
$shortcutPath = Join-Path $startMenuDir "MyPad.lnk"

Write-Host "Creating Start Menu shortcut at: $shortcutPath" -ForegroundColor Yellow
$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExe
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "MyPad - Lightweight native Windows text editor"
$shortcut.IconLocation = "$installedExe,0"
$shortcut.Save()

# 5. Register in Windows App Paths (for Win+R 'mypad')
try {
    $regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\App Paths\mypad.exe"
    if (!(Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
    }
    Set-ItemProperty -Path $regPath -Name "(default)" -Value $installedExe
    Set-ItemProperty -Path $regPath -Name "Path" -Value $installDir
    Write-Host "Registered in Windows App Paths (Win+R 'mypad')" -ForegroundColor Green
} catch {
    Write-Warning "Could not register App Path: $_"
}

Write-Host "==========================================" -ForegroundColor Green
Write-Host " MyPad successfully installed and updated!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

# 6. Launch installed application
if (!$NoLaunch) {
    Write-Host "Launching installed MyPad..." -ForegroundColor Cyan
    Start-Process -FilePath $installedExe
}
