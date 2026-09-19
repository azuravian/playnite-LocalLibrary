using Ookii.Dialogs.Wpf;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
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

    // New helper class for path matching / relative path computation
    public static class PathUtils
    {
        /// <summary>
        /// Attempts to compute a relative path by matching <paramref name="fullPath"/> against the provided root paths.
        /// Rules applied:
        /// 1) Find the first root in <paramref name="roots"/> that is a prefix of <paramref name="fullPath"/> (case-insensitive).
        /// 2) Verify there are at least <paramref name="minDepthAfterRoot"/> path segments after the matched root.
        /// 3) Remove the first (top-level) segment of those remaining segments and return the rest joined with the platform directory separator.
        /// </summary>
        /// <param name="fullPath">Full absolute path to evaluate.</param>
        /// <param name="roots">Collection of candidate root paths (assumed to be distinct - no root contains another).</param>
        /// <param name="relativePath">Output relative path (segments after removing the top-level folder).</param>
        /// <param name="minDepthAfterRoot">Minimum number of path segments required after the matched root (default 2).</param>
        /// <returns>True if a match is found and relativePath set; otherwise false.</returns>
        public static bool TryGetRelativePathFromRoots(string fullPath, IEnumerable<string> roots, out string relativePath, int minDepthAfterRoot = 2)
        {
            relativePath = null;

            if (string.IsNullOrWhiteSpace(fullPath) || roots == null)
            {
                return false;
            }

            // Normalize full path
            string normalizedFull;
            try
            {
                normalizedFull = Path.GetFullPath(fullPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return false;
            }

            // Add trailing separator to full path for prefix comparison
            string fullWithSep = normalizedFull + Path.DirectorySeparatorChar;

            // Find the first matching root
            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root))
                    continue;

                string normalizedRoot;
                try
                {
                    normalizedRoot = Path.GetFullPath(root)
                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        + Path.DirectorySeparatorChar;
                }
                catch
                {
                    continue;
                }

                if (fullWithSep.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    // Found a match - compute the relative path
                    string remainder = fullWithSep.Substring(normalizedRoot.Length)
                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (string.IsNullOrEmpty(remainder))
                    {
                        return false;
                    }

                    // Split into segments
                    var segments = remainder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                    // Verify depth
                    if (segments.Length < minDepthAfterRoot)
                    {
                        return false;
                    }

                    // Remove the top-level directory from the remainder, then rejoin the rest as the relative path
                    var resultSegments = segments.Skip(1);
                    relativePath = string.Join(Path.DirectorySeparatorChar.ToString(), resultSegments);

                    return true;
                }
            }

            return false;
        }
    }
}
