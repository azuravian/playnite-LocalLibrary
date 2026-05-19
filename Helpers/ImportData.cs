using LocalLibrary.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace LocalLibrary.Helpers
{
    internal class ImportData
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static Dictionary<string, object> ImportFromJson(string gamename, string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                logger.Info($"{gamename}: Successfully deserialized JSON to metadata items");
                return data;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{gamename}: Failed to deserialize JSON to metadata items");
                return new Dictionary<string, object>(); // Return empty dictionary on failure
            }
        }

        public static void ImportExtra(Game game, string metadataPath)
        {
            if (game == null || metadataPath == null)
                return;

            try
            {
                // Read and deserialize disk metadata
                var json = File.ReadAllText(metadataPath);
                var metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (metadata.TryGetValue("Notes", out var notesValue))
                {
                    logger.Info($"Notes type: {notesValue?.GetType().FullName}");

                    game.Notes = notesValue?.ToString();
                }

                if (metadata.TryGetValue("EnableSystemHdr", out var HdrValue))
                {
                    logger.Info($"EnableSystemHdr type: {HdrValue?.GetType().FullName}");

                    game.EnableSystemHdr = (bool)HdrValue;
                }

                if (metadata.TryGetValue("Manual", out var manualValue))
                {
                    logger.Info($"Manual type: {manualValue?.GetType().FullName}");

                    game.Manual = manualValue?.ToString();
                }

                if (metadata.TryGetValue("Added", out var addedValue) &&
                    DateTime.TryParse(Convert.ToString(addedValue), out var addedDate))
                {
                    game.Added = addedDate;
                }

                if (metadata.TryGetValue("Modified", out var modifiedValue) &&
                    DateTime.TryParse(Convert.ToString(modifiedValue), out var modifiedDate))
                {
                    game.Modified = modifiedDate;
                }

                // Import media files if they exist
                ImportMediaFiles(game, metadataPath);

                API.Instance.Database.Games.Update(game);
                logger.Info($"Successfully imported metadata for game '{game.Name}'");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to import extra metadata for game '{game.Name}': {ex.Message}");
            }
        }

        private static void ImportMediaFiles(Game game, string metadataPath)
        {
            if (game == null || string.IsNullOrEmpty(metadataPath))
                return;

            var metadataDir = Path.GetDirectoryName(metadataPath);
            if (string.IsNullOrEmpty(metadataDir) || !Directory.Exists(metadataDir))
            {
                logger.Debug($"Metadata directory not found for import: {metadataDir}");
                return;
            }

            try
            {
                // Import Icon
                var iconFile = Directory.EnumerateFiles(metadataDir, "Icon*").FirstOrDefault();
                if (!string.IsNullOrEmpty(iconFile) && File.Exists(iconFile))
                {
                    game.Icon = ImportMediaFile(iconFile, game.Id);
                    logger.Info($"Imported icon for game '{game.Name}'");
                }

                // Import CoverImage
                var coverFile = Directory.EnumerateFiles(metadataDir, "Cover*").FirstOrDefault();
                if (!string.IsNullOrEmpty(coverFile) && File.Exists(coverFile))
                {
                    game.CoverImage = ImportMediaFile(coverFile, game.Id);
                    logger.Info($"Imported cover image for game '{game.Name}'");
                }

                // Import BackgroundImage
                var backgroundFile = Directory.EnumerateFiles(metadataDir, "Background*").FirstOrDefault();
                if (!string.IsNullOrEmpty(backgroundFile) && File.Exists(backgroundFile))
                {
                    game.BackgroundImage = ImportMediaFile(backgroundFile, game.Id);
                    logger.Info($"Imported background image for game '{game.Name}'");
                }

                // Import Logo (stored in ExtraMetadata)
                var logoFile = Path.Combine(metadataDir, "Logo.png");
                if (File.Exists(logoFile))
                {
                    try
                    {
                        var extraMetadataDir = Path.Combine(API.Instance.Paths.ConfigurationPath, "ExtraMetadata", "games", game.Id.ToString());
                        Directory.CreateDirectory(extraMetadataDir);
                        var logoDestPath = Path.Combine(extraMetadataDir, "Logo.png");
                        File.Copy(logoFile, logoDestPath, true);
                        logger.Info($"Imported logo for game '{game.Name}'");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Failed to import logo for game '{game.Name}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to import media files for game '{game.Name}': {ex.Message}");
            }
        }

        private static string ImportMediaFile(string sourceFile, Guid gameId)
        {
            try
            {
                var libraryFilesPath = Path.Combine(API.Instance.Paths.ApplicationPath, "library", "files");
                Directory.CreateDirectory(libraryFilesPath);

                // Create a unique filename using game ID and extension
                var ext = Path.GetExtension(sourceFile);
                var newFileName = $"{gameId}{ext}";
                var destPath = Path.Combine(libraryFilesPath, newFileName);

                // Copy the file
                File.Copy(sourceFile, destPath, true);
                logger.Debug($"Imported media file from '{sourceFile}' to '{destPath}'");

                // Return the relative path format that Playnite expects
                return newFileName;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to import media file '{sourceFile}' for game ID '{gameId}'");
                return null;
            }
        }

        private static Dictionary<string, object> NormalizeDataKeys(Dictionary<string, object> data)
        {
            // Create a mapping of known display names (with spaces) to canonical property names (without spaces)
            var keyMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Sorting Name", "SortingName" },
                { "Age Ratings", "AgeRatings" },
                { "Completion Status", "CompletionStatus" },
                { "Enable HDR Support", "EnableHDRSupport" }
            };

            var normalizedData = new Dictionary<string, object>(data.Count);
            foreach (var kvp in data)
            {
                var normalizedKey = kvp.Key;

                // Check if this key needs to be mapped to a canonical name
                if (keyMapping.TryGetValue(kvp.Key, out var mappedKey))
                {
                    normalizedKey = mappedKey;
                    logger.Info($"Normalized metadata key from '{kvp.Key}' to '{mappedKey}'");
                }

                normalizedData[normalizedKey] = kvp.Value;
            }

            return normalizedData;
        }

        public static void ImportMetadataToGame(LocalLibrarySettings settings, Game game, Dictionary<string, object> data, String importDir)
        {
            if (game == null || data == null)
                return;

            // Normalize keys in the data dictionary to handle legacy exports with spaces
            data = NormalizeDataKeys(data);

            var gameType = typeof(Game);
            var dbcoll = PlayniteHelpers.GetDatabaseCollections(API.Instance);
            var gamecoll = PlayniteHelpers.GetGameCollections(game);

            foreach (var kvp in data)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                var prop = gameType.GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null)
                    continue;
                else if (!prop.CanWrite && typeof(IList).IsAssignableFrom(prop.PropertyType))
                {
                    try
                    {
                        // Handle read-only collection
                        var list = prop.GetValue(game) as IList;
                        if (list == null)
                        {
                            var dblist = dbcoll.ContainsKey(key) ? dbcoll[key] as IList : null;
                            var gamelist = gamecoll.ContainsKey(key) ? gamecoll[key] as IList : null;
                        }
                        else
                        {
                            foreach (var item in (IEnumerable)value)
                            {
                                if (!list.Contains(item))
                                    list.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Unable to import {prop.Name} into {game.Name}");
                    }
                }
                else
                {
                    try
                    {
                        object convertedValue = null;

                        // Handle string
                        if (prop.PropertyType == typeof(string))
                        {
                            convertedValue = value?.ToString();
                        }

                        // Handle lists of database object names
                        else if (typeof(IEnumerable<string>).IsAssignableFrom(prop.PropertyType))
                        {
                            if (value is IEnumerable<object> objList)
                            {
                                convertedValue = objList.Select(v => v?.ToString() ?? "").ToList();
                            }
                        }

                        // Handle Link collections
                        else if (typeof(IEnumerable<Link>).IsAssignableFrom(prop.PropertyType))
                        {
                            var list = new ObservableCollection<Link>();
                            if (value is IEnumerable<object> dicts)
                            {
                                foreach (var item in dicts)
                                {
                                    if (item is JObject jObj)
                                    {
                                        var name = jObj["name"]?.ToString();
                                        var url = jObj["url"]?.ToString();

                                        if (!string.IsNullOrEmpty(name)
                                        {
                                            list.Add(new Link(name, url));
                                        }
                                    }
                                }
                            }
                            convertedValue = list;
                        }

                        // Handle enums (e.g., CompletionStatus, Source, etc.)
                        else if (prop.PropertyType.IsEnum)
                        {
                            try
                            {
                                if (value != null)
                                {
                                    convertedValue = Enum.Parse(prop.PropertyType, value.ToString(), ignoreCase: true);
                                }
                            }
                            catch
                            {
                                // Ignore invalid enum values (keep null)
                            }
                        }

                        // Handle DateTime and nullable DateTime
                        else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                        {
                            if (DateTime.TryParse(value?.ToString(), null, DateTimeStyles.RoundtripKind, out var parsedDate))
                                convertedValue = parsedDate;
                        }

                        // Handle numeric and primitive types
                        else if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(decimal))
                        {
                            convertedValue = Convert.ChangeType(value, prop.PropertyType);
                        }
                        else if (Nullable.GetUnderlyingType(prop.PropertyType)?.IsPrimitive == true)
                        {
                            var underlying = Nullable.GetUnderlyingType(prop.PropertyType);
                            convertedValue = value == null ? null : Convert.ChangeType(value, underlying);
                        }

                        // Fallback for anything else (e.g., object)
                        else
                        {
                            convertedValue = value;
                        }

                        // Apply the property if we got something
                        if (convertedValue != null)
                        {
                            prop.SetValue(game, convertedValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, $"{game.Name}: Failed to set property '{key}' from import data");
                    }
                }
            }

            var metaDataPath = Directory.GetFiles(importDir, "*metadata.json").FirstOrDefault();
            ImportExtra(game, metaDataPath);
            LoadTextElements(data, game, settings.TextElements);

            API.Instance.Database.Games.Update(game);
        }

        private static void LoadTextElements(Dictionary<string, object> data, Game game, ObservableCollection<MetadataItem> elements)
        {
            if (game == null || data == null)
                return;

            var gameType = typeof(Game);

            foreach (var element in elements)
            {
                var prop = gameType.GetProperty(element.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null || !prop.CanWrite)
                {
                    continue; 
                }

                try
                {
                    object value = data[element.Name];
                    if (value != null && !prop.PropertyType.IsAssignableFrom(value.GetType()))
                    {
                        value = Convert.ChangeType(value, prop.PropertyType);
                    }

                    prop.SetValue(game, value);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"{game.Name}: Failed to set property '{element.Name}' with value '{data[element.Name]}'");
                }
            }
        }
    }
}
