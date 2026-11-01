namespace LocalLibrary.Helpers
{
    using System;

    public sealed class ReleaseUpdateMetadata
    {
        public string RequiredVersion { get; set; }
        public string TargetVersion { get; set; }
    }

    public static class ReleaseUpdateMetadataParser
    {
        public static bool TryParse(string label, out ReleaseUpdateMetadata metadata)
        {
            metadata = null;
            if (string.IsNullOrEmpty(label) || label.Length > 1024)
            {
                return false;
            }

            int markerIndex = -1;
            int markerCount = 0;
            string[] markers = { "update", "patch", "hotfix" };

            for (int i = 0; i < label.Length; i++)
            {
                foreach (string marker in markers)
                {
                    if (i + marker.Length <= label.Length &&
                        string.Compare(label, i, marker, 0, marker.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        bool beforeOk = i == 0 || IsSeparator(label[i - 1]);
                        bool afterOk = i + marker.Length == label.Length || IsSeparator(label[i + marker.Length]);
                        if (beforeOk && afterOk)
                        {
                            markerCount++;
                            if (markerIndex == -1)
                            {
                                markerIndex = i;
                            }
                        }
                    }
                }
            }

            if (markerCount != 1 || markerIndex == -1)
            {
                return false;
            }

            int pos = markerIndex;
            while (pos < label.Length && !IsSeparator(label[pos]))
            {
                pos++;
            }

            bool dotSeparator = false;
            while (pos < label.Length && IsSeparator(label[pos]))
            {
                if (label[pos] == '.')
                {
                    if (dotSeparator) return false;
                    dotSeparator = true;
                }
                pos++;
            }

            if (pos >= label.Length)
            {
                return false;
            }

            string version1 = ParseVersionToken(label, ref pos);
            if (version1 == null)
            {
                return false;
            }

            int savedPos = pos;
            while (pos < label.Length && char.IsWhiteSpace(label[pos]))
            {
                pos++;
            }

            string version2 = null;
            bool isRange = false;

            if (pos < label.Length && label[pos] == '-')
            {
                int afterHyphen = pos + 1;
                while (afterHyphen < label.Length && char.IsWhiteSpace(label[afterHyphen]))
                {
                    afterHyphen++;
                }

                if (afterHyphen < label.Length && (IsAsciiDigit(label[afterHyphen]) ||
                    (char.ToUpperInvariant(label[afterHyphen]) == 'V' && afterHyphen + 1 < label.Length && IsAsciiDigit(label[afterHyphen + 1]))))
                {
                    pos = afterHyphen;
                    version2 = ParseVersionToken(label, ref pos);
                    if (version2 == null)
                    {
                        return false;
                    }
                    isRange = true;
                }
            }

            if (!isRange)
            {
                pos = savedPos;
            }

            if (isRange)
            {
                if (CompareVersions(version1, version2) > 0)
                {
                    return false;
                }
                metadata = new ReleaseUpdateMetadata
                {
                    RequiredVersion = version1,
                    TargetVersion = version2
                };
            }
            else
            {
                metadata = new ReleaseUpdateMetadata
                {
                    RequiredVersion = null,
                    TargetVersion = version1
                };
            }

            return true;
        }

        private static bool IsSeparator(char c)
        {
            return c == '.' || c == '_' || c == '-' || char.IsWhiteSpace(c);
        }

        private static string ParseVersionToken(string label, ref int pos)
        {
            int start = pos;
            while (pos < label.Length && !char.IsWhiteSpace(label[pos]) && label[pos] != '_' && label[pos] != '-')
            {
                pos++;
            }

            if (pos == start)
            {
                return null;
            }

            string token = label.Substring(start, pos - start);
            return NormalizeVersion(token);
        }

        private static string NormalizeVersion(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            int start = 0;
            if (token.Length > 0 && (token[0] == 'v' || token[0] == 'V'))
            {
                start = 1;
            }

            string version = token.Substring(start);
            if (string.IsNullOrEmpty(version))
            {
                return null;
            }

            int dotCount = 0;
            for (int i = 0; i < version.Length; i++)
            {
                char c = version[i];
                if (c == '.')
                {
                    dotCount++;
                    if (i == 0 || i == version.Length - 1 || version[i - 1] == '.' || !IsAsciiDigit(version[i + 1]))
                    {
                        return null;
                    }
                }
                else if (!IsAsciiDigit(c))
                {
                    return null;
                }
            }

            if (dotCount > 0 && version.EndsWith(".", StringComparison.Ordinal))
            {
                return null;
            }

            return version;
        }

        private static bool IsAsciiDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static int CompareVersions(string a, string b)
        {
            return new UpdatePackageComparer().Compare(
                new UpdatePackage { TargetVersion = a, Path = string.Empty },
                new UpdatePackage { TargetVersion = b, Path = string.Empty });
        }
    }
}
