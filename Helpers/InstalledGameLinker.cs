using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Playnite.SDK.Models;

namespace LocalLibrary.Helpers
{
    public static class InstalledGameLinker
    {
        // Metadata only: no installation, process launch, file move or deletion.
        public static void Link(Game game, string installationDirectory, string executable)
        {
            if (game == null) throw new ArgumentNullException("game");
            if (string.IsNullOrWhiteSpace(installationDirectory) || string.IsNullOrWhiteSpace(executable))
                throw new ArgumentException("Choose the installed game's folder and executable.");
            string fullRoot = Path.GetFullPath(installationDirectory);
            string root = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string file = Path.GetFullPath(executable);
            if (string.Equals(root, Path.GetPathRoot(fullRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
                !Directory.Exists(root) || new DirectoryInfo(root).Parent == null)
                throw new ArgumentException("Choose the game's folder, not a drive or share root.");
            if (!file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(file), ".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
                throw new ArgumentException("Choose an existing game executable inside that folder.");

            var actions = game.GameActions == null ? new List<GameAction>() : new List<GameAction>(game.GameActions);
            bool linked = false;
            foreach (var action in actions)
            {
                if (action == null || !action.IsPlayAction || action.Type != GameActionType.File) continue;
                string oldPath = action.Path;
                if (string.IsNullOrEmpty(oldPath)) continue;
                if (!string.IsNullOrEmpty(game.InstallDirectory)) oldPath = oldPath.Replace("{InstallDir}", game.InstallDirectory);
                if (!Path.IsPathRooted(oldPath))
                {
                    if (string.IsNullOrEmpty(game.InstallDirectory)) continue;
                    oldPath = Path.Combine(game.InstallDirectory, oldPath);
                }
                try
                {
                    if (string.Equals(Path.GetFullPath(oldPath), file, StringComparison.OrdinalIgnoreCase)) { linked = true; break; }
                }
                catch (ArgumentException) { }
                catch (NotSupportedException) { }
                catch (IOException) { }
            }
            if (!linked)
                actions.Add(new GameAction { Name = "Play", Path = file, WorkingDir = Path.GetDirectoryName(file),
                    Type = GameActionType.File, TrackingMode = TrackingMode.Default, IsPlayAction = true });
            game.GameActions = new ObservableCollection<GameAction>(actions);
            game.InstallDirectory = root;
            game.IsInstalled = true;
            game.IsInstalling = false;
        }
    }
}
