using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using LocalLibrary.Helpers;
using Playnite.SDK.Models;

public static class UpdateWorkflowTests
{
    static int checks;
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    static string FileAt(string root, string relative)
    {
        string path = Path.GetFullPath(Path.Combine(root, relative));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, "Non-executable workflow fixture only.");
        return path;
    }
    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "locallibrary-workflow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Type type = typeof(InstalledGameLinker).Assembly.GetType("LocalLibrary.Finder", true);
            object finder = Activator.CreateInstance(type, true);
            MethodInfo scan = type.GetMethod("FindGameUpdates");
            string mainPath = FileAt(root, "base.iso");
            string oldPath = FileAt(root, "Updates/Game.update.1.9/setup.exe");
            string newPath = FileAt(root, "Updates/Game.update.1.9-1.10-ElAmigos/setup.exe");
            var main = new GameAction { Name = "Install", Path = mainPath, Arguments = "keep-main", Type = GameActionType.File };
            var old = new GameAction { Name = "My custom patch", Path = oldPath.Replace('\\', '/'), Arguments = "keep-update", WorkingDir = "keep-workdir", Type = GameActionType.File };
            var play = new GameAction { Name = "Custom Play", Path = "game.exe", Arguments = "keep-launch", IsPlayAction = true, Type = GameActionType.File };
            var game = new Game { Name = "Workflow fixture", Playtime = 2222, CoverImage = "keep-cover", IsInstalled = true,
                GameActions = new ObservableCollection<GameAction> { play, main, old } };
            int count = (int)scan.Invoke(finder, new object[] { game, root, true });
            Check(count == 2 && game.GameActions.Count == 4, "discover one update without duplicating existing mixed-separator path");
            Check(ReferenceEquals(game.GameActions[0], main), "source installer remains primary");
            Check(ReferenceEquals(game.GameActions[1], old) && old.Name == "My custom patch", "manual name and object retained");
            Check(old.Arguments == "keep-update" && old.WorkingDir == "keep-workdir", "custom installer settings retained");
            Check(game.GameActions[2].Path == newPath && game.GameActions[2].Name == "Update 1.10", "version-ordered new update associated");
            Check(ReferenceEquals(game.GameActions[3], play) && play.Arguments == "keep-launch", "play action retained separately");
            Check(main.Arguments == "keep-main" && game.Playtime == 2222 && game.CoverImage == "keep-cover" && game.IsInstalled, "game metadata retained");
            scan.Invoke(finder, new object[] { game, root, true });
            Check(game.GameActions.Count == 4 && ReferenceEquals(game.GameActions[1], old), "repeat scan idempotent");
            File.Delete(oldPath);
            scan.Invoke(finder, new object[] { game, root, true });
            Check(game.GameActions.Contains(old), "missing source link is retained rather than silently removed");
            var before = game.GameActions;
            Directory.Move(Path.Combine(root, "Updates"), Path.Combine(root, "temporarily-offline"));
            count = (int)scan.Invoke(finder, new object[] { game, root, true });
            Check(count == 0 && ReferenceEquals(before, game.GameActions), "unavailable update source leaves collection untouched");
            Directory.Move(Path.Combine(root, "temporarily-offline"), Path.Combine(root, "Updates"));
            var rom = new GameRom { Name = "Installer", Path = mainPath };
            var romGame = new Game { Roms = new ObservableCollection<GameRom> { rom }, GameActions = new ObservableCollection<GameAction> { play } };
            count = (int)scan.Invoke(finder, new object[] { romGame, root, false });
            Check(count == 1 && romGame.Roms.Count == 2 && ReferenceEquals(romGame.Roms[0], rom), "ROM source discovery preserves main");
            Check(ReferenceEquals(romGame.GameActions[0], play), "ROM discovery does not touch play actions");
            Console.WriteLine("PASS " + checks + " actual-plugin workflow checks");
            return 0;
        }
        catch (Exception e)
        {
            Exception detail = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
            Console.WriteLine("FAIL after " + checks + " checks: " + detail.GetType().Name + ": " + detail.Message);
            return 1;
        }
        finally { Directory.Delete(root, true); }
    }
}
