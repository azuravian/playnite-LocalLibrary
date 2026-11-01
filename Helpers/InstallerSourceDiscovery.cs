using System;
using System.Collections.Generic;
using System.IO;

namespace LocalLibrary.Helpers
{
    public static class InstallerSourceDiscovery
    {
        // Never guess between multiple setups or follow junctions out of a package.
        public static string FindUniqueInstaller(string folder, out bool incompleteOrAmbiguous)
        {
            incompleteOrAmbiguous = false;
            var preferred = new List<string>();
            var others = new List<string>();
            var queue = new Queue<KeyValuePair<string, int>>();
            queue.Enqueue(new KeyValuePair<string, int>(folder, 0));
            int visited = 0;
            try
            {
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if ((File.GetAttributes(current.Key) & FileAttributes.ReparsePoint) != 0)
                    {
                        incompleteOrAmbiguous = true;
                        continue;
                    }
                    foreach (string entry in Directory.EnumerateFileSystemEntries(current.Key))
                    {
                        if (++visited > 2048) { incompleteOrAmbiguous = true; return null; }
                        var attributes = File.GetAttributes(entry);
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            incompleteOrAmbiguous = true;
                            continue;
                        }
                        if ((attributes & FileAttributes.Directory) != 0)
                        {
                            string name = Path.GetFileName(entry);
                            if (string.Equals(name, "redist", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(name, "_CommonRedist", StringComparison.OrdinalIgnoreCase)) continue;
                            if (current.Value >= 3) { incompleteOrAmbiguous = true; continue; }
                            queue.Enqueue(new KeyValuePair<string, int>(entry, current.Value + 1));
                            continue;
                        }
                        if (!UpdateSourceOrdering.IsInstallerExtension(Path.GetExtension(entry))) continue;
                        string stem = Path.GetFileNameWithoutExtension(entry);
                        if (stem.StartsWith("unins", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(stem, "setup", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(stem, "patch", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(stem, "install", StringComparison.OrdinalIgnoreCase)) preferred.Add(entry);
                        else others.Add(entry);
                    }
                }
            }
            catch (IOException) { incompleteOrAmbiguous = true; }
            catch (UnauthorizedAccessException) { incompleteOrAmbiguous = true; }
            catch (System.Security.SecurityException) { incompleteOrAmbiguous = true; }
            if (incompleteOrAmbiguous) return null;
            var candidates = preferred.Count > 0 ? preferred : others;
            incompleteOrAmbiguous = candidates.Count > 1;
            return candidates.Count == 1 ? candidates[0] : null;
        }
    }
}
