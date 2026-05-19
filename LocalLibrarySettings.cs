using LocalLibrary.Helpers;
using LocalLibrary.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;

namespace LocalLibrary
{
    public class MetadataItem : ObservableObject
    {
        public string Name { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetValue(ref _isSelected, value); }
    }

    public class MetadataSelection
    {
        public List<string> TextElements { get; set; } = new List<string>();
        public List<string> StatusElements { get; set; } = new List<string>();
        public List<string> NumericElements { get; set; } = new List<string>();
        public List<string> DateElements { get; set; } = new List<string>();
        public List<string> MediaElements { get; set; } = new List<string>();
        public List<string> ScriptElements { get; set; } = new List<string>();
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

        private ObservableCollection<string> _locationoptions = new ObservableCollection<string>
        {
            "Use Default Path",
            "Prompt for Location"
        };
        [DontSerialize] public ObservableCollection<string> LocationOptions { get => _locationoptions; set => SetValue(ref _locationoptions, value); }


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

        private string _defaultRoot = string.Empty;
        public string DefaultRoot { get => _defaultRoot; set => SetValue(ref _defaultRoot, value); }

        private string _selectedregex = String.Empty;
        [DontSerialize] public string SelectedRegex { get => _selectedregex; set => SetValue(ref _selectedregex, value); }
        
        private string _selectedstring = String.Empty;
        [DontSerialize] public string SelectedString { get => _selectedstring; set => SetValue(ref _selectedstring, value); }

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.

        private ObservableCollection<ReplaceRule> _replaceRules = new ObservableCollection<ReplaceRule>();

        public ObservableCollection<ReplaceRule> ReplaceRules 
        { 
            get => _replaceRules;
            set => SetValue(ref _replaceRules, value);
        }
            

        private ObservableCollection<MetadataItem> _MetadataItems = new ObservableCollection<MetadataItem>();
        public ObservableCollection<MetadataItem> MetadataItems
        {
            get => _MetadataItems;
            set => SetValue(ref _MetadataItems, value);
        }

        // Grouped collections for Metadata tab
        private ObservableCollection<MetadataItem> _TextElements = new ObservableCollection<MetadataItem>();
        public ObservableCollection<MetadataItem> TextElements { get => _TextElements; set => SetValue(ref _TextElements, value); }

        private ObservableCollection<MetadataItem> _StatusElements = new ObservableCollection<MetadataItem>();
        public ObservableCollection<MetadataItem> StatusElements { get => _StatusElements; set => SetValue(ref _StatusElements, value); }

        private ObservableCollection<MetadataItem> _NumericElements = new ObservableCollection<MetadataItem>();
        public ObservableCollection<MetadataItem> NumericElements { get => _NumericElements; set => SetValue(ref _NumericElements, value); }

        private ObservableCollection<MetadataItem> _DateElements = new ObservableCollection<MetadataItem>();
        public ObservableCollection<MetadataItem> DateElements { get => _DateElements; set => SetValue(ref _DateElements, value); }

        private ObservableCollection<MetadataItem> _MediaElements = new ObservableCollection<MetadataItem>();
        public ObservableCollection<MetadataItem> MediaElements { get => _MediaElements; set => SetValue(ref _MediaElements, value); }

        private ObservableCollection<MetadataItem> _ScriptElements = new ObservableCollection<MetadataItem>();
        public ObservableCollection<MetadataItem> ScriptElements { get => _ScriptElements; set => SetValue(ref _ScriptElements, value); }

        private string _locationSetting = "Use Default Path";
        public string LocationSetting { get => _locationSetting; set => SetValue(ref _locationSetting, value); }

        private string _metadataRelPath = string.Empty;
        public string MetadataRelPath { get => _metadataRelPath; set => SetValue(ref _metadataRelPath, value); }

        private bool _importMetadataOnAdd = false;
        public bool ImportMetadataOnAdd { get => _importMetadataOnAdd; set => SetValue(ref _importMetadataOnAdd, value); }

        private DateTime _lastAutoUpdateTime = DateTime.MinValue;
        public DateTime LastAutoUpdateTime { get => _lastAutoUpdateTime; set => SetValue(ref _lastAutoUpdateTime, value); }

        private ObservableCollection<MetadataExtractionRule> _metadataExtractionRules = new ObservableCollection<MetadataExtractionRule>();
        public ObservableCollection<MetadataExtractionRule> MetadataExtractionRules
        {
            get => _metadataExtractionRules;
            set => SetValue(ref _metadataExtractionRules, value);
        }

