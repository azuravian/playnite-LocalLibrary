using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalLibrary.Helpers
{
    public class LocalGameMetadata
    {
        //Strings
        public string Name { get; set; }
        public string SortingName { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Notes { get; set; }
        
        //Scripts
        public string GameStartedScript { get; set; }
        public string PostScript { get; set; }
        public string PreScript { get; set; }
        public bool Favorite { get; set; }
        public bool EnableSystemHdr { get; set; }
        public bool Hidden { get; set; }

        // Media
        public string BackgroundImage { get; set; }
        public string CoverImage { get; set; }
        public string Icon { get; set; }
        public string Manual { get; set; }

        // numeric
        public int? CriticScore { get; set; }
        public int? CommunityScore { get; set; }
        public int? UserScore { get; set; }
        public ulong PlayCount { get; set; }
        public ulong Playtime { get; set; } // In seconds

        // Lists
        public List<string> AgeRatings { get; set; }
        public List<string> Platforms { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Series { get; set; }
        public List<string> Regions { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> Features { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Genres { get; set; }
        public ObservableCollection<Link> Links { get; set; }

        //Custom
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? Added { get; set; }
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? LastActivity { get; set; }
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? Modified { get; set; }

        [JsonConverter(typeof(ReleaseDateConverter))]
        public ReleaseDate? ReleaseDate { get; set; }



        public string CompletionStatus { get; set; }
        public string Source { get; set; }
        // Add any other properties relevant for export/import

        //Constructor
        public LocalGameMetadata(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            // Strings
            Name = game.Name;
            SortingName = game.SortingName;
            Description = game.Description;
            Version = game.Version;
            Notes = game.Notes;

            // Scripts
            GameStartedScript = game.GameStartedScript;
            PostScript = game.PostScript;
            PreScript = game.PreScript;

            // Booleans
            Favorite = game.Favorite;
            EnableSystemHdr = game.EnableSystemHdr;
            Hidden = game.Hidden;

            // Media
            BackgroundImage = game.BackgroundImage;
            CoverImage = game.CoverImage;
            Icon = game.Icon;
            Manual = game.Manual;

            // Numeric
            CriticScore = game.CriticScore;
            CommunityScore = game.CommunityScore;
            UserScore = game.UserScore;
            PlayCount = game.PlayCount;
            Playtime = game.Playtime;

            // Lists
            AgeRatings = game.AgeRatings?.Select(a => a.Name).ToList() ?? new List<string>();
            Platforms = game.Platforms?.Select(p => p.Name).ToList() ?? new List<string>();
            Tags = game.Tags?.Select(t => t.Name).ToList() ?? new List<string>();
            Series = game.Series?.Select(s => s.Name).ToList() ?? new List<string>();
            Regions = game.Regions?.Select(r => r.Name).ToList() ?? new List<string>();
            Developers = game.Developers?.Select(d => d.Name).ToList() ?? new List<string>();
            Publishers = game.Publishers?.Select(p => p.Name).ToList() ?? new List<string>();
            Features = game.Features?.Select(f => f.Name).ToList() ?? new List<string>();
            Categories = game.Categories?.Select(c => c.Name).ToList() ?? new List<string>();
            Genres = game.Genres?.Select(g => g.Name).ToList() ?? new List<string>();
            Links = new ObservableCollection<Link>(game.Links ?? new ObservableCollection<Link>());

            // Dates
            Added = game.Added;
            LastActivity = game.LastActivity;
            Modified = game.Modified;
            ReleaseDate = game.ReleaseDate;

            // Custom
            CompletionStatus = game.CompletionStatus?.ToString() ?? string.Empty;
            Source = game.Source?.Name ?? string.Empty;
        }
    }

    public static class LocalGameMetadataMapper
    {
        public static Game CreateGameFromMetadata(LocalGameMetadata metadata, Guid pluginId, Guid sourceId)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var game = new Game
            {
                Name = metadata.Name,
                SortingName = metadata.SortingName,
                Description = metadata.Description,
                Version = metadata.Version,
                Notes = metadata.Notes,

                GameStartedScript = metadata.GameStartedScript,
                PostScript = metadata.PostScript,
                PreScript = metadata.PreScript,
                Favorite = metadata.Favorite,
                EnableSystemHdr = metadata.EnableSystemHdr,
                Hidden = metadata.Hidden,

                BackgroundImage = metadata.BackgroundImage,
                CoverImage = metadata.CoverImage,
                Icon = metadata.Icon,
                Manual = metadata.Manual,

                CriticScore = metadata.CriticScore,
                CommunityScore = metadata.CommunityScore,
                UserScore = metadata.UserScore,
                PlayCount = metadata.PlayCount,
                Playtime = metadata.Playtime,

                Added = metadata.Added ?? DateTime.Now,
                LastActivity = metadata.LastActivity,
                Modified = metadata.Modified,
                ReleaseDate = metadata.ReleaseDate,

                PluginId = pluginId,
                SourceId = sourceId
            };

            // --- Lists ---
            foreach (string tag in metadata.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    Tag newtag = new Tag(tag);
                    if (!game.Tags.Contains(newtag))
                    {
                        game.Tags.Add(newtag);
                    }
                    
                }
            }
            
            foreach (string agerating in metadata.AgeRatings)
            {
                if (!string.IsNullOrWhiteSpace(agerating))
                {
                    AgeRating newagerating = new AgeRating(agerating);
                    if (!game.AgeRatings.Contains(newagerating))
                    {
                        game.AgeRatings.Add(newagerating);
                    }
                }
            }

            foreach (string platform in metadata.Platforms)
            {
                if (!string.IsNullOrWhiteSpace(platform))
                {
                    Platform newplatform = new Platform(platform);
                    if (!game.Platforms.Contains(newplatform))
                    {
                        game.Platforms.Add(newplatform);
                    }
                }
            }

            foreach (string series in metadata.Series)
            {
                if (!string.IsNullOrWhiteSpace(series))
                {
                    Series newseries = new Series(series);
                    if (!game.Series.Contains(newseries))
                    {
                        game.Series.Add(newseries);
                    }
                }
            }

            foreach (string region in metadata.Regions)
            {
                if (!string.IsNullOrWhiteSpace(region))
                {
                    Region newregion = new Region(region);
                    if (!game.Regions.Contains(newregion))
                    {
                        game.Regions.Add(newregion);
                    }
                }
            }

            foreach (string developer in metadata.Developers)
            {
                if (!string.IsNullOrWhiteSpace(developer))
                {
                    Company newdeveloper = new Company(developer);
                    if (!game.Developers.Contains(newdeveloper))
                    {
                        game.Developers.Add(newdeveloper);
                    }
                }
            }

            foreach (string publisher in metadata.Publishers)
            {
                if (!string.IsNullOrWhiteSpace(publisher))
                {
                    Company newpublisher = new Company(publisher);
                    if (!game.Publishers.Contains(newpublisher))
                    {
                        game.Publishers.Add(newpublisher);
                    }
                }
            }

            foreach (string feature in metadata.Features)
            {
                if (!string.IsNullOrWhiteSpace(feature))
                {
                    GameFeature newfeature = new GameFeature(feature);
                    if (!game.Features.Contains(newfeature))
                    {
                        game.Features.Add(newfeature);
                    }
                }
            }

            foreach (string category in metadata.Categories)
            {
                if (!string.IsNullOrWhiteSpace(category))
                {
                    var cat = new Category(category);
                    if (cat != null && !game.Categories.Contains(cat))
                    {
                        game.Categories.Add(cat);
                    }
                }
            }

            foreach (string genre in metadata.Genres)
            {
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    var gen = new Genre(genre);
                    if (gen != null && !game.Genres.Contains(gen))
                    {
                        game.Genres.Add(gen);
                    }
                }
            }

            game.Links = metadata.Links != null
                ? new ObservableCollection<Link>(metadata.Links)
                : new ObservableCollection<Link>();

            // --- Custom / Playnite-specific ---
            if (!string.IsNullOrEmpty(metadata.CompletionStatus))
            {
                game.CompletionStatus.Name = metadata.CompletionStatus;
            }

            if (!string.IsNullOrEmpty(metadata.Source))
            {
                game.Source.Name = metadata.Source;
            }

            return game;
        }
    }

}
