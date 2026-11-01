param([string]$BuildDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'bin/Release'))
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The Windows .NET Framework C# compiler is required.' }
$sdk = Join-Path $root 'packages/PlayniteSDK.6.11.0/lib/net462/Playnite.SDK.dll'
$json = Join-Path $root 'packages/Newtonsoft.Json.10.0.3/lib/net45/Newtonsoft.Json.dll'
$plugin = Join-Path $BuildDirectory 'LocalLibrary.dll'
foreach ($path in @($sdk, $json, $plugin)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Build the plugin first; missing dependency: $path" }
}
$temp = Join-Path ([IO.Path]::GetTempPath()) ('locallibrary-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
$owned = @('Playnite.SDK.dll', 'Newtonsoft.Json.dll', 'LocalLibrary.dll', 'Ookii.Dialogs.Wpf.dll')
$cases = @(
    @{ Name='UpdatePackageComparerTests'; Sources=@('UpdatePackageComparer.cs'); References=@() },
    @{ Name='ReleaseUpdateMetadataTests'; Sources=@('UpdatePackageComparer.cs','ReleaseUpdateMetadata.cs'); References=@() },
    @{ Name='UpdateDiscoveryTests'; Sources=@('UpdatePackageComparer.cs','ReleaseUpdateMetadata.cs','UpdateSourceOrdering.cs','InstallerSourceDiscovery.cs'); References=@() },
    @{ Name='InstalledGameLinkerTests'; Sources=@('InstalledGameLinker.cs'); References=@($sdk,$json) },
    @{ Name='UpdateWorkflowTests'; Sources=@(); References=@($plugin,$sdk,$json) }
)
try {
    Copy-Item -LiteralPath $sdk -Destination $temp
    Copy-Item -LiteralPath $json -Destination $temp
    Copy-Item -LiteralPath $plugin -Destination $temp
    Copy-Item -LiteralPath (Join-Path $BuildDirectory 'Ookii.Dialogs.Wpf.dll') -Destination $temp
    foreach ($case in $cases) {
        $exe = Join-Path $temp ($case.Name + '.exe')
        $owned += $case.Name + '.exe'
        $arguments = @('/nologo','/langversion:5','/target:exe',"/out:$exe")
        foreach ($reference in $case.References) { $arguments += "/r:$reference" }
        foreach ($source in $case.Sources) { $arguments += Join-Path $root ('Helpers/' + $source) }
        $arguments += Join-Path $PSScriptRoot ($case.Name + '.cs')
        & $compiler @arguments
        if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $($case.Name)" }
        $start = New-Object Diagnostics.ProcessStartInfo
        $start.FileName = $exe
        $start.WorkingDirectory = $temp
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $process = New-Object Diagnostics.Process
        $process.StartInfo = $start
        try {
            $null = $process.Start()
            if (-not $process.WaitForExit(30000)) { $process.Kill(); throw "Fixture timeout: $($case.Name)" }
            $stdout = $process.StandardOutput.ReadToEnd()
            $stderr = $process.StandardError.ReadToEnd()
            Write-Output $stdout.Trim()
            if ($stderr) { Write-Output $stderr.Trim() }
            if ($process.ExitCode -ne 0) { throw "Fixture failed: $($case.Name)" }
        } finally { $process.Dispose() }
    }
} finally {
    foreach ($name in $owned) {
        $path = Join-Path $temp $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    if (@(Get-ChildItem -Force -LiteralPath $temp).Count -eq 0) { Remove-Item -LiteralPath $temp }
}