        private void InitializeMetadataGroups()
        {
            TextElements.Clear();
            StatusElements.Clear();
            NumericElements.Clear();
            DateElements.Clear();
            MediaElements.Clear();
            ScriptElements.Clear();

            string[] textElements = { "Name", "SortingName", "Series", "Description", "Region", "Platforms", "Categories", "Features", "Genres", "Links", "Tags", "Version", "Developers", "Publishers", "Source", "AgeRatings", "Notes" };
            string[] statusElements = { "CompletionStatus", "Hidden", "Favorite", "EnableHDRSupport" };
            string[] numericElements = { "UserScore", "CriticScore", "CommunityScore", "PlayCount", "TimePlayed" };
            string[] dateElements = { "ReleaseDate", "Added", "Modified", "LastActivity" };
            string[] mediaElements = { "Icon", "CoverImage", "BackgroundImage", "Logo", "Manual" };
            string[] scriptElements = { "GameStartedScript", "PostScript", "PreScript" };
            foreach (var name in textElements) TextElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in statusElements) StatusElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in numericElements) NumericElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in dateElements) DateElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in mediaElements) MediaElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in scriptElements) ScriptElements.Add(new MetadataItem { Name = name, IsSelected = false });
        }

        public void ResetMetadataGroups()
        {
            TextElements.Clear();
            StatusElements.Clear();
            NumericElements.Clear();
            DateElements.Clear();
            MediaElements.Clear();
            ScriptElements.Clear();

            string[] textElements = { "Name", "SortingName", "Series", "Description", "Region", "Platforms", "Categories", "Features", "Genres", "Links", "Tags", "Version", "Developers", "Publishers", "Source", "AgeRatings", "Notes" };
            string[] statusElements = { "CompletionStatus", "Hidden", "Favorite", "EnableHDRSupport" };
            string[] scoreElements = { "UserScore", "CriticScore", "CommunityScore", "PlayCount", "TimePlayed" };
            string[] dateElements = { "ReleaseDate", "Added", "Modified", "LastActivity" };
            string[] mediaElements = { "Icon", "CoverImage", "BackgroundImage", "Logo", "Manual" };
            string[] scriptElements = { "GameStartedScript", "PostScript", "PreScript" };
            foreach (var name in textElements) TextElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in statusElements) StatusElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in scoreElements) NumericElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in dateElements) DateElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in mediaElements) MediaElements.Add(new MetadataItem { Name = name, IsSelected = false });
            foreach (var name in scriptElements) ScriptElements.Add(new MetadataItem { Name = name, IsSelected = false });
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

        public ObservableCollection<string> TypeOptions { get; } = new ObservableCollection<string>
        {
            "String",
            "Regex"
        };  

        public ObservableCollection<string> MetadataTypeOptions { get; } = new ObservableCollection<string>
        {
            "Version",
            "Genre",
            "Platform",
            "Tag",
            "Feature",
            "Category",
            "Series",
            "UserScore",
            "Notes"
        };

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
                
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                // Check for path containment conflicts
                string normalizedNewPath;
                try
                {
                    normalizedNewPath = Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                catch
                {
                    API.Instance.Dialogs.ShowMessage("Invalid path selected.");
                    return;
                }

                foreach (var existingPath in settings.InstallPaths)
                {
                    if (string.IsNullOrWhiteSpace(existingPath))
                        continue;

                    string normalizedExisting;
                    try
                    {
                        normalizedExisting = Path.GetFullPath(existingPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    }
                    catch
                    {
                        continue;
                    }

                    // Check if paths are identical
                    if (string.Equals(normalizedNewPath, normalizedExisting, StringComparison.OrdinalIgnoreCase))
                    {
                        API.Instance.Dialogs.ShowMessage("This path is already in the list.");
                        return;
                    }

                    // Check if new path contains existing path
                    if ((normalizedExisting + Path.DirectorySeparatorChar).StartsWith(normalizedNewPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        API.Instance.Dialogs.ShowMessage($"Cannot add path because it contains existing path:\n{existingPath}");
                        return;
                    }

                    // Check if existing path contains new path
                    if ((normalizedNewPath + Path.DirectorySeparatorChar).StartsWith(normalizedExisting + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        API.Instance.Dialogs.ShowMessage($"Cannot add path because it's contained within existing path:\n{existingPath}");
                        return;
                    }
                }

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

        public RelayCommand AddReplaceRuleCommand
            => new RelayCommand(() =>
            {
                if (settings.ReplaceRules == null)
                {
                    settings.ReplaceRules = new ObservableCollection<ReplaceRule>();
                }
                settings.ReplaceRules.Add(new ReplaceRule { Pattern = string.Empty, Replacement = string.Empty });
            });

        public RelayCommand<ReplaceRule> RemoveReplaceRuleCommand
            => new RelayCommand<ReplaceRule>((rule) =>
            {
                if (settings.ReplaceRules != null && rule != null)
                {
                    settings.ReplaceRules.Remove(rule);
                }
            });

        public RelayCommand AddMetadataExtractionRuleCommand
            => new RelayCommand(() =>
            {
                if (settings.MetadataExtractionRules == null)
                {
                    settings.MetadataExtractionRules = new ObservableCollection<MetadataExtractionRule>();
                }
                settings.MetadataExtractionRules.Add(new MetadataExtractionRule 
                {
                    MetadataType = "",
                    OpenDelimiter = "", 
                    CloseDelimiter = "", 
                    Pattern = "",
                    Append = false
                });
            });

        public RelayCommand<MetadataExtractionRule> RemoveMetadataExtractionRuleCommand
            => new RelayCommand<MetadataExtractionRule>((rule) =>
            {
                if (settings.MetadataExtractionRules != null && rule != null)
                {
                    settings.MetadataExtractionRules.Remove(rule);
                }
            });

        public RelayCommand ApplyPluginIdCommand => new RelayCommand(() =>
        {
            LocalLibrary.PluginIdUpdate(Settings.SelectedSources);
        });

        public RelayCommand AddGamesCommand => new RelayCommand(() =>
        {
            Finder addGames = new Finder();
            var replacerules = Settings.ReplaceRules?.ToList() ?? new List<ReplaceRule>();
            addGames.FindInstallers(Settings.InstallPaths.ToList(), Settings, replacerules);
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
            foreach (var item in Settings.TextElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.StatusElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.NumericElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.DateElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.MediaElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
            foreach (var item in Settings.ScriptElements)
                item.IsSelected = !uncheckedItems.Contains(item.Name);
        });

        public RelayCommand MetadataBrowseCommand => new RelayCommand(() =>
        {
            string metadataFullPath = API.Instance.Dialogs.SelectFolder();
            if (!string.IsNullOrEmpty(metadataFullPath))
            {
                PathUtils.TryGetRelativePathFromRoots(
                    metadataFullPath,
                    Settings.InstallPaths,
                    out string relPath,
                    2);

                if (string.IsNullOrEmpty(relPath))
                {
                    API.Instance.Dialogs.ShowMessage("Selected path must be relative to one of your game install folders.");
                    return;
                }

                Settings.MetadataRelPath = relPath;
            }
        });

        public RelayCommand ExportMetadataCommand => new RelayCommand(() =>
        {
            string exportPath = string.Empty;
            if (settings.LocationSetting == "Use Default Path")
            {
                exportPath = settings.MetadataRelPath;
            }
            else
            {
                API.Instance.Dialogs.ShowMessage("Select folder where you want to export metadata.");
                exportPath = API.Instance.Dialogs.SelectFolder();
            }

            ExportData.ExportAllGamesData(Settings, API.Instance, exportPath);
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
            Settings.LocationOptions = Settings.LocationOptions is null
                ? new ObservableCollection<string>
                {
                    "Use Default Path",
                    "Prompt for Location"
                }
                : new ObservableCollection<string>(Settings.LocationOptions);

            // Ensure grouped metadata collections are correct (no duplicates, all items present)
            EnsureMetadataGroups();
        }

        public LocalLibrarySettingsViewModel() 
        {
            // Parameterless for design time support
        }

        private void EnsureMetadataGroups()
        {
            // If any group is empty or has duplicates, reset all
            bool needsReset =
                HasDuplicatesOrMissing(Settings.TextElements, new[] { "Name", "Sorting Name", "Series", "Description", "Region", "Platforms", "Categories", "Features", "Genres", "Links", "Tags", "Version", "Developers", "Publishers", "Source", "AgeRatings", "Notes" }) ||
                HasDuplicatesOrMissing(Settings.StatusElements, new[] { "CompletionStatus", "Hidden", "Favorite", "Enable HDR Support" }) ||
                HasDuplicatesOrMissing(Settings.NumericElements, new[] { "UserScore", "CriticScore", "CommunityScore", "PlayCount", "TimePlayed" }) ||
                HasDuplicatesOrMissing(Settings.DateElements, new[] { "ReleaseDate", "Added", "Modified", "LastActivity" }) ||
                HasDuplicatesOrMissing(Settings.MediaElements, new[] { "Icon", "CoverImage", "BackgroundImage", "Logo", "Manual" }) ||
                HasDuplicatesOrMissing(Settings.ScriptElements, new[] { "GameStartedScript", "PostScript", "PreScript" });
            if (needsReset)
            {
                Settings.ResetMetadataGroups();
            }
        }

        private bool HasDuplicatesOrMissing(ObservableCollection<MetadataItem> collection, string[] expectedNames)
        {
            var names = collection.Select(x => x.Name).ToList();
            return names.Count != expectedNames.Length || names.Distinct().Count() != expectedNames.Length || expectedNames.Except(names).Any();
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            EditingClone = Serialization.GetClone(Settings);

            // Only migrate if we *still* have old lists populated
            if ((Settings.RegexList?.Any() ?? false) || (Settings.StringList?.Any() ?? false))
            {
                var rules = Migration.ConvertOldLists(Settings.RegexList, Settings.StringList);

                // Merge with existing ReplaceRules if needed
                if (Settings.ReplaceRules == null || Settings.ReplaceRules.Count == 0)
                {
                    Settings.ReplaceRules = rules;
                }
                else
                {
                    foreach (var rule in rules)
                    {
                        Settings.ReplaceRules.Add(rule);
                    }
                }

                // Clear the old lists so they don't get written back to config.json
                Settings.RegexList = new ObservableCollection<string>();
                Settings.StringList = new ObservableCollection<string>();
            }

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
    }
}