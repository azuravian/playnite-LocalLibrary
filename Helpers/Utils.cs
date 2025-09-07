using Ookii.Dialogs.Wpf;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalLibrary.Helpers
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static string SanitizeDirectoryName(string name, string replacement = "")
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);

            foreach (char c in name)
            {
                if (invalidChars.Contains(c))
                {
                    if (!string.IsNullOrEmpty(replacement))
                    {
                        sb.Append(replacement);
                    }
                    // else skip invalid character
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static string CleanString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Step 1: Replace '+' and '&' with "and"
            string cleaned = input.Replace("+", "and").Replace("&", "and");

            // Step 2: Remove all characters not A-Za-z0-9 or space
            cleaned = Regex.Replace(cleaned, @"[^A-Za-z0-9\s]", "");

            // Step 3: Replace multiple spaces with a single space
            cleaned = Regex.Replace(cleaned, @"\s+", " ");

            // Step 4: Trim leading and trailing spaces
            cleaned = cleaned.Trim();

            return cleaned;
        }
    }

    public class CustomDialogs
    {
        public static string SelectFolderWithDefault(string defaultPath, IDialogsFactory dialogs)
        {
            if (string.IsNullOrEmpty(defaultPath))
            {
                return dialogs.SelectFolder();
            }
            else if (!defaultPath.EndsWith("\\") && !defaultPath.EndsWith("/"))
            { 
                // Ensure it ends with a slash
                defaultPath += "\\";
            }
              
            var window = dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowCloseButton = true
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

        public static string SelectFileWithDefault(string defaultPath, string filter, IDialogsFactory dialogs, Game game)
        {
            if (string.IsNullOrEmpty(defaultPath))
            {
                defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            var gameName = StringExtensions.SanitizeDirectoryName(game.Name);
            var gameDir = Path.Combine(defaultPath, gameName);
            if (Directory.Exists(gameDir))
            {
                defaultPath = gameDir;
            }

            var window = dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowCloseButton = true
            });
            var picker = new VistaOpenFileDialog
            {
                Title = "Select a file",
                Filter = filter,
                InitialDirectory = defaultPath,
                FileName = Path.Combine(defaultPath, "*.*"),
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };
            if (picker.ShowDialog(window) == true)
            {
                return picker.FileName;
            }

            return string.Empty;
        }
    }
}
