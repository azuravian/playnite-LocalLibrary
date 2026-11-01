using System;
using System.Collections.Generic;

namespace LocalLibrary.Helpers
{
    public sealed class UpdatePackage
    {
        public string Path { get; set; }
        public string TargetVersion { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
    }

    public sealed class UpdatePackageComparer : IComparer<UpdatePackage>
    {
        public int Compare(UpdatePackage a, UpdatePackage b)
        {
            if (ReferenceEquals(a, b))
            {
                return 0;
            }
            if (a == null)
            {
                return 1;
            }
            if (b == null)
            {
                return -1;
            }

            var va = ParseVersion(a.TargetVersion);
            var vb = ParseVersion(b.TargetVersion);
            bool ha = va != null;
            bool hb = vb != null;

            if (ha && !hb) return -1;
            if (!ha && hb) return 1;

            int cmp;
            if (ha)
            {
                cmp = CompareVersions(va, vb);
                if (cmp != 0) return cmp;
            }

            cmp = CompareDate(a.PublishedAtUtc, b.PublishedAtUtc);
            if (cmp != 0) return cmp;

            string pa = a.Path ?? string.Empty;
            string pb = b.Path ?? string.Empty;
            cmp = StringComparer.OrdinalIgnoreCase.Compare(pa, pb);
            if (cmp != 0) return cmp;
            return StringComparer.Ordinal.Compare(pa, pb);
        }

        private static List<string> ParseVersion(string s)
        {
            if (s == null) return null;
            string t = s.Trim();
            if (t.Length == 0) return null;
            if (t[0] == 'v' || t[0] == 'V')
            {
                t = t.Substring(1);
                if (t.Length == 0) return null;
            }
            string[] parts = t.Split('.');
            var list = new List<string>(parts.Length);
            foreach (string part in parts)
            {
                if (part.Length == 0) return null;
                foreach (char c in part)
                {
                    if (c < '0' || c > '9') return null;
                }
                int i = 0;
                while (i < part.Length && part[i] == '0') i++;
                string norm = part.Substring(i);
                if (norm.Length == 0) norm = "0";
                list.Add(norm);
            }
            return list;
        }

        private static int CompareVersions(List<string> a, List<string> b)
        {
            int max = a.Count > b.Count ? a.Count : b.Count;
            for (int i = 0; i < max; i++)
            {
                string av = i < a.Count ? a[i] : "0";
                string bv = i < b.Count ? b[i] : "0";
                if (av.Length != bv.Length)
                {
                    return av.Length < bv.Length ? -1 : 1;
                }
                if (av != bv)
                {
                    return StringComparer.Ordinal.Compare(av, bv);
                }
            }
            return 0;
        }

        private static int CompareDate(DateTime? a, DateTime? b)
        {
            bool ha = a.HasValue;
            bool hb = b.HasValue;
            if (ha && !hb) return -1;
            if (!ha && hb) return 1;
            if (!ha) return 0;
            if (a.Value < b.Value) return -1;
            if (a.Value > b.Value) return 1;
            return 0;
        }
    }
}