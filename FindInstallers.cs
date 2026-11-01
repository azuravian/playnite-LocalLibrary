using LocalLibrary.Helpers;
using LocalLibrary.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
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
        public Tuple<string, string, List<Dictionary<String, String>>> GetActionsRoms(Game game, LocalLibrarySettings settings)
        {
            bool actions = settings.UseActions;
            string gameInstallerPath = null;
            string gameInstallArgs = null;
            List<GameAction> gameActions = new List<GameAction>();
            List<GameRom> gameRoms = new List<GameRom>();
            List<Dictionary<String, String>> installers = new List<Dictionary<String, String>>();
            if (actions && game.GameActions != null)
            {
                gameActions = game.GameActions.Where(a => a != null && !a.IsPlayAction && a.Type == GameActionType.File).ToList();
                foreach (GameAction ga in gameActions)
                {
                    if (UpdateSourceOrdering.IsMainName(ga.Name))
                    {
                        if (gameInstallerPath == null)
                        {
                            gameInstallerPath = API.Instance.ExpandGameVariables(game, ga).Path;
                            gameInstallArgs = API.Instance.ExpandGameVariables(game, ga).Arguments;
                        }
                        continue;
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
                if (String.IsNullOrEmpty(gameInstallerPath) && gameActions.Count > 0)
                {
                    gameInstallerPath = API.Instance.ExpandGameVariables(game, gameActions[0]).Path;
                    gameInstallArgs = API.Instance.ExpandGameVariables(game, gameActions[0]).Arguments;
                    installers.Remove(installers.FirstOrDefault(a => a["Path"] == gameInstallerPath));
                }
            }
            else if (!actions && game.Roms != null)
            {
                gameRoms = game.Roms.ToList();
                foreach (GameRom gr in gameRoms)
                {
                    //Take the ROM with the name Install or Installer and use it as the installer
                    if (UpdateSourceOrdering.IsMainName(gr.Name))
                    {
                        if (gameInstallerPath == null) gameInstallerPath = gr.Path;
                        continue;
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
                if (String.IsNullOrEmpty(gameInstallerPath) && gameRoms.Count > 0)
                {
                    //If no Install or Installer ROM is found, use the first ROM in the list
                    gameInstallerPath = gameRoms[0].Path;
                    installers.Remove(installers.FirstOrDefault(a => a["Path"] == gameInstallerPath));
                }
            }
            if (String.IsNullOrEmpty(gameInstallerPath))
            {
                gameInstallerPath = API.Instance.ExpandGameVariables(game, game.InstallDirectory);
            }
            return Tuple.Create(gameInstallerPath, gameInstallArgs, installers);
        }

        public string GetMainInstaller(string dir)
        {
            logger.Info($"GetMainInstaller({dir})");
            string gameInstaller = "";
            List<string> validExt = new List<string> { ".iso", ".rar", ".zip", ".7z", ".exe", ".msi", ".bat", ".ps", ".ps1" };
            List<string> prefExt = new List<string> { ".exe", ".msi", ".bat", ".ps", ".ps1" };
            List<string> dirFiles = Directory.GetFiles(dir).ToList();
            logger.Info($"GetMainInstaller({dir}) - Found {dirFiles.Count} files in directory");
            foreach (string file in dirFiles)
            {
                logger.Info($"GetMainInstaller({dir}) - Checking file: {file}");
                var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                if (prefExt.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    logger.Info($"GetMainInstaller({dir}) - Found preferred extension: {Path.GetExtension(file)} for file: {file}");
                    // Check for common installer names
                    if (fileName == "setup" || fileName == "install")
                    {
                        gameInstaller = file;
                        break;
                    }
                    else if (fileName.Contains("setup") || fileName.Contains("install"))
                    {
                        gameInstaller = file;
                        break;
                    }
                    else
                    {
                        logger.Info($"GetMainInstaller({dir}) - {file} is not a preferred filename.");
                    }
                }
                
                if (validExt.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    logger.Info($"GetMainInstaller({dir}) - Found valid extension: {Path.GetExtension(file)} for file: {file}");
                    gameInstaller = file;
                }
            }
            return gameInstaller;
        }

        public List<Game> AddGame(List<Game> gamesAdded, string dir, LocalLibrarySettings settings, Guid source, string platform, List<ReplaceRule> replacelist)
        {
            bool useActions = settings.UseActions;
            string gamename = Path.GetFileName(dir);
            string gameInstaller = GetMainInstaller(dir);
            if (gameInstaller == "")
            {
                return gamesAdded;
            }

            // Extract metadata from folder name first
            var extractedMetadata = MetadataExtractor.ExtractFromName(gamename, settings.MetadataExtractionRules);
            
            // Use the cleaned name for further processing
            gamename = extractedMetadata.CleanedName;

            if (replacelist != null)
            {
                foreach (ReplaceRule item in replacelist)
                {
                    var type = item.Type;
                    var pattern = item.Pattern;
                    var replacement = item.Replacement ?? string.Empty;

                    // Skip rules where pattern is empty
                    if (string.IsNullOrEmpty(pattern))
                        continue;

                    if (type == "String")
                    {
                        if (gamename.Contains(pattern))
                        {
                            gamename = gamename.Replace(pattern, replacement);
                        }
                    }
                    else if (type == "Regex")
                    {
                        if (Regex.IsMatch(gamename, pattern))
                        {
                            gamename = Regex.Replace(gamename, pattern, replacement);
                        }
                    }
                }

                // Normalize whitespace once at the end
                gamename = Regex.Replace(gamename, @"\s+", " ").Trim();
            }

            IEnumerable<Game> games = API.Instance.Database.Games
                    ?.Where(game => game != null && game.Source != null && game.Source.Id == source)
                    ?? Enumerable.Empty<Game>();
            var matchingGame = games.FirstOrDefault(game => StringExtensions.CleanString(game.Name).ToLowerInvariant() == StringExtensions.CleanString(gamename).ToLowerInvariant());
            
            if (matchingGame != null)
            {
                // Game found - update installer and apply extracted metadata
                logger.Info($"Found existing game '{matchingGame.Name}' for directory '{dir}'");

                // Determine if the game already had linked files (actions or roms)
                bool hadFiles = (matchingGame.GameActions != null && matchingGame.GameActions.Any()) || (matchingGame.Roms != null && matchingGame.Roms.Any());

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

                    if (matchingGame.GameActions == null)
                    {
                        matchingGame.GameActions = new ObservableCollection<GameAction>();
                    }
                    matchingGame.GameActions.AddMissing(action);
                }
                else
                {
                    GameRom installRom = new GameRom
                    {
                        Name = "Install",
                        Path = gameInstaller
                    };

                    if (matchingGame.Roms == null)
                    {
                        matchingGame.Roms = new ObservableCollection<GameRom>();
                    }
                    matchingGame.Roms.AddMissing(installRom);
                }

                // If the game previously had no linked files, optionally discover updates
                try
                {
                    if (!hadFiles && settings.FindUpdates)
                    {
                        var updatesSearchDir = Path.GetDirectoryName(gameInstaller);
                        if (!string.IsNullOrEmpty(updatesSearchDir) && Directory.Exists(Path.Combine(updatesSearchDir, "Updates")))
                        {
                            int updatesAdded = FindGameUpdates(matchingGame, updatesSearchDir, useActions);
                            logger.Info($"Added {updatesAdded} updates for existing game '{matchingGame.Name}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to locate updates for existing game '{matchingGame.Name}': {ex.Message}");
                }

                // Apply extracted metadata to existing game
                if (extractedMetadata.Metadata.Any())
                {
                    logger.Info($"Applying extracted metadata to existing game '{matchingGame.Name}'");
                    MetadataExtractor.ApplyMetadataToGame(matchingGame, extractedMetadata, API.Instance);
                }

                API.Instance.Database.Games.Update(matchingGame);
                return gamesAdded;
            }

            // Game not found - create new game with metadata
            logger.Info($"Creating new game '{gamename}' for directory '{dir}'");

            Game newGame;
            var installDir = Path.GetDirectoryName(gameInstaller);
            var metaDataPath = Path.Combine(installDir, settings.MetadataRelPath, $"{gamename}_metadata.json");
            
            newGame = new Game
            {
                Name = gamename,
                Added = DateTime.Now,
                PluginId = LocalLibrary.PluginId,
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

            // Apply extracted metadata to new game
            if (extractedMetadata.Metadata.Any())
            {
                logger.Info($"Applying extracted metadata to new game '{gamename}'");
                MetadataExtractor.ApplyMetadataToGame(newGame, extractedMetadata, API.Instance);
            }

            gamesAdded.Add(newGame);
            return gamesAdded;
        }

        private sealed class UpdateSourceItem
        {
            public string Name;
            public string Path;
            public GameAction Action;
            public GameRom Rom;
            public bool Main;
            public bool Protected;
        }

        public int FindGameUpdates(Game game, string dir, bool useActions)
        {
            var updatesPath = Path.Combine(dir, "Updates");
            if (!Directory.Exists(updatesPath)) return 0;
            var items = new List<UpdateSourceItem>();
            var playActions = new List<GameAction>();
            if (useActions && game.GameActions != null)
            {
                foreach (var action in game.GameActions)
                {
                    if (action == null) continue;
                    if (action.IsPlayAction) { playActions.Add(action); continue; }
                    bool main = UpdateSourceOrdering.IsMainName(action.Name);
                    items.Add(new UpdateSourceItem { Name = action.Name, Path = action.Path, Action = action,
                        Main = main, Protected = main || action.Type != GameActionType.File });
                }
            }
            else if (!useActions && game.Roms != null)
            {
                foreach (var rom in game.Roms)
                {
                    if (rom == null) continue;
                    bool main = UpdateSourceOrdering.IsMainName(rom.Name);
                    items.Add(new UpdateSourceItem { Name = rom.Name, Path = rom.Path, Rom = rom, Main = main, Protected = main });
                }
            }
            if (!items.Any(x => x.Main))
            {
                // Retain the established fallback primary rather than sorting an update into its place.
                var primary = items.FirstOrDefault(x => x.Action == null || x.Action.Type == GameActionType.File);
                if (primary == null) return 0;
                primary.Main = primary.Protected = true;
            }
            var seen = new HashSet<string>(items.Where(x => !string.IsNullOrEmpty(x.Path)).Select(x => UpdateSourceOrdering.NormalizeSourcePath(x.Path)), StringComparer.OrdinalIgnoreCase);
            string[] entries;
            try { entries = Directory.GetFileSystemEntries(updatesPath); }
            catch (IOException ex) { logger.Warn("Update source is unavailable: " + ex.Message); return 0; }
            catch (UnauthorizedAccessException ex) { logger.Warn("Update source is inaccessible: " + ex.Message); return 0; }
            if (entries.Length > 2048) { logger.Warn("Update folder is too large for automatic discovery; existing links were retained."); return 0; }
            foreach (string entry in entries)
            {
                if (seen.Contains(UpdateSourceOrdering.NormalizeSourcePath(entry))) continue;
                string candidate = null;
                if (File.Exists(entry) && UpdateSourceOrdering.IsInstallerExtension(Path.GetExtension(entry)) &&
                    !Path.GetFileNameWithoutExtension(entry).StartsWith("unins", StringComparison.OrdinalIgnoreCase))
                    candidate = entry;
                else if (Directory.Exists(entry))
                {
                    bool uncertain;
                    candidate = InstallerSourceDiscovery.FindUniqueInstaller(entry, out uncertain);
                    if (uncertain) logger.Warn("Choose the update installer manually for ambiguous or incomplete folder: " + entry);
                }
                if (candidate == null || !seen.Add(UpdateSourceOrdering.NormalizeSourcePath(candidate))) continue;
                items.Add(new UpdateSourceItem { Path = candidate });
            }
            items = UpdateSourceOrdering.Sort(items, x => x.Protected, x => x.Path);
            int updateIndex = 0;
            foreach (var item in items)
            {
                if (!item.Protected) updateIndex++;
                if (item.Action != null || item.Rom != null) continue;
                string version = UpdateSourceOrdering.CreateKey(item.Path).TargetVersion;
                item.Name = string.IsNullOrEmpty(version) ? "Update " + updateIndex : "Update " + version;
                if (useActions)
                    item.Action = new GameAction { Name = item.Name, Path = item.Path, Type = GameActionType.File,
                        TrackingMode = TrackingMode.Default, IsPlayAction = false };
                else item.Rom = new GameRom { Name = item.Name, Path = item.Path };
            }
            if (useActions)
                game.GameActions = new ObservableCollection<GameAction>(items.Select(x => x.Action).Concat(playActions));
            else game.Roms = new ObservableCollection<GameRom>(items.Select(x => x.Rom));
            return updateIndex;
        }

        public List<Game> FindInstallers(List<string> installPaths, LocalLibrarySettings settings, List<ReplaceRule> replacelist)
        {
            bool useActions = settings.UseActions;
            int lpercent = settings.Levenshtein;
            ObservableCollection<GameSourceOption> sources = settings.SelectedSources;
            string platform = settings.SelectedPlatform;
            bool findupdates = settings.FindUpdates;

            List<ReportItem> reportItems = new List<ReportItem>();
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

                string gameImagePath = GetActionsRoms(game, settings).Item1;
                if (String.IsNullOrEmpty(gameImagePath))
                {
                    continue;
                }
                List<string> exts = new List<string> { ".exe", ".iso", ".rar", ".zip", ".7z", ".bat", ".ps", ".ps1" };
                if (exts.Contains(Path.GetExtension(gameImagePath), StringComparer.OrdinalIgnoreCase) || File.Exists(gameImagePath))
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
            )
            {
                IsIndeterminate = false
            };
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

                        if (gameInstallDirs.Contains(dir, StringComparer.OrdinalIgnoreCase))
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
                            percent *= 100;
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
                                        gamesAdded = AddGame(gamesAdded, dir, settings, primarysourceid, platform, replacelist);
                                    }
                                });
                            }
                        }
                        else
                        {
                            gamesAdded = AddGame(gamesAdded, dir, settings, primarysourceid, platform, replacelist);
                        }
                    }

                    int totalNewUpdates = 0;
                    foreach (Game game in gamesAdded)
                    {
                        if (findupdates)
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
