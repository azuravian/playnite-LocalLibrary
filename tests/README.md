# Development acceptance

Use a disposable source checkout on Windows. The standalone fixtures never
start a real game/installer or modify a Playnite library. The temporary `.exe`
source files in discovery/link fixtures contain plain test text and are never
executed. The test programs themselves are ordinary compiled console programs.

After building the plugin, run all five suites with:

```powershell
./tests/run-all-tests.ps1
```

The initial comparer fixture can also be run separately with:

```powershell
powershell -NoProfile -ExecutionPolicy RemoteSigned -File tests/run-comparer-tests.ps1
```

For the other console fixtures, compile the test entry point with its sources:

| Fixture | Inputs in addition to the test source | Actual checks |
|---|---|---:|
| UpdatePackageComparerTests | Helpers/UpdatePackageComparer.cs | 388 |
| ReleaseUpdateMetadataTests | comparer and Helpers/ReleaseUpdateMetadata.cs | 124 |
| UpdateDiscoveryTests | comparer, parser, UpdateSourceOrdering, InstallerSourceDiscovery | 22 |
| InstalledGameLinkerTests | Helpers/InstalledGameLinker.cs, Playnite.SDK and Newtonsoft.Json references | 23 |
| UpdateWorkflowTests | built LocalLibrary.dll, Playnite.SDK reference; library dependencies beside executable | 12 |

For example, in a Developer Command Prompt:

```bat
csc /nologo /langversion:5 /out:MetadataTests.exe Helpers\UpdatePackageComparer.cs Helpers\ReleaseUpdateMetadata.cs tests\ReleaseUpdateMetadataTests.cs
MetadataTests.exe
```

Do not compile all console entry points into one executable without selecting
the desired main class. Use a temporary output directory and clean only files
created by the fixture. The numbers above are executed assertions, not game
compatibility counts. Total: 569 checks across the five suites.

## Full plugin build

The project targets .NET Framework 4.6.2. Install normal Windows development
tools or use explicit isolated paths with `build.ps1`. The supervised build
used Microsoft.Net.Compilers.Toolset 4.8.0 and
Microsoft.NETFramework.ReferenceAssemblies.net462 1.0.3, downloaded from NuGet
and checked against the catalog's SHA-512 values. Those tools are development
dependencies, not end-user plugin prerequisites.

```powershell
./build.ps1 -ReferenceRoot C:\development\references\build -CompilerDirectory C:\development\compiler\tasks\net472
```

The reference root is the parent containing `.NETFramework`, not that folder
itself. The script checks the reference pack before building so a missing pack
cannot silently substitute newer framework APIs. This recipe was exercised
in an owned temporary Windows directory, without installing global tools.
Normal current MSBuild/Visual Studio is the preferred development environment;
the explicit legacy fallback is an isolated compatibility recipe, not a promise
that future compiler packages support old MSBuild.

No interactive Playnite/Helium GUI, remote-client SSHFS ISO or real game
installation acceptance has been completed. Keep those release gates open.
