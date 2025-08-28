using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Input;

namespace LocalLibrary
{
    public class ExportMetadataItem : ObservableObject
    {
        public string Name { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetValue(ref _isSelected, value); }
    }
    public class LocalLibrarySettings : ObservableObject
    {
        private bool _useactions = false;
        public bool UseActions { get => _useactions; set => SetValue(ref _useactions, value); }

        private bool _removeplay = false;
        public bool RemovePlay { get => _removeplay; set => SetValue(ref _removeplay, value); }

        private bool _findupdates = false;
        public bool FindUpdates { get => _findupdates; set => SetValue(ref _findupdates, value); }

        private ObservableCollection<GameSource> _pluginsources = new ObservableCollection<GameSource>();
        [DontSerialize] public ObservableCollection<GameSource> PluginSources { get => _pluginsources; set => SetValue(ref _pluginsources, value); }

        private ObservableCollection<GameSourceOption> _selectedsources = new ObservableCollection<GameSourceOption>();
        public ObservableCollection<GameSourceOption> SelectedSources { get => _selectedsources; set => SetValue(ref _selectedsources, value); }

        private ObservableCollection<Platform> _platforms = new ObservableCollection<Platform>();
        [DontSerialize] public ObservableCollection<Platform> Platforms { get => _platforms; set => SetValue(ref _platforms, value); }

        private string _selectedplatform = String.Empty;
        public string SelectedPlatform { get => _selectedplatform; set => SetValue(ref _selectedplatform, value); }

        private bool _usepaths = false;
        public bool UsePaths { get => _usepaths; set => SetValue(ref _usepaths, value); }

        private ObservableCollection<string> _installpaths = new ObservableCollection<string>();
        public ObservableCollection<string> InstallPaths { get => _installpaths; set => SetValue(ref _installpaths, value); }

        private ObservableCollection<string> _regexlist = new ObservableCollection<string>();
        public ObservableCollection<string> RegexList { get => _regexlist; set => SetValue(ref _regexlist, value); }

        private ObservableCollection<string> _stringlist = new ObservableCollection<string>();
        public ObservableCollection<string> StringList { get => _stringlist; set => SetValue(ref _stringlist, value); }

        private string _defaultRoot = string.Empty;
        public string DefaultRoot { get => _defaultRoot; set => SetValue(ref _defaultRoot, value); }

        private int _levenshtein = 100;
        public int Levenshtein { get => _levenshtein; set => SetValue(ref _levenshtein, value); }

        private bool _autoupdate = false;
        public bool AutoUpdate { get => _autoupdate; set => SetValue(ref _autoupdate, value); }

        private string _archivepath = string.Empty;
        public string ArchivePath { get => _archivepath; set => SetValue(ref _archivepath, value); }

        private bool _rb7z = true;
        public bool RB7z { get => _rb7z; set => SetValue(ref _rb7z, value); }

        private bool _rbrar = false;
        public bool RBRar { get => _rbrar; set => SetValue(ref _rbrar, value); }

        private string _selectedregex = String.Empty;
        [DontSerialize] public string SelectedRegex { get => _selectedregex; set => SetValue(ref _selectedregex, value); }
        
        private string _selectedstring = String.Empty;
        [DontSerialize] public string SelectedString { get => _selectedstring; set => SetValue(ref _selectedstring, value); }

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.

        private ObservableCollection<ExportMetadataItem> _exportMetadataItems = new ObservableCollection<ExportMetadataItem>();
        public ObservableCollection<ExportMetadataItem> ExportMetadataItems
        {
            get => _exportMetadataItems;
            set => SetValue(ref _exportMetadataItems, value);
        }

        // Grouped collections for Export tab
        private ObservableCollection<ExportMetadataItem> _exportTextElements = new ObservableCollection<ExportMetadataItem>();
        public ObservableCollection<ExportMetadataItem> ExportTextElements { get => _exportTextElements; set => SetValue(ref _exportTextElements, value); }

