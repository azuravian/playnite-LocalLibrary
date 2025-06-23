using Playnite.SDK;

using Playnite.SDK.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using API = Playnite.SDK.API;

namespace LocalLibrary
{
    internal class Finder
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public List<string> GetDirectories(string parentDir)
        {
            List<string> dirs = new List<string>();
            //get parent directory with parameter of method
            DirectoryInfo di = new DirectoryInfo(parentDir);
            //get directories that match in the parent directory
            DirectoryInfo[] directories =
                di.GetDirectories("*", SearchOption.TopDirectoryOnly);
            //iterate the parent directory matching directories and add to list
            foreach (DirectoryInfo dinfo in directories)
            {
                dirs.Add(dinfo.FullName);
            }
            return dirs;
        }

        public string GetDeepestDirectory(string path)
        {
            path = path.Replace(":", "");
            if (File.Exists(path))
            {
                // If it's a file, get its directory
                path = Path.GetDirectoryName(path);
            }
            // Get the last part of the directory path
            return new DirectoryInfo(path).Name;
        }

        // Combined method to get actions or ROMs based on the game and whether actions are requested
        public Tuple<string, string, List<Dictionary<String, String>>> GetActionsRoms(Game game, bool actions)
        {
            string gameImagePath = null;
            string gameInstallArgs = null;
            List<GameAction> gameActions = new List<GameAction>();
            List<GameRom> gameRoms = new List<GameRom>();
            List<Dictionary<String, String>> installers = new List<Dictionary<String, String>>();
            if (actions && game.GameActions != null)
            {
                gameActions = game.GameActions.ToList();
                foreach (GameAction ga in gameActions)
                {
                    if (gameImagePath == null && (ga.Name == "Install" || ga.Name == "Installer"))
                    {
                        gameImagePath = API.Instance.ExpandGameVariables(game, ga).Path;
                        gameInstallArgs = API.Instance.ExpandGameVariables(game, ga).Arguments;
                    }
                    else
                    {
                        Dictionary<String, String> installDict = new Dictionary<String, String>
                        {
                            { "Path", API.Instance.ExpandGameVariables(game, ga).Path },
                            { "InstallArgs", API.Instance.ExpandGameVariables(game, ga).Arguments }
                        };
                        installers.Add(installDict);
                    }
                }
                if (String.IsNullOrEmpty(gameImagePath) && gameActions.Count > 0)
                {
                    gameImagePath = API.Instance.ExpandGameVariables(game, gameActions[0]).Path;
                    installers.Remove(installers.FirstOrDefault(a => a["Path"] == gameImagePath));
                }
            }
            else if (!actions && game.Roms != null)
            {
                gameRoms = game.Roms.ToList();
                foreach (GameRom gr in gameRoms)
                {
                    //Take the ROM with the name Install or Installer and use it as the installer
                    if (gameImagePath == null && (gr.Name == "Install" || gr.Name == "Installer"))
                    {
                        gameImagePath = gr.Path;
                    }
                    else
                    {
                        Dictionary<String, String> installDict = new Dictionary<String, String>
                        {
                            { "Path", gr.Path },
                            { "InstallArgs", null }
                        };
                        installers.Add(installDict);
                    }
                }
                if (String.IsNullOrEmpty(gameImagePath) && gameRoms.Count > 0)
                {
                    //If no Install or Installer ROM is found, use the first ROM in the list
                    gameImagePath = gameRoms[0].Path;
                    installers.Remove(installers.FirstOrDefault(a => a["Path"] == gameImagePath));
                }
            }
            if (String.IsNullOrEmpty(gameImagePath))
            {
                gameImagePath = API.Instance.ExpandGameVariables(game, game.InstallDirectory);
            }
            return Tuple.Create(gameImagePath, gameInstallArgs, installers);
        }

        public string GetMainInstaller(string dir)
        {
            string gameInstaller = "";
            List<string> validExt = new List<string> { ".iso", ".rar", ".zip", ".7z" };
            List<string> prefExt = new List<string> { ".exe", ".msi", ".bat", ".ps", ".ps1" };
            List<string> dirFiles = Directory.GetFiles(dir).ToList();
            foreach (string file in dirFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                if (prefExt.Contains(Path.GetExtension(file).ToLower()))
                {
                    // Check for common installer names
                    if (fileName == "setup" || fileName == "install")
                    {
                        gameInstaller = file;
                        break;
                    }
                }
                else if (prefExt.Contains(Path.GetExtension(file).ToLower()))
                {
                    if (fileName.Contains("setup") || fileName.Contains("install"))
                    {
                        gameInstaller = file;
                        break;
                    }
                }
                else if (validExt.Contains(Path.GetExtension(file)))
                {
                    gameInstaller = file;
                }
            }
            return gameInstaller;
        }

