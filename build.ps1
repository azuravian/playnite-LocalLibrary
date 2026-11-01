param(
    [string]$ReferenceRoot = "${env:ProgramFiles(x86)}/Reference Assemblies/Microsoft/Framework",
    [string]$CompilerDirectory = '',
    [string]$MSBuildPath = ''
)
$ErrorActionPreference = 'Stop'
$framework = Join-Path $ReferenceRoot '.NETFramework/v4.6.2'
if (-not (Test-Path (Join-Path $framework 'RedistList/FrameworkList.xml'))) {
    throw 'The .NET Framework 4.6.2 reference assemblies are required. Pass -ReferenceRoot if using an isolated reference package.'
}
if (-not $MSBuildPath) {
    $command = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($command) { $MSBuildPath = $command.Source }
    else { $MSBuildPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/MSBuild.exe' }
}
if (-not (Test-Path -LiteralPath $MSBuildPath)) { throw 'MSBuild is required.' }
$arguments = @((Join-Path $PSScriptRoot 'LocalLibrary.csproj'), '/nologo', '/t:Rebuild', '/p:Configuration=Release',
    "/p:TargetFrameworkRootPath=$ReferenceRoot", "/p:FrameworkPathOverride=$framework", '/v:minimal')
if ($MSBuildPath.Replace('\', '/') -like '*Microsoft.NET/Framework*') {
    if (-not $CompilerDirectory) { throw 'The legacy MSBuild fallback requires an explicit modern compiler directory.' }
    $arguments += '/tv:4.0'
}
if ($CompilerDirectory) {
    if (-not (Test-Path (Join-Path $CompilerDirectory 'csc.exe'))) { throw 'CompilerDirectory must contain csc.exe.' }
    $arguments += "/p:CscToolPath=$CompilerDirectory", '/p:CscToolExe=csc.exe'
}
& $MSBuildPath @arguments
if ($LASTEXITCODE -ne 0) { throw 'LocalLibrary build failed.' }
