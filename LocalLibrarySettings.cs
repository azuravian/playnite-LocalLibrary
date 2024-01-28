using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime;
using System.Windows;
using System.Windows.Controls;

namespace LocalLibrary
{
    public class LocalLibrarySettings : ObservableObject
    {
        private bool useactions = false;
        public bool UseActions { get => useactions; set => SetValue(ref useactions, value); }
        
        private List<GameSource>pluginsources = null;
        public List<GameSource>PluginSources { get => pluginsources; set => SetValue(ref pluginsources, value); }

        private List<GameSource> includedsources = null;
        public List<GameSource> IncludedSources { get => pluginsources; set => SetValue(ref includedsources, value); }

        private ComboBox pluginslist = null;
        public ComboBox PluginsList { get => pluginslist; set => SetValue(ref pluginslist, value); }

        private string selectedsource = null;
        public string SelectedSource { get => selectedsource; set => SetValue(ref selectedsource, value); }

        private bool autoupdate = false;
        public bool AutoUpdate { get => autoupdate; set => SetValue(ref autoupdate, value); }
        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
    }

    public class LocalLibrarySettingsViewModel : ObservableObject, ISettings
    {
        private readonly LocalLibrary plugin;
        private LocalLibrarySettings editingClone { get; set; }

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

        public LocalLibrarySettingsViewModel(LocalLibrary plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<LocalLibrarySettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new LocalLibrarySettings();
            }
        }
        
        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);

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
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
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