        public List<Game> AddGame(List<Game> gamesAdded, string dir, bool useActions, Guid source, string platform, List<MergedItem> ignorelist)
        {
            string gamename = Path.GetFileName(dir);
            string gameInstaller = GetMainInstaller(dir);
            if (gameInstaller == "")
            {
                return gamesAdded;
            }
            if (ignorelist != null)
            {
                foreach (MergedItem item in ignorelist)
                {
                    var type = item.Source;
                    var value = item.Value;

                    if (type == "String")
                    {
                        if (gamename.Contains(value))
                        {
                            gamename = gamename.Replace(value, "");
                        }
                    }
                    else if (type == "Regex")
                    {
                        if (Regex.IsMatch(gamename, value))
                        {
                            gamename = Regex.Replace(gamename, value, "");
                        }
                    }
                    gamename = Regex.Replace(gamename, @"\s+", " ").Trim();
                }
            }

            IEnumerable<Game> games = API.Instance.Database.Games
                    ?.Where(game => game != null && game.Source != null && game.Source.Id == source)
                    ?? Enumerable.Empty<Game>();
            var matchingGame = games.FirstOrDefault(game => game.Name.Replace(": ", " - ") == gamename);
            if (matchingGame != null)
            {
                if (useActions)
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
                    action.Path = gameInstaller;
                    action.Name = "Install";
                    action.TrackingMode = TrackingMode.Default;
                    action.IsPlayAction = false;
                    matchingGame.GameActions = new ObservableCollection<GameAction>();
                    matchingGame.GameActions.AddMissing(action);
                    API.Instance.Database.Games.Update(matchingGame);
                }
                else
                {
                    GameRom installRom = new GameRom
                    {
                        Name = "Install",
                        Path = gameInstaller
                    };
                    matchingGame.Roms = new ObservableCollection<GameRom>();
                    matchingGame.Roms.AddMissing(installRom);
                    API.Instance.Database.Games.Update(matchingGame);
                }

                return gamesAdded;
            }

            Game newGame = new Game
            {
                Name = gamename,
                Added = DateTime.Now,
                PluginId = Guid.Parse("2d01017d-024e-444d-80d3-f62f5be3fca5"),
                SourceId = source,
                PlatformIds = new List<Guid> { API.Instance.Database.Platforms.FirstOrDefault(a => a.Name == platform)?.Id ?? Guid.Empty }
            };

