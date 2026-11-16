param([string]$OutputDirectory = "dist")
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$outputRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $root $OutputDirectory }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force $outputRoot | Out-Null
$sources = Get-ChildItem -LiteralPath "$root\src" -Filter '*.cs' | Select-Object -ExpandProperty FullName
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /out:"$outputRoot\GureumDrive.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll "/resource:$root\src\Strings.en.json,CloudDock.Strings.en.json" "/resource:$root\src\RcloneHelp.ko.json,CloudDock.RcloneHelp.ko.json" $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item -LiteralPath "$root\README.md" -Destination "$outputRoot\README.md"
Copy-Item -LiteralPath "$root\LICENSE" -Destination "$outputRoot\LICENSE"
Copy-Item -LiteralPath "$root\src\CloudDock.exe.config" -Destination "$outputRoot\GureumDrive.exe.config"
Copy-Item -LiteralPath "$root\THIRD-PARTY-NOTICES.md" -Destination "$outputRoot\THIRD-PARTY-NOTICES.md"
Write-Host "Built: $outputRoot\GureumDrive.exe"
