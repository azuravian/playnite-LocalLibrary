using Ookii.Dialogs.Wpf;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalLibrary.Helpers
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }

    public class CustomDialogs
    {
        public static string SelectFolderWithDefault(string defaultPath, IDialogsFactory dialogs)
        {
            var window = dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowCloseButton = true,
            });

            var picker = new VistaFolderBrowserDialog
            {
                Description = "Choose a folder",
                UseDescriptionForTitle = true,
                SelectedPath = defaultPath
            };

            if (picker.ShowDialog(window) == true)
            {
                return picker.SelectedPath;
            }

            return string.Empty;
        }

    }
}
