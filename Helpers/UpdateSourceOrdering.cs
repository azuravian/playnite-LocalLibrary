using System;
using System.Collections.Generic;
using System.IO;

namespace LocalLibrary.Helpers
{
    public static class UpdateSourceOrdering
    {
        private sealed class Item<T>
        {
            public T Value;
            public bool Main;
            public int Index;
            public UpdatePackage Key;
        }

        // Preserve original objects and their settings; compute remote-file keys once.
        public static List<T> Sort<T>(IEnumerable<T> entries, Func<T, bool> isMain, Func<T, string> path)
        {
            if (entries == null) throw new ArgumentNullException("entries");
            if (isMain == null) throw new ArgumentNullException("isMain");
            if (path == null) throw new ArgumentNullException("path");
            var items = new List<Item<T>>();
            foreach (T value in entries)
                items.Add(new Item<T> { Value = value, Main = isMain(value), Index = items.Count, Key = CreateKey(path(value)) });
            var comparer = new UpdatePackageComparer();
            items.Sort(delegate(Item<T> a, Item<T> b)
            {
                if (a.Main != b.Main) return a.Main ? -1 : 1;
                // Do not silently choose another primary installer from a name sort.
                int order = a.Main ? 0 : comparer.Compare(a.Key, b.Key);
                return order != 0 ? order : a.Index.CompareTo(b.Index);
            });
            var result = new List<T>();
            foreach (var item in items) result.Add(item.Value);
            return result;
        }

        public static UpdatePackage CreateKey(string path)
        {
            var key = new UpdatePackage { Path = path };
            if (string.IsNullOrWhiteSpace(path)) return key;
            try
            {
                string label = Path.GetFileName(path);
                string extension = Path.GetExtension(label);
                if (IsInstallerExtension(extension)) label = Path.GetFileNameWithoutExtension(label);
                ReleaseUpdateMetadata metadata;
                if (!ReleaseUpdateMetadataParser.TryParse(label, out metadata))
                    ReleaseUpdateMetadataParser.TryParse(Path.GetFileName(Path.GetDirectoryName(path)), out metadata);
                if (metadata != null) key.TargetVersion = metadata.TargetVersion;
                // Local source timestamp is a fallback, not an xREL publication date.
                if (File.Exists(path)) key.PublishedAtUtc = File.GetLastWriteTimeUtc(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
            catch (System.Security.SecurityException) { }
            return key;
        }

        public static bool IsMainName(string name)
        {
            return string.Equals(name, "Install", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "Installer", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeSourcePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            // Playnite variables need expansion by the host, not guessed here.
            if (path.IndexOf('{') >= 0) return path.Replace('/', '\\');
            try { return Path.GetFullPath(path).TrimEnd('\\', '/'); }
            catch (ArgumentException) { return path; }
            catch (NotSupportedException) { return path; }
            catch (System.Security.SecurityException) { return path; }
            catch (IOException) { return path; }
        }

        public static bool IsInstallerExtension(string extension)
        {
            return string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".msi", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".ps1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".ps", StringComparison.OrdinalIgnoreCase);
        }
    }
}
