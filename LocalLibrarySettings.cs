using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace LocalLibrary
{
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

        public LocalLibrarySettingsViewModel(LocalLibrary plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<LocalLibrarySettings>();

            // LoadPluginSettings returns null if not saved data is available.
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
    }
}