            if (useActions)
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
                action.Path = gameInstaller;
                action.Name = "Install";
                action.TrackingMode = TrackingMode.Default;
                action.IsPlayAction = false;
                newGame.GameActions = new ObservableCollection<GameAction>();
                newGame.GameActions.AddMissing(action);
            }
            else
            {
                GameRom installRom = new GameRom
                {
                    Name = "Install",
                    Path = gameInstaller
                };
                newGame.Roms = new ObservableCollection<GameRom>();
                newGame.Roms.AddMissing(installRom);
            }
            gamesAdded.Add(newGame);
            return gamesAdded;
        }

        public int FindGameUpdates(Game game, string dir, bool useActions)
        {
            var gameUpdates = new List<Dictionary<string, string>>();
            var updatesPath = Path.Combine(dir, "Updates");
            if (!Directory.Exists(updatesPath))
            {
                return 0;
            }

            // Get all top-level files and directories
            var entries = Directory.GetFileSystemEntries(updatesPath).OrderBy(Path.GetFileName);

            // Filter out invalid actions
            if (useActions)
            {
                gameUpdates = game.GameActions
                    .Where(action => File.Exists(action.Path) || Directory.Exists(action.Path))
                    .Select(action => new Dictionary<string, string>
                    {
                        { "Path", action.Path },
                        { "Name", action.Name }
                    })
                    .ToList();
            }
            else
            {
                gameUpdates = game.Roms
                    .Where(rom => File.Exists(rom.Path) || Directory.Exists(rom.Path))
                    .Select(rom => new Dictionary<string, string>
                    {
                        { "Path", rom.Path },
                        { "Name", rom.Name }
                    })
                    .ToList();
            }

            var preferredFilenames = new[] { "patch", "setup", "install" };   
            var extensions = new[] { ".exe", ".msi", ".bat", ".ps", ".ps1" };

            foreach (var entry in entries)
            {
                string entryName = string.Empty;

                if (game.GameActions?.Any(action => action.Path == entry) == true
                                || game.Roms?.Any(rom => rom.Path == entry) == true)
                {
                    continue;
                }

                if (File.Exists(entry))
                {
                    var newentry = new Dictionary<string, string> { { "Path", entry }, { "Name", "" } };
                    gameUpdates.Add(newentry);
                }
                else if (Directory.Exists(entry))
                {
                    var validExecutable = Directory
                        .EnumerateFiles(entry, "*", SearchOption.AllDirectories)
                        .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                        .FirstOrDefault(file =>
                            preferredFilenames.Contains(
                                Path.GetFileNameWithoutExtension(file),
                                StringComparer.OrdinalIgnoreCase)) ?? Directory.GetFiles(entry, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (validExecutable != null)
                    {
                        var newentry = new Dictionary<string, string> { { "Path", validExecutable }, { "Name", "" } };
                        gameUpdates.Add(newentry);
                    }
                }
            }
            // Sort and rename updates
            var seenPaths = new HashSet<string>();
            gameUpdates = gameUpdates
                .Where(update => seenPaths.Add(update["Path"].ToString()))  // Adds path if not already in HashSet
                .ToList();


            gameUpdates = gameUpdates
                .OrderBy(update => update["Name"].StartsWith("install", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(update => update["Path"])
                .ToList();

            for (int i = 1; i < gameUpdates.Count; i++)
            {
                gameUpdates[i]["Name"] = $"Update {i}";
            }

            if (useActions)
            {
                var gameActions = gameUpdates.Select(update => new GameAction
                {
                    Name = update["Name"],
                    Path = update["Path"],
                    Type = GameActionType.File,
                    TrackingMode = TrackingMode.Default,
                    IsPlayAction = false
                });
                game.GameActions = new ObservableCollection<GameAction>(gameActions);
            }
            else
            {
                var gameRoms = gameUpdates.Select(update => new GameRom
                {
                    Name = update["Name"],
                    Path = update["Path"]
                });
                game.Roms = new ObservableCollection<GameRom>(gameRoms);
            }
            return gameUpdates.Count - 1;
        }

        public List<Game> FindInstallers(List<string> installPaths, bool useActions, int lpercent, ObservableCollection<GameSourceOption> sources, string platform, List<MergedItem> ignorelist, bool findupdates)
        {
            List<ReportItem> reportItems = new List<ReportItem>();
            List<Game> NoItems = new List<Game>();
            int totalupdates = 0;
            Guid primarysourceid = sources.FirstOrDefault(a => a.IsPrimary)?.Id ?? Guid.Empty;
            if (primarysourceid == Guid.Empty)
            {
                API.Instance.Dialogs.ShowMessage("You must designate a Source as the primary source in order to find and add new games. Open the Local Library addon settings and select a primary source.", "No Primary Source selected");
            }
            IEnumerable<Game> games = API.Instance.Database.Games;
            List<string> gameInstallDirs = new List<string>();

            foreach (Game game in games)
            {
                if (!sources.Any(source => source.Name == (game.Source?.ToString() ?? "")))
                {
                    continue;
                }

                string gameImagePath = GetActionsRoms(game, useActions).Item1;
                if (String.IsNullOrEmpty(gameImagePath))
                {
                    NoItems.Add(game);
                    continue;
                }
                List<string> exts = new List<string> { ".exe", ".iso", ".rar", ".zip", ".7z", ".bat", ".ps", ".ps1" };
                if (exts.Contains(Path.GetExtension(gameImagePath)) || File.Exists(gameImagePath))
                {
                    gameImagePath = Path.GetDirectoryName(gameImagePath);
                }
                gameInstallDirs.Add(gameImagePath);
                if (findupdates)
                {
                    int updatescount = FindGameUpdates(game, gameImagePath, useActions);
                    totalupdates += updatescount;
                    reportItems.Add(new ReportItem(game.Name, false, updatescount > 0, updatescount));
                }
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"Local Library - Finding Installers...",
                true
            );
            globalProgressOptions.IsIndeterminate = false;
            List<Game> gamesAdded = new List<Game>();
            API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    List<string> dirsFinal = installPaths
                        .Where(Directory.Exists)
                        .SelectMany(GetDirectories)
                        .ToList();

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    activateGlobalProgress.ProgressMaxValue = dirsFinal.Count;
                    string cancelText = string.Empty;

                    foreach (string dir in dirsFinal)
                    {
                        activateGlobalProgress.CurrentProgressValue++;
                        string dirName = dir;
                        List<string> regPaths = new List<string>();
                        foreach (string p in installPaths)
                        {
                            string doubled = Regex.Replace(p, @"\\", @"\\");
                            regPaths.Add(doubled);
                        }
                        string pattern = @"(" + String.Join("|", regPaths.ToArray()) + @")([^\\]*)\\*";
                        pattern.Replace(@"\", @"\\");
                        RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
                        Regex reg = new Regex(pattern, options);
                        MatchCollection matches = reg.Matches(dir);

                        if (matches.Count == 1)
                        {
                            dirName = dir.Replace(matches[0].ToString(), "");
                        }

                        if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                        {
                            cancelText = " canceled";
                            break;
                        }

                        if (gameInstallDirs.Contains(dir))
                        {
                            continue;
                        }

                        Levenshtein myLevenshtein = new Levenshtein();
                        List<string> posmatches = new List<string>();
                        gameInstallDirs.Sort();
                        foreach (string gameInstallDir in gameInstallDirs)
                        {
                            if (String.IsNullOrEmpty(gameInstallDir))
                            {
                                continue;
                            }
                            string gameInstallDirName = GetDeepestDirectory(gameInstallDir);
                            int ldistance = myLevenshtein.Distance(dirName, gameInstallDirName);
                            float percent = 1 - (Convert.ToSingle(ldistance) / Convert.ToSingle(Math.Max(gameInstallDir.Length, dir.Length)));
                            percent = percent * 100;
                            if (percent >= lpercent)
                            {
                                posmatches.Add(gameInstallDir);
                            }
                        }
                        if (posmatches.Count() > 0)
                        {
                            if (posmatches.Count() == 1)
                            {
                                continue;
                            }
                            else
                            {
                                posmatches.Sort();
                                ObservableCollection<string> lmatches = new ObservableCollection<string>(
                                    posmatches.Select(match => match)
                                );

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    SelectionDialog dialog = new SelectionDialog(lmatches);
                                    dialog.SelectText.Text = $"Below are possible matches for {dirName}. Select a match from the list or choose 'None are correct'.";
                                    dialog.ShowDialog();
                                    if (dialog.IsCancelled)
                                    {
                                        gamesAdded = AddGame(gamesAdded, dir, useActions, primarysourceid, platform, ignorelist);
                                    }
                                });
                            }
                        }
                        else
                        {
                            gamesAdded = AddGame(gamesAdded, dir, useActions, primarysourceid, platform, ignorelist);
                        }
                    }


                    foreach (Game game in NoItems)
                    {
                        if (!gameInstallDirs.Any(dir => dir.Contains(game.Name)))
                        {
                            continue;
                        }
                    }

                    int totalNewUpdates = 0;
                    foreach (Game game in gamesAdded)
                    {
                        int updatescount =
                        FindGameUpdates(
                            game,
                            useActions && game.GameActions.Any()
                            ? Path.GetDirectoryName(game.GameActions.First().Path)
                            : game.Roms.Any()
                                ? Path.GetDirectoryName(game.Roms.First().Path)
                                : string.Empty,
                            useActions);
                        totalNewUpdates += updatescount;
                        reportItems.Add(new ReportItem(game.Name, true, updatescount > 0, updatescount));
                    }

                    API.Instance.Database.Games.Add(gamesAdded);
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"Task FindInstallers(){cancelText} - {ts:mm\\:ss\\.ff} for {activateGlobalProgress.CurrentProgressValue}/{dirsFinal.Count} items");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, ex.ToString());
                }
            }, globalProgressOptions);
            return gamesAdded;
        }
    }
}
