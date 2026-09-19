using LocalLibrary.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace LocalLibrary.Helpers
{
    public class MetadataExtractor
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public class MetadataCollection
        {
            public string Value { get; set; }
            public bool Append { get; set; } = false;
            public MetadataCollection()
            {
                Value = "";
            }
        }

        public class ExtractedMetadata
        {
            public string CleanedName { get; set; }
            public Dictionary<string, List<MetadataCollection>> Metadata { get; set; }

            public ExtractedMetadata()
            {
                Metadata = new Dictionary<string, List<MetadataCollection>>();
            }
        }

        /// <summary>
        /// Extract metadata from folder name based on extraction rules
        /// </summary>
        public static ExtractedMetadata ExtractFromName(string folderName, ObservableCollection<MetadataExtractionRule> rules)
        {
            var result = new ExtractedMetadata
            {
                CleanedName = folderName
            };

            if (rules == null || !rules.Any() || string.IsNullOrWhiteSpace(folderName))
            {
                return result;
            }

            string workingName = folderName;

            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.OpenDelimiter) || 
                    string.IsNullOrWhiteSpace(rule.CloseDelimiter) || 
                    string.IsNullOrWhiteSpace(rule.MetadataType))
                {
                    continue;
                }

                try
                {
                    // Escape regex special characters in delimiters
                    string escapedOpen = Regex.Escape(rule.OpenDelimiter);
                    string escapedClose = Regex.Escape(rule.CloseDelimiter);

                    // Pattern to match content between delimiters
                    string pattern = $@"{escapedOpen}([^{escapedClose}]+?){escapedClose}";
                    var matches = Regex.Matches(workingName, pattern);

                    if (matches.Count > 0)
                    {
                        if (!result.Metadata.ContainsKey(rule.MetadataType))
                        {
                            result.Metadata[rule.MetadataType] = new List<MetadataCollection>();
                        }

                        foreach (Match match in matches)
                        {
                            if (match.Success && match.Groups.Count > 1)
                            {
                                string value = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    MetadataCollection collection = new MetadataCollection();
                                    if (rule.Append && rule.MetadataType == "Notes")
                                    {
                                        collection.Append = true;
                                    }
                                    if (rule.Pattern != null)
                                    {
                                        // Apply additional pattern filtering if specified
                                        value = rule.Pattern.Replace("*", value);
                                    }
                                    collection.Value = value;
                                    result.Metadata[rule.MetadataType].Add(collection);
                                    logger.Info($"Extracted {rule.MetadataType}: '{value}' from '{folderName}'");
                                }

                                // Remove the matched text (including delimiters) from the working name
                                workingName = workingName.Replace(match.Value, " ");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to extract metadata using rule: {rule.OpenDelimiter}...{rule.CloseDelimiter} for type {rule.MetadataType}");
                }
            }

            // Clean up multiple spaces and trim
            result.CleanedName = Regex.Replace(workingName, @"\s+", " ").Trim();
            logger.Info($"Cleaned name: '{folderName}' -> '{result.CleanedName}'");

            return result;
        }

        /// <summary>
        /// Apply extracted metadata to a game object
        /// </summary>
        public static void ApplyMetadataToGame(Game game, ExtractedMetadata extractedMetadata, IPlayniteAPI api)
        {
            if (game == null || extractedMetadata == null || !extractedMetadata.Metadata.Any())
            {
                return;
            }

            foreach (var metadataEntry in extractedMetadata.Metadata)
            {
                string metadataType = metadataEntry.Key;
                List<MetadataCollection> values = metadataEntry.Value;

                if (!values.Any())
                    continue;

                try
                {
                    switch (metadataType)
                    {
                        case "Version":
                            // Version is a simple string
                            game.Version = string.Join(", ", values.Select(v => v.Value));
                            logger.Info($"Set Version to '{game.Version}' for game '{game.Name}'");
                            break;
                            
                        case "Genre":
                            ApplyDatabaseObjectCollection(game, values, api.Database.Genres, 
                                (g, ids) => g.GenreIds = ids, "Genre");
                            break;

                        case "Platform":
                            ApplyDatabaseObjectCollection(game, values, api.Database.Platforms, 
                                (g, ids) => g.PlatformIds = ids, "Platform");
                            break;

                        case "Tag":
                            ApplyDatabaseObjectCollection(game, values, api.Database.Tags, 
                                (g, ids) => g.TagIds = ids, "Tag");
                            break;

                        case "Feature":
                            ApplyDatabaseObjectCollection(game, values, api.Database.Features, 
                                (g, ids) => g.FeatureIds = ids, "Feature");
                            break;

                        case "Category":
                            ApplyDatabaseObjectCollection(game, values, api.Database.Categories, 
                                (g, ids) => g.CategoryIds = ids, "Category");
                            break;

                        case "AgeRating":
                            ApplyDatabaseObjectCollection(game, values, api.Database.AgeRatings, 
                                (g, ids) => g.AgeRatingIds = ids, "AgeRating");
                            break;

                        case "UserScore":
                            if (values.Count == 1 && int.TryParse(values[0].Value, out int score))
                            {
                                game.UserScore = score;
                                logger.Info($"Set UserScore to '{game.UserScore}' for game '{game.Name}'");
                            }
                            else
                            {
                                logger.Warn($"Invalid UserScore value(s): '{string.Join(", ", values)}' for game '{game.Name}'");
                            }
                            break;

                        case "Notes":
                            if (values.Any(v => v.Append == true))
                            {
                                game.Notes += (string.IsNullOrWhiteSpace(game.Notes) ? "" : "\n") + string.Join("\n", values.SelectMany(v => v.Value));
                            }
                            else
                                game.Notes = string.Join(", ", values.SelectMany(v => v.Value));
                            logger.Info($"Set Notes to '{game.Notes}' for game '{game.Name}'");
                            break;

                        default:
                            logger.Warn($"Unknown metadata type: {metadataType}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to apply {metadataType} metadata to game '{game.Name}'");
                }
            }
        }

        private static void ApplyDatabaseObjectCollection<T>(
            Game game, 
            List<MetadataCollection> values, 
            IItemCollection<T> dbCollection, 
            Action<Game, List<Guid>> setter,
            string metadataTypeName) where T : DatabaseObject
        {
            var ids = new List<Guid>();

            foreach (var coll in values)
            {
                // Try to find existing item by name (case-insensitive)
                var value = coll.Value;
                var existingItem = dbCollection.FirstOrDefault(item => 
                    string.Equals(item.Name, value, StringComparison.OrdinalIgnoreCase));

                if (existingItem != null)
                {
                    ids.Add(existingItem.Id);
                    logger.Info($"Found existing {metadataTypeName}: '{value}' for game '{game.Name}'");
                }
                else
                {
                    // Create new item
                    var newItem = Activator.CreateInstance<T>();
                    newItem.Name = value;
                    dbCollection.Add(newItem);
                    ids.Add(newItem.Id);
                    logger.Info($"Created new {metadataTypeName}: '{value}' for game '{game.Name}'");
                }
            }

            if (ids.Any())
            {
                setter(game, ids);
            }
        }
    }
}
