using Playnite.SDK;

using Playnite.SDK.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
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

        public Tuple<string, string, List<GameAction>> GetActions(Game game) 
        {
            string gameImagePath = null;
            string gameInstallArgs = null;
            List<GameAction> gameActions = new List<GameAction>();
            if (game.GameActions != null)
            { 
                gameActions = game.GameActions.ToList();
                foreach (GameAction g in gameActions)
                {
                    if (g.Name == "Install" || g.Name == "Installer")
                    {
                        gameImagePath = API.Instance.ExpandGameVariables(game, g).Path;
                        gameInstallArgs = API.Instance.ExpandGameVariables(game, g).Arguments;
                        break;
                    }
                }
                if (String.IsNullOrEmpty(gameImagePath) && gameActions.Count > 0)
                {
                    gameImagePath = API.Instance.ExpandGameVariables(game, gameActions[0]).Path;
                }
            }
            else
            {
                gameImagePath = API.Instance.ExpandGameVariables(game, game.InstallDirectory);
            }
            return Tuple.Create(gameImagePath, gameInstallArgs, gameActions);
        }

        public Tuple<string, string, List<GameRom>> GetRoms(Game game)
        {
            string gameImagePath = null;
            string gameInstallArgs = null;
            List<GameRom> gameRoms = new List<GameRom>();
            if (game.Roms != null)
            {
                gameRoms = game.Roms.ToList();
                foreach (GameRom gr in gameRoms)
                {
                    //Take the ROM with the name Install or Installer and use it as the installer
                    if (gr.Name == "Install" || gr.Name == "Installer")
                    {
                        gameImagePath = gr.Path;
                        break;
                    }
                }
                if (String.IsNullOrEmpty(gameImagePath) && gameRoms.Count > 0)
                {
                    //If no Install or Installer ROM is found, use the first ROM in the list
                    gameImagePath = gameRoms[0].Path;
                }
            }
            if (String.IsNullOrEmpty(gameImagePath))
            {
                gameImagePath = API.Instance.ExpandGameVariables(game, game.InstallDirectory);
            }
            return Tuple.Create(gameImagePath, gameInstallArgs, gameRoms);
        }

        public List<Game> AddGame(List<Game> gamesAdded, string dir, bool useActions, string source, string platform)
        {
            Game newGame = new Game
            {
                Name = Path.GetFileName(dir),
                Added = DateTime.Now,
                PluginId = Guid.Parse("2d01017d-024e-444d-80d3-f62f5be3fca5"),
                SourceId = API.Instance.Database.Sources.FirstOrDefault(a => a.Name == source)?.Id ?? Guid.Empty,
                PlatformIds = new List<Guid> { API.Instance.Database.Platforms.FirstOrDefault(a => a.Name == platform)?.Id ?? Guid.Empty }
            };

            string gameInstaller = "";
            List<string> validExt = new List<string> { ".iso", ".rar", ".zip", ".7z" };
            List<string> dirFiles = Directory.GetFiles(dir).ToList();
            foreach (string file in dirFiles)
            {
                if (Path.GetExtension(file) == ".exe")
                {
                    if (Path.GetFileNameWithoutExtension(file).ToLower() == "setup" || Path.GetFileNameWithoutExtension(file).ToLower() == "install")
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
            if ( gameInstaller == "" )
            {
                return gamesAdded;
            }
            else if (useActions)
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
                GameRom installRom = new GameRom();
                installRom.Name = "Install";
                installRom.Path = gameInstaller;
                newGame.Roms = new ObservableCollection<GameRom>();
                newGame.Roms.AddMissing(installRom);
            }
            gamesAdded.Add(newGame);
            return gamesAdded;
        }

        public void FindInstallers(List<string> installPaths, bool useActions, int lpercent, string source, string platform)
        {
            IEnumerable<Game> games = API.Instance.Database.Games;
            List<string> gameInstallDirs = new List<string>();

            foreach (Game game in games)
            {
                if ((game.Source?.ToString() ?? "") != source)
                {
                    continue;
                }

                string gameImagePath = useActions ? GetActions(game).Item1 : GetRoms(game).Item1;
                List<string> exts = new List<string> { ".exe", ".iso", ".rar", ".zip", ".7z" };
                if (exts.Contains(Path.GetExtension(gameImagePath)) || File.Exists(gameImagePath))
                {
                    gameImagePath = Path.GetDirectoryName(gameImagePath);
                }
                gameInstallDirs.Add(gameImagePath);
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"Local Library - Finding Installers...",
                true
                        );
            globalProgressOptions.IsIndeterminate = false;
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
                    List<Game> gamesAdded = new List<Game>();

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
                                if (gameInstallDir == null)
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
                                    gamesAdded = AddGame(gamesAdded, dir, useActions, source, platform);
                                }
                            });
                        }
                            }
                        else
                        {
                            gamesAdded = AddGame(gamesAdded, dir, useActions, source, platform);
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
        }
    }
}
