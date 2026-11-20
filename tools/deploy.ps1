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

$assetsSource = Join-Path $projectDir "src\MyPad.App\Assets"
if (Test-Path $assetsSource) {
    Copy-Item -Path $assetsSource -Destination $installDir -Recurse -Force
}

$installedExe = Join-Path $installDir "MyPad.App.exe"
$installedIco = Join-Path $installDir "Assets\MyPad.ico"

# 4. Create/update Start Menu Shortcut
$startMenuDir = [Environment]::GetFolderPath([Environment+SpecialFolder]::Programs)
$shortcutPath = Join-Path $startMenuDir "MyPad.lnk"

Write-Host "Creating Start Menu shortcut at: $shortcutPath" -ForegroundColor Yellow
$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExe
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "MyPad - Lightweight native Windows text editor"
if (Test-Path $installedIco) {
    $shortcut.IconLocation = "$installedIco,0"
} else {
    $shortcut.IconLocation = "$installedExe,0"
}
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

# 6. Register Application & "Open with" File Associations (HKCU - no admin required)
try {
    Write-Host "Registering Windows File Associations and 'Open with' (HKCU)..." -ForegroundColor Yellow

    function Set-RegKeyDefault {
        param([string]$SubKeyPath, [string]$Value, [string]$Name = "")
        $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($SubKeyPath)
        if ($key) {
            $key.SetValue($Name, $Value)
            $key.Close()
        }
    }

    # App registration in Software\Classes\Applications\MyPad.App.exe
    Set-RegKeyDefault "Software\Classes\Applications\MyPad.App.exe" "MyPad"
    Set-RegKeyDefault "Software\Classes\Applications\MyPad.App.exe" "MyPad" "FriendlyAppName"
    Set-RegKeyDefault "Software\Classes\Applications\MyPad.App.exe" "MyPad" "ApplicationCompany"
    Set-RegKeyDefault "Software\Classes\Applications\MyPad.App.exe\DefaultIcon" "$installedIco,0"
    Set-RegKeyDefault "Software\Classes\Applications\MyPad.App.exe\shell\open" "MyPad" "FriendlyAppName"
    Set-RegKeyDefault "Software\Classes\Applications\MyPad.App.exe\shell\open\command" "`"$installedExe`" `"%1`""

    # Supported file extensions
    $supportedExts = @('.txt', '.md', '.markdown', '.log', '.ini', '.cfg', '.json', '.xml', '.csv', '.yaml', '.yml')
    foreach ($ext in $supportedExts) {
        Set-RegKeyDefault "Software\Classes\Applications\MyPad.App.exe\SupportedTypes" "" $ext
        Set-RegKeyDefault "Software\Classes\$ext\OpenWithProgids" "" "MyPad.Document"
        Set-RegKeyDefault "Software\Classes\$ext\OpenWithList\MyPad.App.exe" ""
        Set-RegKeyDefault "Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\$ext\OpenWithProgids" "" "MyPad.Document"
    }

    # Register ProgID: MyPad.Document
    Set-RegKeyDefault "Software\Classes\MyPad.Document" "MyPad Document"
    Set-RegKeyDefault "Software\Classes\MyPad.Document\DefaultIcon" "$installedIco,0"
    Set-RegKeyDefault "Software\Classes\MyPad.Document\shell\open" "MyPad" "FriendlyAppName"
    Set-RegKeyDefault "Software\Classes\MyPad.Document\shell\open\command" "`"$installedExe`" `"%1`""

    # Add MyPad directly into Explorer OpenWithList MRU for each extension
    foreach ($ext in $supportedExts) {
        try {
            $mruKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\$ext\OpenWithList")
            if ($mruKey) {
                $valNames = $mruKey.GetValueNames()
                $found = $false
                foreach ($vn in $valNames) {
                    if ($vn -match '^[a-z]$' -and ($mruKey.GetValue($vn) -eq "MyPad.App.exe")) {
                        $found = $true
                        break
                    }
                }
                if (!$found) {
                    $mru = [string]$mruKey.GetValue("MRUList", "")
                    $letters = "abcdefghijklmnopqrstuvwxyz"
                    for ($i = 0; $i -lt $letters.Length; $i++) {
                        $c = $letters[$i].ToString()
                        if ($mru.IndexOf($c) -lt 0) {
                            $mruKey.SetValue($c, "MyPad.App.exe")
                            $mruKey.SetValue("MRUList", $c + $mru)
                            break
                        }
                    }
                }
                $mruKey.Close()
            }
        } catch { }
    }

    # Capabilities & RegisteredApplications
    Set-RegKeyDefault "Software\MyPad\Capabilities" "MyPad" "ApplicationName"
    Set-RegKeyDefault "Software\MyPad\Capabilities" "A lightweight native Windows text editor with Markdown live preview" "ApplicationDescription"
    Set-RegKeyDefault "Software\MyPad\Capabilities" "$installedIco,0" "ApplicationIcon"
    foreach ($ext in $supportedExts) {
        Set-RegKeyDefault "Software\MyPad\Capabilities\FileAssociations" "MyPad.Document" $ext
    }
    Set-RegKeyDefault "Software\RegisteredApplications" "Software\MyPad\Capabilities" "MyPad"

    # Context Menu: "Edit with MyPad" in Windows Explorer context menu for all files and text files
    $contextTargets = @(
        "Software\Classes\*\shell\OpenWithMyPad",
        "Software\Classes\SystemFileAssociations\text\shell\OpenWithMyPad",
        "Software\Classes\SystemFileAssociations\.md\shell\OpenWithMyPad"
    )

    foreach ($ctx in $contextTargets) {
        Set-RegKeyDefault $ctx "Edit with MyPad"
        Set-RegKeyDefault $ctx "$installedIco,0" "Icon"
        Set-RegKeyDefault "$ctx\command" "`"$installedExe`" `"%1`""
    }

    # Notify Windows Shell about association changes
    try {
        Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class ShellHelper {
    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
"@ -ErrorAction SilentlyContinue
        [ShellHelper]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)
    } catch { }

    Write-Host "File associations, context menus, and 'Open with' successfully registered!" -ForegroundColor Green
} catch {
    Write-Warning "Could not register file associations: $_"
}

Write-Host "==========================================" -ForegroundColor Green
Write-Host " MyPad successfully installed and updated!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

# 6. Launch installed application
if (!$NoLaunch) {
    Write-Host "Launching installed MyPad..." -ForegroundColor Cyan
    Start-Process -FilePath $installedExe
}
