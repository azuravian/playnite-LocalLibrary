using Playnite.SDK;
using LocalLibrary.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System;

namespace LocalLibrary
{
    public partial class LocalLibrarySettingsView : UserControl
    {
        public LocalLibrarySettingsView(LocalLibrary plugin)
        {
            InitializeComponent();

            DataContext = new LocalLibrarySettingsViewModel(plugin);
        }
    }
}