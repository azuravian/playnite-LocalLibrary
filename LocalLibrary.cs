using Playnite.SDK;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Management.Automation;
using API = Playnite.SDK.API;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Collections.ObjectModel;
using Playnite.SDK.Events;
using System.Management.Automation.Runspaces;
using System.Resources;
using System.Threading;
using System.Windows.Documents;

namespace LocalLibrary
{
    public class LocalLibrary : LibraryPlugin
    {
        private static readonly string iconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png");

        private static readonly ILogger logger = LogManager.GetLogger();

        public LocalLibrarySettingsViewModel Settings { get; set; }

        public LocalLibrarySettingsView SettingsView { get; private set; }

        public override Guid Id { get; } = Guid.Parse("9244ed08-9948-4f7b-bb26-95661e34b038");

        public override string Name => "Local Library";

        public override string LibraryIcon => iconPath;

        public LocalLibrary(IPlayniteAPI api) : base(api)
        {
            Settings = new LocalLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
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
                var source = Settings.Settings.SelectedSource;
                PluginIdUpdate(source);
            }
        }

        public static void PluginIdUpdate(string source)
        {
            Guid Id = Guid.Parse("9244ed08-9948-4f7b-bb26-95661e34b038");
            IEnumerable<Game> games = API.Instance.Database.Games;

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"Local Library - Applying Plugin ID...",
                true
            );
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

                        if (game.Source != null && game.Source.ToString() == source && game.PluginId != Id)
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

        public void GameSelect(Game selectedGame)
        {
            string gameExe = API.Instance.Dialogs.SelectFile("Game Executable|*.exe").Replace(selectedGame.Name, "{Name}");

            if (!String.IsNullOrEmpty(gameExe))
            {
                GameAction action = new GameAction();
                GameAction uaction = new GameAction();                

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
            this.Dispose();
        }

        private void ProcessEnded(object sender, EventArgs e)
        {
            var process = sender as Process;
            if (process != null)
            {
                var eCode = process.ExitCode;
            }
        }
        
        public void GameInstaller(Game game, LocalInstallController install)
        {
            Game selectedGame = game;

            string gameImagePath = null;
            string gameInstallArgs = null;
            List<string> driveList = new List<string>();
            List<string> driveList2 = new List<string>();
            string command = null;
            string driveLetter = null;
            int code;

            if (Settings.Settings.UseActions)
            {
                try
                {
                    List<GameAction> gameActions = selectedGame.GameActions.ToList();
                    try
                    {
                        foreach (GameAction g in gameActions)
                        {
                            if (g.Name == "Install")
                            {
                                gameImagePath = API.Instance.ExpandGameVariables(selectedGame, g).Path.Replace(": ", " - ");
                                gameInstallArgs = API.Instance.ExpandGameVariables(selectedGame, g).Arguments.Replace(": ", " - ");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.ToString());
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
                    {
                        var gameRoms = selectedGame.Roms.ToList();
                        gameImagePath = gameRoms[0].Path;
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
                else if (Path.GetFileName(gameImagePath).EndsWith(".exe"))
                {
                    command = gameImagePath;
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
                        String[] Files = Directory.GetFiles(gameImagePath, "*.exe");

                        if (Files.Count() > 1)
                        {
                            MessageBoxResult result = MessageBox.Show("More than 1 .exe in folder.  Would you like to select the appropriate .exe?", "Too many programs", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                command = API.Instance.Dialogs.SelectFile("Installer|*.exe");
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (Files.Count().ToString() == "0")
                        {
                            API.Instance.Dialogs.ShowErrorMessage("No executables found in folder.  Check Rom Path.", "No Executables.");
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
                    API.Instance.Dialogs.ShowErrorMessage("The provided Rom file has an invalid extension. Please provide valid iso/exe/directory.", "Invalid Executable/ISO.");
                    return;
                }
            }
            if (!File.Exists(command))
            {
                MessageBoxResult result = MessageBox.Show("Setup.exe was not found in your ISO.  Would you like to select the appropriate .exe?", "Setup.exe not found", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    command = API.Instance.Dialogs.SelectFile("Installer|*.exe");
                }
                else
                {
                    GameSelect(selectedGame);
                }
            }
            try
            {
                using (Process p = new Process())
                {
                    String dpath = "";
                    p.StartInfo.FileName = command;
                    p.StartInfo.UseShellExecute = true;
                    if (gameInstallArgs != null)
                    {
                        p.StartInfo.Arguments = gameInstallArgs;
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
            catch
            {
                return;
            }
            if (code != 0)
            {
                MessageBoxResult result = MessageBox.Show("The installation was either canceled or failed.  Do you want to continue processing this installation?", "Installation canceled/failed", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    install.Dispose();
                    return;
                }
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

            GameSelect(selectedGame);
        }

        public void GameUninstaller(Game game)
        {
            Game selectedGame = game;
            if (selectedGame != null)
            {
                string uninstaller = "";
                string installDir = selectedGame.InstallDirectory.Replace("{Name}", selectedGame.Name);
                if ( Directory.Exists(installDir) )
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
                    }
                }
                catch
                {
                    return;
                }

                var actions =
                    from action in selectedGame.GameActions
                    where action.Name == "Play"
                    && action.IsPlayAction == true
                    select action;

                if (actions != null)
                {
                    try
                    {
                        foreach (var action in actions.ToList())
                        {
                            selectedGame.GameActions.Remove(action);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.ToString());
                        API.Instance.Dialogs.ShowErrorMessage("There was an error removing the Play Action or no Play Action was found.\n\nPlease check the log for details.", "Action Failed");
                        return;
                    }
                }

                selectedGame.InstallDirectory = null;
                selectedGame.IsInstalled = false;
                API.Instance.Database.Games.Update(selectedGame);
                this.Dispose();
            }
        }

    }
}