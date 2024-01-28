
using Playnite.SDK.Models;
using Playnite.SDK;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System;

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
    }
}