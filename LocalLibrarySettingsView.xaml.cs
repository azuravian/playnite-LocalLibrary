using Playnite.SDK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LocalLibrary
{
    public partial class LocalLibrarySettingsView : UserControl
    {
        public LocalLibrarySettingsView()
        {
            InitializeComponent();
        }

        public void Button_ApplyPluginId_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<GameSourceOption> sources = SelectedSourcesList.Items.Cast<GameSourceOption>().ToObservable();
            LocalLibrary.PluginIdUpdate(sources);
        }

        public void Button_AddGames_Click(object sender, RoutedEventArgs e)
        {
            Finder addGames = new Finder();
            List<string> installPaths = PathsBox.Items.Cast<string>().ToList();

            if (installPaths == null || !installPaths.Any())
            {
                MessageBox.Show("No install paths found.");
                return;
            }

            if (cbActions == null || LevenSlider == null)
            {
                MessageBox.Show("Please ensure all options are selected.");
                return;
            }

            bool useActions = cbActions.IsChecked ?? false;
            if (!int.TryParse(LevenSlider.Value.ToString(), out int levenValue))
            {
                MessageBox.Show("Invalid Levenshtein value.");
                return;
            }

            if (SelectedSourcesList.Items.Count == 0)
            {
                MessageBox.Show("Please make sure you add a Source to the Selected Sources list.");
                return;
            }
            ObservableCollection<GameSourceOption> sources = SelectedSourcesList.Items.Cast<GameSourceOption>().ToObservable();

            if (PlatformList.SelectedItem == null)
            {
                MessageBox.Show("Please select a platform.");
                return;
            }
            string platform = PlatformList.SelectedItem.ToString();

            var ignorelist = RegexList.Items.Cast<string>().Select(item => new MergedItem { Value = item, Source = "Regex" })
                .Concat(StringList.Items.Cast<string>().Select(item => new MergedItem { Value = item, Source = "String" }))
                .ToList();
            addGames.FindInstallers(installPaths, useActions, levenValue, sources, platform, ignorelist, cbFindUpdates.IsChecked ?? false);
        }

        public void Button_ArchiveBrowse_Click(object sender, RoutedEventArgs e)
        {
            string archivepath = API.Instance.Dialogs.SelectFile("Unarchive Executable|*.exe");
            if (archivepath != null)
            {
                ArchivePath.Text = archivepath;
                if (archivepath.ToLower().Contains("winrar"))
                {
                    RBRar.IsChecked = true;
                }
                else if (archivepath.ToLower().Contains("7z"))
                {
                    RB7z.IsChecked = true;
                }
            }
        }

        public void Button_AddRegex_Click(object sender, RoutedEventArgs e)
        {
            string regex = RegexBox.Text;
            if (string.IsNullOrEmpty(regex))
            {
                MessageBox.Show("Please enter a regex.");
                return;
            }
            RegexList.Items.Add(regex);
            RegexBox.Text = string.Empty;
        }

        public void Button_RemoveRegex_Click(object sender, RoutedEventArgs e)
        {
            if (RegexList.SelectedItem == null)
            {
                MessageBox.Show("Please select a regex to remove.");
                return;
            }
            RegexList.Items.Remove(RegexList.SelectedItem);
        }

        public void Button_AddString_Click(object sender, RoutedEventArgs e)
        {
            string str = StringBox.Text;
            if (string.IsNullOrEmpty(str))
            {
                MessageBox.Show("Please enter a string.");
                return;
            }
            StringList.Items.Add(str);
            StringBox.Text = string.Empty;
        }

        public void Button_RemoveString_Click(object sender, RoutedEventArgs e)
        {
            if (StringList.SelectedItem == null)
            {
                MessageBox.Show("Please select a string to remove.");
                return;
            }
            StringList.Items.Remove(StringList.SelectedItem);
        }
    }
}