        private ObservableCollection<ExportMetadataItem> _exportStatusElements = new ObservableCollection<ExportMetadataItem>();
        public ObservableCollection<ExportMetadataItem> ExportStatusElements { get => _exportStatusElements; set => SetValue(ref _exportStatusElements, value); }

        private ObservableCollection<ExportMetadataItem> _exportScoreElements = new ObservableCollection<ExportMetadataItem>();
        public ObservableCollection<ExportMetadataItem> ExportScoreElements { get => _exportScoreElements; set => SetValue(ref _exportScoreElements, value); }

        private ObservableCollection<ExportMetadataItem> _exportDateElements = new ObservableCollection<ExportMetadataItem>();
        public ObservableCollection<ExportMetadataItem> ExportDateElements { get => _exportDateElements; set => SetValue(ref _exportDateElements, value); }

        private ObservableCollection<ExportMetadataItem> _exportMediaElements = new ObservableCollection<ExportMetadataItem>();
        public ObservableCollection<ExportMetadataItem> ExportMediaElements { get => _exportMediaElements; set => SetValue(ref _exportMediaElements, value); }

        private ObservableCollection<ExportMetadataItem> _exportScriptElements = new ObservableCollection<ExportMetadataItem>();
        public ObservableCollection<ExportMetadataItem> ExportScriptElements { get => _exportScriptElements; set => SetValue(ref _exportScriptElements, value); }

        private string _locationSetting = "Use Default Path";
        public string LocationSetting { get => _locationSetting; set => SetValue(ref _locationSetting, value); }

        private string _defaultMetadataPath = string.Empty;
        public string DefaultMetadataPath { get => _defaultMetadataPath; set => SetValue(ref _defaultMetadataPath, value); }

        private string _configSetting = "Use Default Metadata Set";
        public string ConfigSetting { get => _configSetting; set => SetValue(ref _configSetting, value); }

