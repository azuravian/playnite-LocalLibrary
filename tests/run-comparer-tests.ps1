$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler is required.' }
$temp = Join-Path ([IO.Path]::GetTempPath()) ('locallibrary-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
$exe = Join-Path $temp 'ComparerTests.exe'
try {
    & $compiler /nologo /langversion:5 /target:exe "/out:$exe" (Join-Path $root 'Helpers/UpdatePackageComparer.cs') (Join-Path $PSScriptRoot 'UpdatePackageComparerTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Comparer acceptance failed.' }
} finally {
    if (Test-Path -LiteralPath $exe) { Remove-Item -LiteralPath $exe }
    if (@(Get-ChildItem -Force -LiteralPath $temp).Count -eq 0) { Remove-Item -LiteralPath $temp }
}
