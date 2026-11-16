$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$version = 'v1.76.0'
$root = $PSScriptRoot
$tools = Join-Path $root 'tools'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $tools,$dist | Out-Null
$archive = "rclone-$version-windows-amd64.zip"
Invoke-WebRequest "https://downloads.rclone.org/$version/$archive" -OutFile "$tools\rclone.zip"
Invoke-WebRequest "https://downloads.rclone.org/$version/SHA256SUMS" -OutFile "$tools\SHA256SUMS"
$line = Get-Content "$tools\SHA256SUMS" | Where-Object { $_ -match ([regex]::Escape($archive) + '$') } | Select-Object -First 1
if (-not $line) { throw 'Official checksum entry missing' }
$expected = ($line -split '\s+')[0]
if ((Get-FileHash "$tools\rclone.zip" -Algorithm SHA256).Hash -ne $expected) { throw 'rclone checksum mismatch' }
Expand-Archive -LiteralPath "$tools\rclone.zip" -DestinationPath "$tools\unpacked" -Force
Copy-Item -LiteralPath "$tools\unpacked\rclone-$version-windows-amd64\rclone.exe" -Destination "$dist\rclone.exe"
Copy-Item -LiteralPath "$tools\unpacked\rclone-$version-windows-amd64\README.txt" -Destination "$dist\RCLONE-NOTICES.txt"
Write-Host "Verified and installed rclone $version"