        private void InitializeExportMetadataGroups()
        {
            ExportTextElements.Clear();
            ExportStatusElements.Clear();
            ExportScoreElements.Clear();
            ExportDateElements.Clear();
            ExportMediaElements.Clear();
            ExportScriptElements.Clear();

            string[] textElements = { "Name", "Sorting Name", "Series", "Description", "Region", "Platforms", "Categories", "Features", "Genres", "Links", "Tags", "Version", "Developers", "Publishers", "Source", "AgeRatings" };
            string[] statusElements = { "CompletionStatus", "Hidden", "Favorite", "Enable HDR Support" };
            string[] scoreElements = { "UserScore", "CriticScore", "CommunityScore" };
            string[] dateElements = { "ReleaseDate", "Added", "Modified", "TimePlayed", "PlayCount", "LastActivity" };
            string[] mediaElements = { "Icon", "CoverImage", "BackgroundImage", "Logo", "Manual" };
            string[] scriptElements = { "GameStartedScript", "PostScript", "PreScript" };
            foreach (var name in textElements) ExportTextElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in statusElements) ExportStatusElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in scoreElements) ExportScoreElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in dateElements) ExportDateElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in mediaElements) ExportMediaElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in scriptElements) ExportScriptElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
        }

        public void ResetExportMetadataGroups()
        {
            ExportTextElements.Clear();
            ExportStatusElements.Clear();
            ExportScoreElements.Clear();
            ExportDateElements.Clear();
            ExportMediaElements.Clear();
            ExportScriptElements.Clear();

            string[] textElements = { "Name", "Sorting Name", "Series", "Description", "Region", "Platforms", "Categories", "Features", "Genres", "Links", "Tags", "Version", "Developers", "Publishers", "Source", "AgeRatings" };
            string[] statusElements = { "CompletionStatus", "Hidden", "Favorite", "Enable HDR Support" };
            string[] scoreElements = { "UserScore", "CriticScore", "CommunityScore" };
            string[] dateElements = { "ReleaseDate", "Added", "Modified", "TimePlayed", "PlayCount", "LastActivity" };
            string[] mediaElements = { "Icon", "CoverImage", "BackgroundImage", "Logo", "Manual" };
            string[] scriptElements = { "GameStartedScript", "PostScript", "PreScript" };
            foreach (var name in textElements) ExportTextElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in statusElements) ExportStatusElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in scoreElements) ExportScoreElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in dateElements) ExportDateElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in mediaElements) ExportMediaElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
            foreach (var name in scriptElements) ExportScriptElements.Add(new ExportMetadataItem { Name = name, IsSelected = false });
        }

        public LocalLibrarySettings()
        {
            // Do not auto-initialize grouped collections here
        }
    }

    public class LocalLibrarySettingsViewModel : ObservableObject, ISettings
    {
        private readonly LocalLibrary plugin;
        private LocalLibrarySettings EditingClone { get; set; }

        private LocalLibrarySettings settings;
        public LocalLibrarySettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        private string _newRegexText;
        public string NewRegexText
        {
            get => _newRegexText;
            set
            {
                _newRegexText = value;
                OnPropertyChanged(nameof(NewRegexText));
            }
        }

        private string _newStringText;
        public string NewStringText
        {
            get => _newStringText;
            set
            {
                _newStringText = value;
                OnPropertyChanged(nameof(NewStringText));
            }
        }

        public RelayCommand<IList<object>> AddSourceCommand
            => new RelayCommand<IList<object>>((items) =>
            {
                var itemsToProcess = items.OfType<GameSource>().ToList();
                
                foreach (GameSource item in itemsToProcess)
                {
                    var newOption = new GameSourceOption(item.Id, item.Name, false);
                    settings.SelectedSources.AddMissing(newOption);
                    settings.PluginSources.Remove(item);
                }
            }, (items) => items?.Any() ?? false);

        public RelayCommand<IList<object>> RemoveSourceCommand
            => new RelayCommand<IList<object>>((items) =>
            {
                var itemsToProcess = items.OfType<GameSourceOption>().ToList();

                foreach (GameSourceOption item in itemsToProcess)
                {
                    settings.SelectedSources.Remove(item);
                    GameSource source = API.Instance.Database.Sources.FirstOrDefault(a => a.Id == item.Id);
                    settings.PluginSources.AddMissing(source);
                }
            }, (items) => items?.Any() ?? false);

        public RelayCommand CreateSourceCommand
            => new RelayCommand(() =>
            {
                string sourcename = string.Empty;
                var selection = API.Instance.Dialogs.SelectString("New Source Name:", "New Source", "");
                if (selection.Result)
                {
                    sourcename = selection.SelectedString;
                }
                else
                {
                    API.Instance.Dialogs.ShowMessage("No source name entered.");
                    return;
                }
                GameSource source = new GameSource(sourcename);
                API.Instance.Database.Sources.Add(source);
                var newOption = new GameSourceOption(source.Id, source.Name, false);
                settings.SelectedSources.AddMissing(newOption);
            });

        public RelayCommand<GameSourceOption> SetPrimaryCommand
            => new RelayCommand<GameSourceOption>((selectedSource) =>
            {
                // Clear previous primary settings
                foreach (var source in settings.SelectedSources)
                {
                    source.IsPrimary = false;
                }

                if (selectedSource != null)
                {
                    selectedSource.IsPrimary = true;
                }
            });

        public RelayCommand AddPathCommand
            => new RelayCommand(() =>
            {
                string value = API.Instance.Dialogs.SelectFolder();

                settings.InstallPaths.AddMissing(value);
                settings.InstallPaths = new ObservableCollection<string>(settings.InstallPaths.OrderBy(x => x));
            });

        public RelayCommand<IList<object>> RemovePathCommand
            => new RelayCommand<IList<object>>((items) =>
            {
                var itemsToProcess = items.OfType<string>().ToList();

                foreach (string item in itemsToProcess)
                {
                    settings.InstallPaths.Remove(item);
                }
            }, (items) => items?.Any() ?? false);

        public RelayCommand AddRegexCommand
            => new RelayCommand(() =>
            {
                string value = NewRegexText;
                if (string.IsNullOrWhiteSpace(NewRegexText))
                {
                    return;
                }
                settings.RegexList.AddMissing(value);
                NewRegexText = string.Empty;
            });

        public RelayCommand<IList<object>> RemoveRegexCommand
            => new RelayCommand<IList<object>>((items) =>
            {
                var itemsToProcess = items.OfType<string>().ToList();

                foreach (string item in itemsToProcess)
                {
                    settings.RegexList.Remove(item);
                }
            }, (items) => items?.Any() ?? false);

        public RelayCommand AddStringCommand
            => new RelayCommand(() =>
            {
                string value = NewStringText;
                if (string.IsNullOrWhiteSpace(NewStringText))
                {
                    return;
                }
                settings.StringList.AddMissing(value);
                NewStringText = string.Empty;
            });

        public RelayCommand<IList<object>> RemoveStringCommand
            => new RelayCommand<IList<object>>((items) =>
            {
                var itemsToProcess = items.OfType<string>().ToList();

                foreach (string item in itemsToProcess)
                {
                    settings.StringList.Remove(item);
                }
            }, (items) => items?.Any() ?? false);

        public RelayCommand ApplyPluginIdCommand => new RelayCommand(() =>
        {
            LocalLibrary.PluginIdUpdate(Settings.SelectedSources);
        });

        public RelayCommand AddGamesCommand => new RelayCommand(() =>
        {
            Finder addGames = new Finder();
            var ignorelist = Settings.RegexList.Select(item => new MergedItem { Value = item, Source = "Regex" })
                .Concat(Settings.StringList.Select(item => new MergedItem { Value = item, Source = "String" }))
                .ToList();
            addGames.FindInstallers(Settings.InstallPaths.ToList(), Settings, ignorelist);
        });

        public RelayCommand ArchiveBrowseCommand => new RelayCommand(() =>
        {
            string archivepath = API.Instance.Dialogs.SelectFile("Unarchive Executable|*.exe");
            if (!string.IsNullOrEmpty(archivepath))
            {
                Settings.ArchivePath = archivepath;
                Settings.RBRar = archivepath.IndexOf("winrar", StringComparison.OrdinalIgnoreCase) >= 0;
                Settings.RB7z = archivepath.IndexOf("7z", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        });

        public RelayCommand DefaultBrowseCommand => new RelayCommand(() =>
        {
            string defaultRoot = API.Instance.Dialogs.SelectFolder();
            if (!string.IsNullOrEmpty(defaultRoot))
            {
                Settings.DefaultRoot = defaultRoot;
            }
        });

        public LocalLibrarySettingsViewModel(LocalLibrary plugin)
        {
            this.plugin = plugin;
            var savedSettings = plugin.LoadPluginSettings<LocalLibrarySettings>();
            Settings = savedSettings ?? new LocalLibrarySettings();

            Settings.InstallPaths = Settings.InstallPaths is null
                ? new ObservableCollection<string>()
                : new ObservableCollection<string>(Settings.InstallPaths.OrderBy(x => x).ToList());
            Settings.RegexList = Settings.RegexList is null
                ? new ObservableCollection<string>()
                : new ObservableCollection<string>(Settings.RegexList.OrderBy(x => x).ToList());
            Settings.StringList = Settings.StringList is null
                ? new ObservableCollection<string>()
                : new ObservableCollection<string>(Settings.StringList.OrderBy(x => x).ToList());
            Settings.SelectedSources = Settings.SelectedSources is null
                ? new ObservableCollection<GameSourceOption>()
                : new ObservableCollection<GameSourceOption>(Settings.SelectedSources.OrderBy(x => x.Name).ToList());
            Settings.PluginSources = Settings.PluginSources is null
                ? new ObservableCollection<GameSource>()
                : new ObservableCollection<GameSource>(Settings.PluginSources.OrderBy(x => x.Name).ToList());
            Settings.Platforms = Settings.Platforms is null
                ? new ObservableCollection<Platform>()
                : new ObservableCollection<Platform>(Settings.Platforms.OrderBy(x => x.Name).ToList());

            // Ensure grouped metadata collections are correct (no duplicates, all items present)
            EnsureExportMetadataGroups();
        }

        private void EnsureExportMetadataGroups()
        {
            // If any group is empty or has duplicates, reset all
            bool needsReset =
                HasDuplicatesOrMissing(Settings.ExportTextElements, new[] { "Name", "Sorting Name", "Series", "Description", "Region", "Platforms", "Categories", "Features", "Genres", "Links", "Tags", "Version", "Developers", "Publishers", "Source", "AgeRatings" }) ||
                HasDuplicatesOrMissing(Settings.ExportStatusElements, new[] { "CompletionStatus", "Hidden", "Favorite", "Enable HDR Support" }) ||
                HasDuplicatesOrMissing(Settings.ExportScoreElements, new[] { "UserScore", "CriticScore", "CommunityScore" }) ||
                HasDuplicatesOrMissing(Settings.ExportDateElements, new[] { "ReleaseDate", "Added", "Modified", "TimePlayed", "PlayCount", "LastActivity" }) ||
                HasDuplicatesOrMissing(Settings.ExportMediaElements, new[] { "Icon", "CoverImage", "BackgroundImage", "Logo", "Manual" }) ||
                HasDuplicatesOrMissing(Settings.ExportScriptElements, new[] { "GameStartedScript", "PostScript", "PreScript" });
            if (needsReset)
            {
                Settings.ResetExportMetadataGroups();
            }
        }

        private bool HasDuplicatesOrMissing(ObservableCollection<ExportMetadataItem> collection, string[] expectedNames)
        {
            var names = collection.Select(x => x.Name).ToList();
            return names.Count != expectedNames.Length || names.Distinct().Count() != expectedNames.Length || expectedNames.Except(names).Any();
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            EditingClone = Serialization.GetClone(Settings);

            Settings.PluginSources = GetSources();

            ObservableCollection<GameSource> GetSources()
            {
                var avoidsources = new[] { "Steam", "Gog", "Amazon", "Battle.Net", "Epic", "Legacy Games", "Playstation", "Xbox",
                                            "Xbox Game Pass", "Ubisoft Connect", "EA app", "Humble", "itch.io", "Google Play Games" };
                ObservableCollection<GameSource> Sources = new ObservableCollection<GameSource>();
                foreach (var source in plugin.PlayniteApi.Database.Sources)
                {
                    if ( avoidsources.Contains(source.Name, StringComparer.OrdinalIgnoreCase) )
                    {
                        continue;
                    }
                    Sources.Add(source);
                }
                return Sources;
            }

            Settings.Platforms = GetPlatforms();

            ObservableCollection<Platform> GetPlatforms()
            {
                ObservableCollection<Platform> Platforms = new ObservableCollection<Platform>();
                foreach (var platform in plugin.PlayniteApi.Database.Platforms)
                {
                    Platforms.Add(platform);
                }
                return Platforms;
            }


        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = EditingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }

        public RelayCommand ResetExportMetadataCommand => new RelayCommand(() =>
        {
            // List of unchecked items
            var uncheckedItems = new HashSet<string>(new[] {
                "Sorting Name", "Region", "Categories", "Version", "Source",
                "Hidden", "Enable HDR Support",
                "CriticScore", "CommunityScore",
                "Added", "Modified", "TimePlayed", "PlayCount", "LastActivity",
                "Manual",
                "GameStartedScript", "PostScript", "PreScript"
            });
            foreach (var item in Settings.ExportTextElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.ExportStatusElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.ExportScoreElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.ExportDateElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.ExportMediaElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.ExportScriptElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
        });
    }
}