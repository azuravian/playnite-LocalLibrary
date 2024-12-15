using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Controls;

namespace LocalLibrary
{
    public class LocalLibrarySettings : ObservableObject
    {
        private bool _useactions = false;
        public bool UseActions { get => _useactions; set => SetValue(ref _useactions, value); }

        private bool _removeplay = true;
        public bool RemovePlay { get => _removeplay; set => SetValue(ref _removeplay, value); }

        private List<GameSource> _pluginsources = null;
        public List<GameSource> PluginSources { get => _pluginsources; set => SetValue(ref _pluginsources, value); }

        private List<GameSource> _includedsources = null;
        public List<GameSource> IncludedSources { get => _pluginsources; set => SetValue(ref _includedsources, value); }

        private ComboBox _sourcelist = null;
        public ComboBox SourceList { get => _sourcelist; set => SetValue(ref _sourcelist, value); }

        private string _selectedsource = null;
        public string SelectedSource { get => _selectedsource; set => SetValue(ref _selectedsource, value); }

        private List<Platform> _platforms = null;
        public List<Platform> Platforms { get => _platforms; set => SetValue(ref _platforms, value); }

        private string _selectedplatform = null;
        public string SelectedPlatform { get => _selectedplatform; set => SetValue(ref _selectedplatform, value); }

        private bool _usepaths = false;
        public bool UsePaths { get => _usepaths; set => SetValue(ref _usepaths, value); }

        private ObservableCollection<string> _installpaths = null;
        public ObservableCollection<string> InstallPaths { get => _installpaths; set => SetValue(ref _installpaths, value); }

        private ObservableCollection<string> _regexlist = null;
        public ObservableCollection<string> RegexList { get => _regexlist; set => SetValue(ref _regexlist, value); }

        private ObservableCollection<string> _stringlist = null;
        public ObservableCollection<string> StringList { get => _stringlist; set => SetValue(ref _stringlist, value); }

        private string _selectedregex = null;
        public string SelectedRegex { get => _selectedregex; set => SetValue(ref _selectedregex, value); }

        private string _selectedstring = null;
        public string SelectedString { get => _selectedstring; set => SetValue(ref _selectedstring, value); }

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
                foreach (string item in items.ToList().Cast<string>())
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
                foreach (string item in items.ToList().Cast<string>())
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
                foreach (string item in items.ToList().Cast<string>())
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
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            EditingClone = Serialization.GetClone(Settings);

            Settings.PluginSources = GetSources();

            List<GameSource> GetSources()
            {
                List<GameSource> Sources = new List<GameSource>();
                foreach (var source in plugin.PlayniteApi.Database.Sources)
                {
                    Sources.Add(source);
                }
                Sources.Sort();

                return Sources;
            }

            Settings.Platforms = GetPlatforms();

            List<Platform> GetPlatforms()
            {
                List<Platform> Platforms = new List<Platform>();
                foreach (var platform in plugin.PlayniteApi.Database.Platforms)
                {
                    Platforms.Add(platform);
                }
                Platforms.Sort();
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