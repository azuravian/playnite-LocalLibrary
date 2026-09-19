using LocalLibrary;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Newtonsoft.Json;

namespace LocalLibrary.Helpers
{
    public class ExportData
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static string ExportToJson(Dictionary<string, object> data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                logger.Info($"{data["Name"]}: Successfully serialized {data.Count} metadata items to JSON");
                return json;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{data["Name"]}: Failed to serialize data to JSON");
                return "{}"; // Return empty JSON object on failure
            }
        }

        public static Dictionary<string, object> GetExportData(LocalLibrarySettings settings, Game game)
        {
            var data = new Dictionary<string, object>();
            
            try
            {
                // Process all element types
                ProcessTextElements(data, game, settings.TextElements);
                ProcessStatusElements(data, game, settings.StatusElements);
                ProcessNumericElements(data, game, settings.NumericElements);
                ProcessDateElements(data, game, settings.DateElements);
                ProcessScriptElements(data, game, settings.ScriptElements);

                logger.Info($"Successfully processed metadata for game '{game?.Name}' - {data.Count} items exported");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to get export data for game '{game?.Name}'");
            }

            return data;
        }

        public static void ExportGameData(LocalLibrarySettings settings, Game game, string exportDir)
        {
            if (game == null)
            {
                logger.Error("Cannot export game data - game is null");
                return;
            }

            if (string.IsNullOrWhiteSpace(exportDir))
            {
                logger.Error($"Cannot export game data for '{game.Name}' - export directory is null or empty");
                return;
            }

            try
            {
                var gameData = GetExportData(settings, game);
                
                if (!gameData.Any())
                {
                    logger.Info($"No metadata selected for export for game '{game.Name}'");
                    return;
                }

                string output = ExportToJson(gameData);
                
                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                    logger.Info($"Created export directory: {exportDir}");
                }

                var exportFile = Path.Combine(exportDir, $"{StringExtensions.CleanString(game.Name)}_metadata.json");
                File.WriteAllText(exportFile, output);
                logger.Info($"Successfully exported metadata for '{game.Name}' to: {exportFile}");

                ExportMediaElements(game, exportDir, settings.MediaElements);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to export game data for '{game.Name}' to directory '{exportDir}'");
            }
        }

        public static void ExportAllGamesData(LocalLibrarySettings settings, IPlayniteAPI api, string exportPath)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                var errorMsg = "Export path is not set. Please set a valid export path in the Local Library settings.";
                logger.Error(errorMsg);
                api.Dialogs.ShowMessage(errorMsg, "Export Error");
                return;
            }

            if (settings == null)
            {
                logger.Error("Cannot export games data - settings is null");
                return;
            }

            if (api?.Database?.Games == null)
            {
                logger.Error("Cannot export games data - API database is not available");
                return;
            }

            int successCount = 0;
            int failureCount = 0;
            int totalGames = 0;

            try
            {
                var localLibraryGames = api.Database.Games.Where(g => g.PluginId == LocalLibrary.PluginId).ToList();
                totalGames = localLibraryGames.Count;

                logger.Info($"Starting export for {totalGames} Local Library games to path: {exportPath}");

                foreach (var game in localLibraryGames)
                {
                    try
                    {
                        if (game == null)
                        {
                            logger.Error("Encountered null game in collection, skipping");
                            failureCount++;
                            continue;
                        }

                        Finder iFinder = new Finder();
                        var gameInstaller = iFinder.GetActionsRoms(game, settings).Item1;
                        
                        if (!File.Exists(gameInstaller))
                        {
                            logger.Info($"Game installer not found for '{game.Name}': {gameInstaller}. Skipping export.");
                            failureCount++;
                            continue;
                        }

                        var gameDirPath = Path.GetDirectoryName(gameInstaller);
                        if (string.IsNullOrEmpty(gameDirPath))
                        {
                            logger.Error($"Could not determine game directory for '{game.Name}' from installer: {gameInstaller}");
                            failureCount++;
                            continue;
                        }

                        var gameDirName = new DirectoryInfo(gameDirPath).Name;
                        string exportDir;

                        // Check if exportPath is absolute or relative
                        if (Path.IsPathRooted(exportPath))
                        {
                            exportDir = Path.Combine(exportPath, gameDirName);
                        }
                        else
                        {
                            var baseDir = Path.GetDirectoryName(gameInstaller);
                            exportDir = Path.Combine(baseDir, exportPath);
                        }

                        ExportGameData(settings, game, exportDir);
                        successCount++;
                        logger.Info($"Successfully exported metadata for game '{game.Name}' ({successCount}/{totalGames})");
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        logger.Error(ex, $"Failed to export metadata for game '{game?.Name}' ({failureCount} failures so far)");
                    }
                }

                logger.Info($"Export completed. Success: {successCount}, Failed: {failureCount}, Total: {totalGames}");
                
                if (successCount > 0 || failureCount > 0)
                {
                    api.Dialogs.ShowMessage($"Export completed!\n\nSuccessfully exported: {successCount} games\nFailed: {failureCount} games\nTotal processed: {totalGames} games", "Export Complete");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Critical error during export all games operation. Success: {successCount}, Failed: {failureCount}");
                api.Dialogs.ShowMessage($"Export failed with error: {ex.Message}\n\nPartial results: {successCount} successful, {failureCount} failed", "Export Error");
            }
        }

        private static void ProcessTextElements(Dictionary<string, object> data, Game game, ObservableCollection<MetadataItem> elements)
        {
            if (elements == null || game == null) return;

            try
            {
                foreach (var element in elements.Where(e => e.IsSelected))
                {
                    try
                    {
                        switch (element.Name)
                        {
                            case "Name":
                                data[element.Name] = game.Name ?? "";
                                break;
                            case "SortingName":
                                data[element.Name] = game.SortingName ?? "";
                                break;
                            case "Series":
                                data[element.Name] = GetCollectionNames(game.Series);
                                break;
                            case "Description":
                                data[element.Name] = game.Description ?? "";
                                break;
                            case "Region":
                                data[element.Name] = GetCollectionNames(game.Regions);
                                break;
                            case "Platforms":
                                data[element.Name] = GetCollectionNames(game.Platforms);
                                break;
                            case "Categories":
                                data[element.Name] = GetCollectionNames(game.Categories);
                                break;
                            case "Features":
                                data[element.Name] = GetCollectionNames(game.Features);
                                break;
                            case "Genres":
                                data[element.Name] = GetCollectionNames(game.Genres);
                                break;
                            case "Links":
                                data[element.Name] = GetLinksData(game.Links);
                                break;
                            case "Tags":
                                data[element.Name] = GetCollectionNames(game.Tags);
                                break;
                            case "Version":
                                data[element.Name] = game.Version ?? "";
                                break;
                            case "Developers":
                                data[element.Name] = GetCollectionNames(game.Developers);
                                break;
                            case "Publishers":
                                data[element.Name] = GetCollectionNames(game.Publishers);
                                break;
                            case "Source":
                                data[element.Name] = game.Source?.Name ?? "";
                                break;
                            case "AgeRatings":
                                data[element.Name] = GetCollectionNames(game.AgeRatings);
                                break;
                            case "Notes":
                                data[element.Name] = game.Notes ?? "";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Failed to process text element '{element.Name}' for game '{game.Name}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to process text elements for game '{game?.Name}'");
            }
        }

        private static void ProcessNumericElements(Dictionary<string, object> data, Game game, ObservableCollection<MetadataItem> elements)
        {
            if (elements == null || game == null) return;

            try
            {
                foreach (var element in elements.Where(e => e.IsSelected))
                {
                    try
                    {
                        switch (element.Name)
                        {
                            case "Playtime":
                                data[element.Name] = game.Playtime.ToString() ?? "";
                                break;
                            case "PlayCount":
                                data[element.Name] = game.PlayCount.ToString() ?? "";
                                break;
                            case "UserScore":
                                data[element.Name] = game.UserScore?.ToString() ?? "";
                                break;
                            case "CriticScore":
                                data[element.Name] = game.CriticScore?.ToString() ?? "";
                                break;
                            case "CommunityScore":
                                data[element.Name] = game.CommunityScore?.ToString() ?? "";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Failed to process numeric element '{element.Name}' for game '{game.Name}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to process numeric elements for game '{game?.Name}'");
            }
        }

        private static void ProcessStatusElements(Dictionary<string, object> data, Game game, ObservableCollection<MetadataItem> elements)
        {
            if (elements == null || game == null) return;

            try
            {
                foreach (var element in elements.Where(e => e.IsSelected))
                {
                    try
                    {
                        switch (element.Name)
                        {
                            case "CompletionStatus":
                                data[element.Name] = game.CompletionStatus?.Name ?? "";
                                break;
                            case "Hidden":
                                data[element.Name] = game.Hidden;
                                break;
                            case "Favorite":
                                data[element.Name] = game.Favorite;
                                break;
                            case "EnableHDRSupport":
                                data[element.Name] = game.EnableSystemHdr;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Failed to process status element '{element.Name}' for game '{game.Name}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to process status elements for game '{game?.Name}'");
            }
        }

        private static void ProcessDateElements(Dictionary<string, object> data, Game game, ObservableCollection<MetadataItem> elements)
        {
            if (elements == null || game == null) return;

            try
            {
                foreach (var element in elements.Where(e => e.IsSelected))
                {
                    try
                    {
                        switch (element.Name)
                        {
                            case "ReleaseDate":
                                data[element.Name] = game.ReleaseDate?.ToString() ?? "";
                                break;
                            case "Added":
                                data[element.Name] = game.Added?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                                break;
                            case "Modified":
                                data[element.Name] = game.Modified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                                break;
                            case "LastActivity":
                                data[element.Name] = game.LastActivity?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Failed to process date element '{element.Name}' for game '{game.Name}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to process date elements for game '{game?.Name}'");
            }
        }

        private static void ProcessScriptElements(Dictionary<string, object> data, Game game, ObservableCollection<MetadataItem> elements)
        {
            if (elements == null || game == null) return;

            try
            {
                foreach (var element in elements.Where(e => e.IsSelected))
                {
                    try
                    {
                        switch (element.Name)
                        {
                            case "GameStartedScript":
                                data[element.Name] = game.GameStartedScript ?? "";
                                break;
                            case "PostScript":
                                data[element.Name] = game.PostScript ?? "";
                                break;
                            case "PreScript":
                                data[element.Name] = game.PreScript ?? "";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Failed to process script element '{element.Name}' for game '{game.Name}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to process script elements for game '{game?.Name}'");
            }
        }

        private static void ExportMediaElements(Game game, string exportDir, ObservableCollection<MetadataItem> elements)
        {
            if (elements == null || game == null || string.IsNullOrWhiteSpace(exportDir)) return;

            try
            {
                foreach (var element in elements.Where(e => e.IsSelected))
                {
                    try
                    {
                        switch (element.Name)
                        {
                            case "Icon":
                                if (!string.IsNullOrEmpty(game.Icon))
                                {
                                    var iconSrc = Path.Combine(API.Instance.Paths.ApplicationPath, "library", "files", game.Icon);
                                    var iconDest = Path.Combine(exportDir, $"Icon{Path.GetExtension(iconSrc)}");
                                    if (File.Exists(iconSrc))
                                    {
                                        File.Copy(iconSrc, iconDest, true);
                                        logger.Info($"Exported icon for game '{game.Name}' to: {iconDest}");
                                    }
                                    else
                                    {
                                        logger.Info($"Icon file not found for game '{game.Name}': {iconSrc}");
                                    }
                                }
                                break;
                            case "CoverImage":
                                if (!string.IsNullOrEmpty(game.CoverImage))
                                {
                                    var coverSrc = Path.Combine(API.Instance.Paths.ApplicationPath, "library", "files", game.CoverImage);
                                    var coverDest = Path.Combine(exportDir, $"Cover{Path.GetExtension(coverSrc)}");
                                    if (File.Exists(coverSrc))
                                    {
                                        File.Copy(coverSrc, coverDest, true);
                                        logger.Info($"Exported cover image for game '{game.Name}' to: {coverDest}");
                                    }
                                    else
                                    {
                                        logger.Info($"Cover image file not found for game '{game.Name}': {coverSrc}");
                                    }
                                }
                                break;
                            case "BackgroundImage":
                                if (!string.IsNullOrEmpty(game.BackgroundImage))
                                {
                                    var backgroundSrc = Path.Combine(API.Instance.Paths.ApplicationPath, "library", "files", game.BackgroundImage);
                                    var backgroundDest = Path.Combine(exportDir, $"Background{Path.GetExtension(backgroundSrc)}");
                                    if (File.Exists(backgroundSrc))
                                    {
                                        File.Copy(backgroundSrc, backgroundDest, true);
                                        logger.Info($"Exported background image for game '{game.Name}' to: {backgroundDest}");
                                    }
                                    else
                                    {
                                        logger.Info($"Background image file not found for game '{game.Name}': {backgroundSrc}");
                                    }
                                }
                                break;
                            case "Logo":
                                var logoSrc = Path.Combine(API.Instance.Paths.ConfigurationPath, "ExtraMetadata", "games", game.Id.ToString(), "Logo.png");
                                var logoDest = Path.Combine(exportDir, "Logo.png");
                                if (File.Exists(logoSrc))
                                {
                                    File.Copy(logoSrc, logoDest, true);
                                    logger.Info($"Exported logo for game '{game.Name}' to: {logoDest}");
                                }
                                else
                                {
                                    logger.Info($"Logo file not found for game '{game.Name}': {logoSrc}");
                                }
                                break;
                            case "Manual":
                                if (!string.IsNullOrEmpty(game.Manual))
                                {
                                    var manualSrc = game.Manual;
                                    var manualDest = Path.Combine(exportDir, $"Manual{Path.GetExtension(manualSrc)}");
                                    if (File.Exists(manualSrc))
                                    {
                                        File.Copy(manualSrc, manualDest, true);
                                        logger.Info($"Exported manual for game '{game.Name}' to: {manualDest}");
                                    }
                                    else
                                    {
                                        logger.Info($"Manual file not found for game '{game.Name}': {manualSrc}");
                                    }
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Failed to export media element '{element.Name}' for game '{game.Name}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to export media elements for game '{game?.Name}'");
            }
        }

        /// <summary>
        /// Extract names from database object collections, ignoring GUIDs
        /// </summary>
        private static List<string> GetCollectionNames<T>(IEnumerable<T> collection) where T : DatabaseObject
        {
            try
            {
                return collection?
                    .Select(item => item.Name ?? "")
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList() 
                    ?? new List<string>();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to get collection names");
                return new List<string>();
            }
        }

        /// <summary>
        /// Extract link data as name/url pairs
        /// </summary>
        private static List<Dictionary<string, string>> GetLinksData(ObservableCollection<Link> links)
        {
            try
            {
                return links?.Select(link => new Dictionary<string, string>
                {
                    ["name"] = link.Name ?? "",
                    ["url"] = link.Url ?? ""
                }).ToList() ?? new List<Dictionary<string, string>>();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to get links data");
                return new List<Dictionary<string, string>>();
            }
        }

        // TODO: Add import methods
        // public static void ImportFromJson(string jsonData, Game game, IPlayniteAPI api) { ... }
        // private static void SetCollectionByNames<T>(Game game, string propertyName, string[] names, IPlayniteAPI api) where T : DatabaseObject { ... }
    }
}
