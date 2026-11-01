using System;
using System.Collections.Generic;
using System.IO;
using LocalLibrary.Helpers;

public static class UpdateDiscoveryTests
{
    private sealed class Entry { public string Path; public string Arguments; public bool Main; }
    private static int checks;
    private static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
    private static string FileAt(string root, string relative)
    {
        string path = Path.GetFullPath(Path.Combine(root, relative));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, "Non-executable installer-discovery fixture only.");
        return path;
    }
    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "locallibrary-discovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var main = new Entry { Path = FileAt(root, "Base.iso"), Main = true, Arguments = "preserve main arguments" };
            var late = new Entry { Path = FileAt(root, "Game.update.1.10/setup.exe"), Arguments = "custom update arguments" };
            var early = new Entry { Path = FileAt(root, "Game.update.1.9/setup.exe") };
            var duplicate = new Entry { Path = late.Path, Arguments = "different user-selected profile" };
            var input = new[] { late, main, early, duplicate };
            var sorted = UpdateSourceOrdering.Sort(input, x => x.Main, x => x.Path);
            Check(ReferenceEquals(sorted[0], main), "main remains first");
            Check(ReferenceEquals(sorted[1], early), "numeric update sort");
            Check(ReferenceEquals(sorted[2], late) && ReferenceEquals(sorted[3], duplicate), "stable ties preserve profiles");
            Check(late.Arguments == "custom update arguments" && main.Arguments == "preserve main arguments", "arguments not recreated");
            Check(ReferenceEquals(input[0], late), "input sequence unchanged");
            Check(sorted.Count == 4, "no package silently discarded");
            Check(UpdateSourceOrdering.CreateKey(early.Path).TargetVersion == "1.9", "parent release metadata");
            string inline = FileAt(root, "Game.update.1.9-1.10-Group.exe");
            Check(UpdateSourceOrdering.CreateKey(inline).TargetVersion == "1.10", "filename release metadata");
            Check(UpdateSourceOrdering.CreateKey(null).TargetVersion == null, "missing path retains unknown metadata");
            Check(UpdateSourceOrdering.CreateKey(Path.Combine(root, "offline", "unknown.exe")).PublishedAtUtc == null, "offline path has no invented timestamp");
            Check(UpdateSourceOrdering.IsMainName("INSTALL") && UpdateSourceOrdering.IsMainName("Installer"), "exact main names case-insensitive");
            Check(!UpdateSourceOrdering.IsMainName("Install runtimes") && !UpdateSourceOrdering.IsMainName(null), "prefix is not primary identity");
            bool ambiguous;
            Check(InstallerSourceDiscovery.FindUniqueInstaller(Path.GetDirectoryName(early.Path), out ambiguous) == early.Path && !ambiguous, "unique setup");
            FileAt(root, "Game.update.1.9/unins000.exe");
            Check(InstallerSourceDiscovery.FindUniqueInstaller(Path.GetDirectoryName(early.Path), out ambiguous) == early.Path && !ambiguous, "uninstaller not selected");
            string other = FileAt(root, "Single/custom-updater.exe");
            Check(InstallerSourceDiscovery.FindUniqueInstaller(Path.GetDirectoryName(other), out ambiguous) == other && !ambiguous, "one arbitrary updater");
            FileAt(root, "Single/second-updater.exe");
            Check(InstallerSourceDiscovery.FindUniqueInstaller(Path.GetDirectoryName(other), out ambiguous) == null && ambiguous, "ambiguous arbitrary updater refused");
            string primary = FileAt(root, "Package/setup.exe");
            FileAt(root, "Package/helper.exe");
            FileAt(root, "Package/redist/setup.exe");
            Check(InstallerSourceDiscovery.FindUniqueInstaller(Path.GetDirectoryName(primary), out ambiguous) == primary && !ambiguous, "prefer unique setup and skip runtime bundle");
            FileAt(root, "Package/patch.exe");
            Check(InstallerSourceDiscovery.FindUniqueInstaller(Path.GetDirectoryName(primary), out ambiguous) == null && ambiguous, "two named setups refused");
            string deep = FileAt(root, "Deep/a/b/c/d/setup.exe");
            Check(InstallerSourceDiscovery.FindUniqueInstaller(Path.Combine(root, "Deep"), out ambiguous) == null && ambiguous, "incomplete depth-limited scan refused");
            Check(InstallerSourceDiscovery.FindUniqueInstaller(Path.Combine(root, "missing"), out ambiguous) == null && ambiguous, "missing source is not successful scan");
            string unknownA = FileAt(root, "Unknown/a.exe");
            string unknownB = FileAt(root, "Unknown/b.exe");
            File.SetLastWriteTimeUtc(unknownA, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(unknownB, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var dates = UpdateSourceOrdering.Sort(new[] { unknownA, unknownB }, x => false, x => x);
            Check(dates[0] == unknownB, "source date fallback when version unknown");
            string many = Path.Combine(root, "Many");
            Directory.CreateDirectory(many);
            for (int i = 0; i < 2050; i++) File.WriteAllText(Path.Combine(many, "data" + i + ".bin"), "fixture");
            Check(InstallerSourceDiscovery.FindUniqueInstaller(many, out ambiguous) == null && ambiguous, "incomplete scan budget refused");
            Console.WriteLine("PASS " + checks + " independent discovery checks");
            return 0;
        }
        catch (Exception e) { Console.WriteLine("FAIL after " + checks + " checks: " + e.Message); return 1; }
        finally { Directory.Delete(root, true); }
    }
}
