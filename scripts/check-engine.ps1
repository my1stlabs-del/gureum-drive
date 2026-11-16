$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /platform:x64 /out:"$root\dist\EngineCheck.exe" "$PSScriptRoot\EngineCheck.cs"
if($LASTEXITCODE -ne 0) { throw 'Test build failed' }
& "$root\dist\EngineCheck.exe"
if($LASTEXITCODE -ne 0) { throw 'Engine check failed' }
