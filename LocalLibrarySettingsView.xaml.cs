using Playnite.SDK;
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
            var source = PluginsList.SelectedItem.ToString();
            LocalLibrary.PluginIdUpdate(source);
        }

        public void Button_ArchiveBrowse_Click(object sender, RoutedEventArgs e)
        {
            var archivepath = API.Instance.Dialogs.SelectFile("Unarchive Executable|*.exe");
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
    }
}