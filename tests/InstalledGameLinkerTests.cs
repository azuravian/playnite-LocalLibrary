using System;
using System.Collections.ObjectModel;
using System.IO;
using LocalLibrary.Helpers;
using Playnite.SDK.Models;

public static class InstalledGameLinkerTests
{
    static int checks;
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "locallibrary-link-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Game", "bin"));
        try
        {
            string gameRoot = Path.Combine(root, "Game");
            string executable = Path.Combine(gameRoot, "bin", "game.exe");
            File.WriteAllText(executable, "Non-executable metadata-link fixture only.");
            var installer = new GameAction { Name = "Install", Path = "backup.iso", Type = GameActionType.File, Arguments = "keep installer args" };
            var game = new Game { Name = "Already installed fixture", Playtime = 1234, CoverImage = "keep-cover.png", PluginId = Guid.NewGuid(),
                GameActions = new ObservableCollection<GameAction> { installer } };
            Guid id = game.Id;
            Guid plugin = game.PluginId;
            InstalledGameLinker.Link(game, gameRoot, executable);
            Check(game.Id == id && game.PluginId == plugin, "same game and source owner");
            Check(game.IsInstalled && !game.IsInstalling && game.InstallDirectory == gameRoot, "existing installation linked");
            Check(game.Playtime == 1234 && game.CoverImage == "keep-cover.png" && game.Name == "Already installed fixture", "stats and artwork retained");
            Check(ReferenceEquals(game.GameActions[0], installer) && installer.Arguments == "keep installer args", "original source installer retained");
            Check(game.GameActions.Count == 2 && game.GameActions[1].IsPlayAction && game.GameActions[1].Path == executable, "launch action added");
            Check(game.GameActions[1].WorkingDir == Path.GetDirectoryName(executable), "nested executable working folder");
            InstalledGameLinker.Link(game, gameRoot, executable);
            Check(game.GameActions.Count == 2, "relink does not duplicate action");
            game.GameActions[1].Path = "{InstallDir}\\bin\\game.exe";
            game.GameActions[1].Arguments = "keep custom launch args";
            InstalledGameLinker.Link(game, gameRoot, executable);
            Check(game.GameActions.Count == 2 && game.GameActions[1].Arguments == "keep custom launch args", "variable launch path recognized");
            foreach (string invalid in new[] { Path.Combine(root, "outside.exe"), Path.Combine(gameRoot, "missing.exe"), Path.Combine(gameRoot, "notes.txt") })
            {
                File.WriteAllText(Path.Combine(gameRoot, "notes.txt"), "fixture");
                File.WriteAllText(Path.Combine(root, "outside.exe"), "fixture");
                bool rejected = false;
                try { InstalledGameLinker.Link(game, gameRoot, invalid); } catch (ArgumentException) { rejected = true; }
                Check(rejected && game.GameActions.Count == 2 && game.InstallDirectory == gameRoot && game.Playtime == 1234, "invalid selection has no mutation");
            }
            bool rootRejected = false;
            try { InstalledGameLinker.Link(game, Path.GetPathRoot(root), executable); } catch (ArgumentException) { rootRejected = true; }
            Check(rootRejected && game.InstallDirectory == gameRoot, "drive root refused before trimming");
            string siblingRoot = gameRoot + "-other";
            Directory.CreateDirectory(siblingRoot);
            string siblingExe = Path.Combine(siblingRoot, "game.exe");
            File.WriteAllText(siblingExe, "fixture");
            var originalActions = game.GameActions;
            bool siblingRejected = false;
            try { InstalledGameLinker.Link(game, gameRoot, siblingExe); } catch (ArgumentException) { siblingRejected = true; }
            Check(siblingRejected && ReferenceEquals(originalActions, game.GameActions), "same-prefix sibling folder is outside selected game");
            foreach (string invalidRoot in new[] { null, "", " ", Path.Combine(root, "absent") })
            {
                bool rejected = false;
                try { InstalledGameLinker.Link(game, invalidRoot, executable); } catch (ArgumentException) { rejected = true; }
                Check(rejected && ReferenceEquals(originalActions, game.GameActions), "invalid folder rejected before mutation");
            }
            foreach (string invalidExe in new[] { null, "", " " })
            {
                bool rejected = false;
                try { InstalledGameLinker.Link(game, gameRoot, invalidExe); } catch (ArgumentException) { rejected = true; }
                Check(rejected && ReferenceEquals(originalActions, game.GameActions), "empty executable rejected before mutation");
            }
            bool nullGameRejected = false;
            try { InstalledGameLinker.Link(null, gameRoot, executable); } catch (ArgumentNullException) { nullGameRejected = true; }
            Check(nullGameRejected, "missing game rejected");
            var emptyGame = new Game { IsInstalling = true, GameActions = null };
            InstalledGameLinker.Link(emptyGame, gameRoot, executable);
            Check(emptyGame.IsInstalled && !emptyGame.IsInstalling && emptyGame.GameActions.Count == 1 && emptyGame.GameActions[0].IsPlayAction,
                "game without existing actions can be linked");
            Check(File.ReadAllText(executable) == "Non-executable metadata-link fixture only.", "no installer or game content changed");
            Console.WriteLine("PASS " + checks + " real-SDK installation-link checks");
            return 0;
        }
        catch (Exception e) { Console.WriteLine("FAIL after " + checks + " checks: " + e.Message); return 1; }
        finally { Directory.Delete(root, true); }
    }
}
