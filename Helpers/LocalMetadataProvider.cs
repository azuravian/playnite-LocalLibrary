using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LocalLibrary.Helpers
{
    public class LocalMetadataProvider : LibraryMetadataProvider
    {
        private readonly ILogger logger = LogManager.GetLogger();

        private static string FindBackupImage(string folder, string baseName)
        {
            string Image = Directory
                .GetFiles(folder, $"{baseName}.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (Image != null)
            {
                return Image;
            }
            else
            {
                return null;
            }
        }

        public override GameMetadata GetMetadata(Game game)
        {
            if (game == null) return null;

            try
            {
                LocalLibrarySettings Settings = new LocalLibrarySettings();
                Finder iFinder = new Finder();
                // Construct path to metadata JSON file for this game
                var safeName = $"{StringExtensions.CleanString(game.Name)}_metadata.json";
                var installer = iFinder.GetActionsRoms(game, Settings).Item1;
                var gameFolder = Path.GetDirectoryName(installer);
                var metadataDir = Path.Combine(gameFolder, Settings.MetadataRelPath);
                var metadataPath = Path.Combine(gameFolder, Settings.MetadataRelPath, safeName);

                if (!File.Exists(metadataPath))
                    return null; // No disk metadata — fallback to default providers

                // Read and deserialize disk metadata
                var json = File.ReadAllText(metadataPath);
                var metadata = JsonConvert.DeserializeObject<LocalGameMetadata>(json);

                if (metadata == null)
                    return null;

                // Get images from metadata folder
                var bgImage = FindBackupImage(metadataDir, "Background");
                var coverImage = FindBackupImage(metadataDir, "Cover");
                var iconImage = FindBackupImage(metadataDir, "Icon");
                MetadataFile Background = new MetadataFile(bgImage);
                MetadataFile Cover = new MetadataFile(coverImage);
                MetadataFile Icon = new MetadataFile(iconImage);

                // Map your GameMetadata class to Playnite's GameMetadata object
                var playniteMetadata = new GameMetadata
                {
                    Name = metadata.Name,
                    SortingName = metadata.SortingName,
                    Description = metadata.Description,
                    Version = metadata.Version,
                    
                    Favorite = metadata.Favorite,
                    
                    Hidden = metadata.Hidden,

                    BackgroundImage = Background,
                    CoverImage = Cover,
                    Icon = Icon,
                    
                    CriticScore = metadata.CriticScore,
                    CommunityScore = metadata.CommunityScore,
                    UserScore = metadata.UserScore,
                    PlayCount = metadata.PlayCount,
                    Playtime = metadata.Playtime,
                    
                    LastActivity = metadata.LastActivity,
                    
                    ReleaseDate = metadata.ReleaseDate,
                    CompletionStatus = !string.IsNullOrEmpty(metadata.CompletionStatus)
                        ? new MetadataNameProperty(metadata.CompletionStatus)
                        : null,
                    Source = !string.IsNullOrEmpty(metadata.Source)
                        ? new MetadataNameProperty(metadata.Source)
                        : null
                };

                // Map lists (tags, platforms, genres, etc.)
                playniteMetadata.Tags = metadata.Tags?
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Select(tag => new MetadataNameProperty(tag))
                    .ToHashSet<MetadataProperty>();

                playniteMetadata.Platforms = metadata.Platforms?
                    .Where(plat => !string.IsNullOrWhiteSpace(plat))
                    .Select(plat => new MetadataNameProperty(plat))
                    .ToHashSet<MetadataProperty>();

                playniteMetadata.Genres = metadata.Genres?
                    .Where(gen => !string.IsNullOrWhiteSpace(gen))
                    .Select(gen => new MetadataNameProperty(gen))
                    .ToHashSet<MetadataProperty>();

                playniteMetadata.Series = metadata.Series?
                    .Where(ser => !string.IsNullOrWhiteSpace(ser))
                    .Select(ser => new MetadataNameProperty(ser))
                    .ToHashSet<MetadataProperty>();

                playniteMetadata.Regions = metadata.Regions?
                    .Where(reg => !string.IsNullOrWhiteSpace(reg))
                    .Select(reg => new MetadataNameProperty(reg))
                    .ToHashSet<MetadataProperty>();

                playniteMetadata.Developers = metadata.Developers?
                    .Where(dev => !string.IsNullOrWhiteSpace(dev))
                    .Select(dev => new MetadataNameProperty(dev))
                    .ToHashSet<MetadataProperty>();

                playniteMetadata.Publishers = metadata.Publishers?
                    .Where(pub => !string.IsNullOrWhiteSpace(pub))
                    .Select(pub => new MetadataNameProperty(pub))
                    .ToHashSet<MetadataProperty>();

                playniteMetadata.Features = metadata.Features?
                    .Where(feat => !string.IsNullOrWhiteSpace(feat))
                    .Select(feat => new MetadataNameProperty(feat))
                    .ToHashSet<MetadataProperty>();

                playniteMetadata.Categories = metadata.Categories?
                    .Where(cat => !string.IsNullOrWhiteSpace(cat))
                    .Select(cat => new MetadataNameProperty(cat))
                    .ToHashSet<MetadataProperty>();

                // Links
                playniteMetadata.Links = metadata.Links?.ToList();

                return playniteMetadata;
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to read metadata for game {game.Name}");
                return null; // fallback to default providers
            }
        }
    }
}
