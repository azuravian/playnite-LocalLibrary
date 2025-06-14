using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using API = Playnite.SDK.API;

namespace LocalLibrary
{
    public class LocalLibrary : LibraryPlugin
    {
        private static readonly string iconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png");

        private static readonly ILogger logger = LogManager.GetLogger();

        public LocalLibrarySettingsViewModel Settings { get; set; }

        public LocalLibrarySettingsView SettingsView { get; private set; }

        public override Guid Id { get; } = Guid.Parse("2d01017d-024e-444d-80d3-f62f5be3fca5");

        public override string Name => "Local Library";

        public override string LibraryIcon => iconPath;

        public LocalLibrary(IPlayniteAPI api) : base(api)
        {
            Settings = new LocalLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasCustomizedGameImport = true,
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new LocalLibrarySettingsView();
        }

        public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
            {
                yield break;
            }

            yield return new LocalInstallController(args.Game, this);
        }

        public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
            {
                yield break;
            }

            yield return new LocalUninstallController(args.Game, this);
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (Settings.Settings.AutoUpdate == true)
            {
                ObservableCollection<GameSourceOption> sources = Settings.Settings.SelectedSources;
                PluginIdUpdate(sources);
            }
        }

        public override IEnumerable<Game> ImportGames(LibraryImportGamesArgs args)
        {
            List<Game> addedGames = new List<Game>();
            if (Settings.Settings.UsePaths)
            {
                Finder addGames = new Finder();
                var installPaths = Settings.Settings.InstallPaths;
                var ignorelist = Settings.Settings.RegexList.Select(item => new MergedItem { Value = item, Source = "Regex" })
                    .Concat(Settings.Settings.StringList.Select(item => new MergedItem { Value = item, Source = "String" }))
                    .ToList();
                addedGames = addGames.FindInstallers(installPaths.ToList(), Settings.Settings.UseActions, Settings.Settings.Levenshtein, Settings.Settings.SelectedSources, Settings.Settings.SelectedPlatform, ignorelist, Settings.Settings.FindUpdates);
            }
            return addedGames;
        }

        public static void PluginIdUpdate(ObservableCollection<GameSourceOption> sources)
        {
            Guid Id = Guid.Parse("2d01017d-024e-444d-80d3-f62f5be3fca5");
            IEnumerable<Game> games = API.Instance.Database.Games;

            GlobalProgressOptions globalProgressOptions1 = new GlobalProgressOptions(
                            $"Local Library - Applying Plugin ID...",
                            true
                        );
            GlobalProgressOptions globalProgressOptions = globalProgressOptions1;
            globalProgressOptions.IsIndeterminate = false;

            API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    activateGlobalProgress.ProgressMaxValue = games.Count();
                    string CancelText = string.Empty;
                    List<Game> gamesUpdated = new List<Game>();

                    foreach (Game game in games)
                    {
                        if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                        {
                            CancelText = " canceled";
                            break;
                        }

                        if (game.PluginId == Guid.Empty && game.Source != null && sources.Any(source => source.Name == game.Source.ToString()))
                        {
                            game.PluginId = Id;
                            API.Instance.Database.Games.Update(game);
                        }

                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"Task ApplyPluginId(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)games.Count()} items");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, ex.ToString());
                }
            }, globalProgressOptions);
        }

        //Prompt user for installation location and create Play action
        public void GameSelect(Game selectedGame, LocalInstallController install)
        {
            string gameExe = API.Instance.Dialogs.SelectFile("Game Executable|*.exe").Replace(selectedGame.Name, "{Name}");

            if (!string.IsNullOrEmpty(gameExe))
            {
                GameAction action = new GameAction();

                try
                {
                    action.Type = GameActionType.File;
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
                action.Path = Path.GetFileName(gameExe);
                action.WorkingDir = "{InstallDir}";
                action.Name = "Play";
                action.TrackingMode = TrackingMode.Default;
                action.IsPlayAction = true;

                if (selectedGame.GameActions == null)
                {
                    selectedGame.GameActions = new System.Collections.ObjectModel.ObservableCollection<GameAction>();
                }
                try
                {
                    selectedGame.GameActions.AddMissing(action);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                    API.Instance.Dialogs.ShowErrorMessage("There was an error creating the Game Action.  Please check the Playnite log for details.", "Action Failed");
                    return;
                }
                try
                {
                    List<GameAction> gameActions = selectedGame.GameActions.ToList();
                    foreach (GameAction g in gameActions)
                    {
                        if (g.Name == "Play")
                        {
                            selectedGame.IsInstalled = true;
                            selectedGame.IsInstalling = false;
                            selectedGame.InstallDirectory = Path.GetDirectoryName(gameExe);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                    API.Instance.Dialogs.ShowErrorMessage("There was an error marking the game as installed.  Please check the Playnite log for details.", "Action Failed");
                    return;
                }
                API.Instance.Database.Games.Update(selectedGame);
            }
            else
            {
                selectedGame.IsInstalling = false;
                selectedGame.InstallDirectory = null;
                selectedGame.IsInstalled = false;
                API.Instance.Database.Games.Update(selectedGame);
            }
            install.Dispose();
        }

        private void ProcessEnded(object sender, EventArgs e)
        {
            if (sender is Process process)
            {
                _ = process.ExitCode;
            }
        }

        public string GetArchiveCommand(string gameImagePath, string gameInstallArgs)
        {
            var response = MessageBox.Show("The installer path points to an archive.  Would you like to select a folder to extract the archive to?", "Archive Detected", MessageBoxButton.YesNo);
            if (response == MessageBoxResult.No)
            {
                return "failed";
            }
            string extractpath = API.Instance.Dialogs.SelectFolder();
            if (Settings.Settings.RB7z)
            {
                gameInstallArgs = " x -o" + String.Concat("\"", extractpath, "\"") + " " + String.Concat("\"", gameImagePath, "\"");
            }
            else if (Settings.Settings.RBRar)
            {
                gameInstallArgs = " x " + String.Concat("\"", gameImagePath, "\"") + " -op" + String.Concat("\"", extractpath, "\"");
            }
            return gameInstallArgs;
        }

        public void GameInstaller(Game game, LocalInstallController install)
        {
            Game selectedGame = game;

            int code = 0;
            bool failed = false;
            string exce = "";
            string gameImagePath = null;
            string gameInstallArgs = null;
            List<GameAction> gameActions = null;
            List<GameRom> gameRoms = null;
            List<string> driveList = new List<string>();
            List<string> driveList2 = new List<string>();
            string command = null;
            string driveLetter = null;
            string[] archives = { ".7z", ".rar", ".zip" };
            string[] executables = { ".exe", ".msi" };
            bool archive = false;
            bool actions = Settings.Settings.UseActions;

            if (actions)
            {
                try
                {
                    Finder actionsFinder = new Finder();
                    Tuple<string, string, List<GameAction>> actionsTuple = actionsFinder.GetActions(game);
                    gameImagePath = actionsTuple.Item1;
                    gameInstallArgs = actionsTuple.Item2;
                    gameActions = actionsTuple.Item3;

                    GameAction found = gameActions.FirstOrDefault(a => a.Path == gameImagePath);
                    if (found != null)
                    {
                        gameActions.Remove(found);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }
            else
            {
                try
                {
                    Finder romsFinder = new Finder();
                    Tuple<string, string, List<GameRom>> romsTuple = romsFinder.GetRoms(game);
                    gameImagePath = romsTuple.Item1;
                    gameInstallArgs = romsTuple.Item2;
                    gameRoms = romsTuple.Item3;

                    GameRom found = gameRoms.FirstOrDefault(a => a.Path == gameImagePath);
                    if (found != null)
                    {
                        gameRoms.Remove(found);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            if (String.IsNullOrEmpty(gameImagePath))
            {
                var response = MessageBox.Show("The installation path is empty.\nDo you want to specify the location of the installation media?", "No Installation Path", MessageBoxButton.YesNo);
                if (response == MessageBoxResult.Yes)
                {
                    gameImagePath = API.Instance.Dialogs.SelectFolder();
                }
            }
            else
            {
                if (Path.GetFileName(gameImagePath).ToLower().EndsWith(".iso"))
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        driveList.Add(drive.Name);
                    }
                    PowerShell mountedDisk = PowerShell.Create();
                    {
                        mountedDisk.AddCommand("Mount-DiskImage");
                        mountedDisk.AddArgument(gameImagePath);
                        mountedDisk.Invoke();
                    }
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        driveList2.Add(drive.Name);
                    }
                    foreach (var i in driveList2)
                    {
                        if (driveList.Contains(i))
                        {
                            continue;
                        }
                        else
                        {
                            command = i + "\\Setup.exe";
                            driveLetter = i;
                        }
                    }
                }
                else if (executables.Any(x => gameImagePath.EndsWith(x)))
                {
                    command = gameImagePath;
                }
                else if (archives.Any(x => gameImagePath.EndsWith(x)))
                {
                    archive = true;
                    command = Settings.Settings.ArchivePath;
                    gameInstallArgs = GetArchiveCommand(gameImagePath, gameInstallArgs);
                    if (gameInstallArgs == "failed")
                    {
                        API.Instance.Dialogs.ShowErrorMessage("Installing a game stored as an archive requires that you select a folder to extract to.", "Install Canceled");
                        selectedGame.IsInstalling = false;
                        API.Instance.Database.Games.Update(selectedGame);
                        return;
                    }
                }
                else if (!Path.HasExtension(gameImagePath))
                {
                    if (!Directory.Exists(gameImagePath))
                    {
                        API.Instance.Dialogs.ShowErrorMessage("The file/folder specified in the installation path does not exist.", "Invalid Path");
                        return;
                    }
                    string setupFile = Path.Combine(gameImagePath, "setup.exe");
                    if (File.Exists(setupFile))
                    {
                        command = setupFile;
                    }
                    else
                    {
                        List<string> Files = Directory.GetFiles(gameImagePath, "*.*")
                                                  .Where(file => file.ToLower().EndsWith(".exe") || file.ToLower().EndsWith(".msi")).ToList();

                        if (Files.Count() > 1)
                        {
                            MessageBoxResult result = MessageBox.Show("More than 1 possible installer in folder.  Would you like to select the appropriate installer?", "Too many executables", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                command = API.Instance.Dialogs.SelectFile("All Executables|*.exe;*.msi|" + "Installer|*.exe|Installer|*.msi");
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (Files.Count().ToString() == "0")
                        {
                            API.Instance.Dialogs.ShowErrorMessage("No executables found in folder.  Check  Path.", "No Executables.");
                            return;
                        }
                        else
                        {
                            command = Files[0];
                        }
                    }
                }
                else
                {
                    API.Instance.Dialogs.ShowErrorMessage("The provided Rom file has an invalid extension. Please provide valid file or directory.", "Invalid filetype.");
                    return;
                }
            }
            if (!File.Exists(command) && !archive)
            {
                MessageBoxResult result = MessageBox.Show("Setup.exe was not found in your ISO.  Would you like to select the appropriate .exe?", "Setup.exe not found", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    command = API.Instance.Dialogs.SelectFile("Installer|*.exe");
                }
                else
                {
                    return; //
                }
            }
            try
            {
                using (Process p = new Process())
                {
                    String dpath = "";
                    p.StartInfo.UseShellExecute = true;

                    if (archive)
                    {
                        p.StartInfo.UseShellExecute = false;
                    }

                    if (Path.GetExtension(command).Equals(".msi", StringComparison.OrdinalIgnoreCase))
                    {
                        p.StartInfo.FileName = "msiexec.exe";
                        p.StartInfo.Arguments = $"/i \"{command}\" {gameInstallArgs}";
                    }
                    else
                    {
                        p.StartInfo.FileName = command;
                        if (gameInstallArgs != null)
                        {
                            p.StartInfo.Arguments = gameInstallArgs;
                        }
                    }

                    if (driveLetter != null)
                    {
                        dpath = driveLetter;
                    }
                    else if (Path.HasExtension(gameImagePath))
                    {
                        dpath = Path.GetDirectoryName(gameImagePath);
                    }
                    else
                    {
                        dpath = gameImagePath;
                    }

                    p.StartInfo.WorkingDirectory = dpath;
                    p.StartInfo.Verb = "runas";
                    p.Start();
                    p.WaitForExit();
                    code = p.ExitCode;
                }
            }
            catch (Exception ex)
            {
                exce = ex.Message;
            }
            if (gameImagePath.ToLower().EndsWith(".iso"))
            {
                PowerShell dismountDisk = PowerShell.Create();
                {
                    dismountDisk.AddCommand("Dismount-DiskImage");
                    dismountDisk.AddArgument(gameImagePath);
                    dismountDisk.Invoke();
                }
            }
            if (code != 0 || exce != "")
            {
                failed = true;
                MessageBoxResult result = MessageBox.Show("The installation was either canceled or failed.  Do you want to continue processing this installation?", "Installation canceled/failed", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    selectedGame.IsInstalling = false;
                    selectedGame.InstallDirectory = null;
                    selectedGame.IsInstalled = false;
                    API.Instance.Database.Games.Update(selectedGame);
                    install.Dispose();
                    return;
                }
            }
            if (Settings.Settings.UseActions && gameActions.Count > 0)
            {
                failed = Install_Extras(gameActions, selectedGame, install);
            }
            else if (!Settings.Settings.UseActions && gameRoms.Count > 0)
            {
                failed = Install_Extras(gameRoms, selectedGame, install);
            }
            if (!failed)
            {
                GameSelect(selectedGame, install);
            }
            return;
        }

        private void Delete_PlayActions(IEnumerable<GameAction> actions, Game selectedGame)
        {
            if (actions != null)
            {
                try
                {
                    foreach (var action in actions.ToList())
                    {
                        if (action.Name == "Play")
                        {
                            selectedGame.GameActions.Remove(action);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                    API.Instance.Dialogs.ShowErrorMessage("The game was uninstalled, but there was an error removing the Play Action or no Play Action was found.\n\nYou will need to manually remove any Play actions associated with this game.  Please check the log for details.", "Action Failed");
                }
            }
        }

        public bool Install_Extras(List<GameRom> extras, Game selectedGame, LocalInstallController install)
        {
            var failed = false;
            foreach (GameRom extra in extras)
            {
                int code = 0;
                string exce = "";
                string command = null;
                string extraInstallArgs = null;
                var extraPath = extra.Path;

                if (Path.GetFileName(extraPath).EndsWith(".exe"))
                {
                    command = extraPath;
                }
                else
                {
                    continue;
                }
                try
                {
                    using (Process p = new Process())
                    {
                        String dpath = "";
                        p.StartInfo.FileName = command;
                        p.StartInfo.UseShellExecute = true;
                        if (extraInstallArgs != null)
                        {
                            p.StartInfo.Arguments = extraInstallArgs;
                        }
                        dpath = Path.GetDirectoryName(extraPath);

                        p.StartInfo.WorkingDirectory = dpath;
                        p.StartInfo.Verb = "runas";
                        p.Start();
                        p.WaitForExit();
                        code = p.ExitCode;
                    }
                }
                catch (Exception ex)
                {
                    exce = ex.Message;
                }
                if (code != 0 || exce != "")
                {
                    MessageBoxResult result = MessageBox.Show("The installation was either canceled or failed.  Do you want to continue processing this installation?", "Installation canceled/failed", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        selectedGame.IsInstalling = false;
                        selectedGame.InstallDirectory = null;
                        selectedGame.IsInstalled = false;
                        API.Instance.Database.Games.Update(selectedGame);
                        install.Dispose();
                        return true;
                    }
                    failed = false;
                }
                failed = false;
            }
            return failed;
        }

        public bool Install_Extras(List<GameAction> extras, Game selectedGame, LocalInstallController install)
        {
            var failed = false;
            foreach (GameAction extra in extras)
            {
                int code = 0;
                string exce = "";
                string command = null;
                string extraInstallArgs = null;
                var extraPath = extra.Path;
                if (Path.GetFileName(extraPath).EndsWith(".exe"))
                {
                    command = extraPath;
                }
                try
                {
                    using (Process p = new Process())
                    {
                        String dpath = "";
                        p.StartInfo.FileName = command;
                        p.StartInfo.UseShellExecute = true;
                        if (extraInstallArgs != null)
                        {
                            p.StartInfo.Arguments = extraInstallArgs;
                        }
                        dpath = Path.GetDirectoryName(extraPath);

                        p.StartInfo.WorkingDirectory = dpath;
                        p.StartInfo.Verb = "runas";
                        p.Start();
                        p.WaitForExit();
                        code = p.ExitCode;
                    }
                }
                catch (Exception ex)
                {
                    exce = ex.Message;
                }
                if (code != 0 || exce != "")
                {
                    MessageBoxResult result = MessageBox.Show("The installation was either canceled or failed.  Do you want to continue processing this installation?", "Installation canceled/failed", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        selectedGame.IsInstalling = false;
                        selectedGame.InstallDirectory = null;
                        selectedGame.IsInstalled = false;
                        API.Instance.Database.Games.Update(selectedGame);
                        install.Dispose();
                        return true;
                    }
                    failed = false;
                }
                failed = false;
            }
            return failed;
        }

        public void GameUninstaller(Game game, LocalUninstallController uninstall)
        {
            int code = 0;
            string exce = "";
            Game selectedGame = game;
            var actions =
                from action in selectedGame.GameActions
                where action.Name == "Play"
                && action.IsPlayAction == true
                select action;
            if (selectedGame != null)
            {
                string uninstaller = "";
                string installDir = selectedGame.InstallDirectory.Replace("{Name}", selectedGame.Name);
                if (Directory.Exists(installDir))
                {
                    string[] idFiles = Directory.GetFiles(installDir, "*unins*", SearchOption.AllDirectories);
                    foreach (string idFile in idFiles)
                    {
                        if (idFile.ToLower().Contains("uninstall.bat"))
                        {
                            uninstaller = idFile;
                            break;
                        }
                        else if (idFile.ToLower().Contains("uninstall.exe"))
                        {
                            uninstaller = idFile;
                            break;
                        }
                        else if (idFile.ToLower().Contains("unins000.exe"))
                        {
                            uninstaller = idFile;
                            break;
                        }
                    }
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("The Install Directory specified in Playnite could not be found.  Would you like to continue processing this game?", "Install Directory not found", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                if (uninstaller == "")
                {
                    MessageBoxResult result = MessageBox.Show("No uninstaller could be found in the installation directory.  Would you like to select the uninstaller?", "Uninstaller not found", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        uninstaller = API.Instance.Dialogs.SelectFile("Uninstaller|*.exe");
                    }
                    else
                    {
                        MessageBoxResult result2 = MessageBox.Show("No uninstaller is available.  Would you like to delete the games installation folder?\n\nInstallation Folder: " + installDir + "\n\nWarning: This process is not able to be reversed.", "Uninstaller not found", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result2 == MessageBoxResult.Yes)
                        {
                            string unmessage = "";
                            var dir = new DirectoryInfo(installDir);
                            dir.Delete(true);
                            if (Settings.Settings.RemovePlay)
                            {
                                Delete_PlayActions(actions, selectedGame);
                                unmessage = "The installation folder was successfully removed.  The game has had its play action(s) removed and is marked as uninstalled.";
                            }
                            else
                            {
                                unmessage = "The installation folder was successfully removed and the game has been marked as uninstalled.";
                            }
                            selectedGame.InstallDirectory = null;
                            selectedGame.IsInstalled = false;
                            API.Instance.Database.Games.Update(selectedGame);

                            API.Instance.Dialogs.ShowMessage(unmessage, "Uninstall Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                            uninstall.Dispose();
                            return;
                        }
                        else
                        {
                            API.Instance.Dialogs.ShowErrorMessage("The game was not uninstalled.  No changes have been made.", "Game not Uninstalled");
                            game.IsUninstalling = false;
                            API.Instance.Database.Games.Update(selectedGame);
                            uninstall.Dispose();
                            return;
                        }
                    }
                }

                try
                {
                    using (Process p = new Process())
                    {
                        String dpath = "";
                        dpath = Path.GetDirectoryName(uninstaller);
                        p.StartInfo.FileName = uninstaller;
                        p.StartInfo.UseShellExecute = true;
                        p.StartInfo.WorkingDirectory = dpath;
                        p.StartInfo.Verb = "runas";
                        p.Start();
                        p.WaitForExit();
                        code = p.ExitCode;
                    }
                }
                catch (Exception ex)
                {
                    exce = ex.Message;
                }

                if (code != 0 || exce != "")
                {
                    API.Instance.Dialogs.ShowErrorMessage("The uninstall process was either canceled or failed.  The game was not uninstalled.  No changes have been made.", "Game not Uninstalled");
                    game.IsUninstalling = false;
                    API.Instance.Database.Games.Update(selectedGame);
                    uninstall.Dispose();
                    return;
                }
                if (Settings.Settings.RemovePlay)
                {
                    Delete_PlayActions(actions, selectedGame);
                }
                selectedGame.InstallDirectory = null;
                selectedGame.IsInstalled = false;
                API.Instance.Database.Games.Update(selectedGame);
                uninstall.Dispose();
            }
        }

    }
}