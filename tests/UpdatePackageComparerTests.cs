// Independent acceptance authored before applying the model proposal.
// Compile with Helpers/UpdatePackageComparer.cs; no Playnite installation needed.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LocalLibrary.Helpers;

public static class UpdatePackageComparerTests
{
    static int checks;
    static readonly UpdatePackageComparer Comparer = new UpdatePackageComparer();
    static readonly DateTime Early = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    static readonly DateTime Late = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    static UpdatePackage Package(string version, string path, DateTime? date)
    {
        return new UpdatePackage { TargetVersion = version, Path = path, PublishedAtUtc = date };
    }
    static void Check(bool value, string label)
    {
        checks++;
        if (!value) throw new Exception(label);
    }
    static void Versions(string a, string b, int expected)
    {
        var x = Package(a, "same", Early);
        var y = Package(b, "same", Early);
        Check(Math.Sign(Comparer.Compare(x, y)) == expected, "versions: " + a + " / " + b);
        Check(Math.Sign(Comparer.Compare(y, x)) == -expected, "reverse versions: " + a + " / " + b);
    }
    public static int Main()
    {
        try
        {
            Versions("1.9", "1.10", -1);
            Versions("1.2", "1.2.1", -1);
            Versions("1.2", "v01.002.0", 0);
            Versions("0", "000.0.00", 0);
            Versions(" V1.02 ", "1.2.0", 0);
            Versions("184467440737095516160", "184467440737095516159", 1);
            Versions(new string('9', 100), "1" + new string('0', 100), -1);
            Versions("1.0.0.0.1", "1", 1);
            Versions("1.3.2", "1.3.0", 1);
            Versions("7631656", "6931813", 1);
            foreach (var bad in new[] { null, "", " ", "v", "vv1", "1..2", ".1", "1.", "+1", "-1", "1 2", "1.0-beta", "1.0\n2", "\u0661.2", "v1.2-1.3" })
            {
                Check(Comparer.Compare(Package("0", "z", null), Package(bad, "a", Early)) < 0, "known before malformed");
            }
            Check(Comparer.Compare(Package("1.9", "z", Late), Package("1.10", "a", Early)) < 0, "version outranks date");
            Check(Comparer.Compare(Package("1.0", "z", Early), Package("1", "a", Late)) < 0, "equal version dates");
            Check(Comparer.Compare(Package("1", "z", Early), Package("1", "a", null)) < 0, "missing date last");
            Check(Comparer.Compare(Package(null, "z", Early), Package("invalid", "a", Late)) < 0, "unknown version dates");
            Check(Comparer.Compare(Package("1", "a", null), Package("1", "B", null)) < 0, "ordinal-ignore-case path");
            Check(Comparer.Compare(Package("1", "A", null), Package("1", "a", null)) < 0, "ordinal case tie");
            Check(Comparer.Compare(Package("1", null, null), Package("1", "", null)) == 0, "null path empty");
            Check(Comparer.Compare(null, null) == 0, "null pair");
            Check(Comparer.Compare(null, Package("1", "x", null)) > 0, "null package last");
            var same = Package("1", "x", null);
            Check(Comparer.Compare(same, same) == 0, "reference equality");
            var items = new[] {
                Package("1.10", "p10", Early), Package(null, "unknown-new", Late),
                Package("1.9", "p9", Late), Package("1.10.0", "p10-new", Late),
                Package("garbage", "unknown-old", Early), Package("0", "zero", null),
                Package("1.10", "p10-no-date", null), Package("v01.09", "p9", Late),
                Package("1.0.1", "patch", Early), null
            };
            foreach (var x in items)
            foreach (var y in items)
            {
                var xy = Math.Sign(Comparer.Compare(x, y));
                Check(xy == -Math.Sign(Comparer.Compare(y, x)), "antisymmetry");
                foreach (var z in items)
                    if (xy <= 0 && Comparer.Compare(y, z) <= 0)
                        Check(Comparer.Compare(x, z) <= 0, "transitivity across missing versions/dates");
            }
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                Check(Comparer.Compare(Package("1", "I", null), Package("1", "i", null)) < 0, "culture independent");
            }
            finally { Thread.CurrentThread.CurrentCulture = oldCulture; }
            Array.Sort(items, Comparer);
            string[] expected = { "zero", "patch", "p9", "p9", "p10", "p10-new", "p10-no-date", "unknown-old", "unknown-new", null };
            for (int i = 0; i < expected.Length; i++)
                Check((items[i] == null ? null : items[i].Path) == expected[i], "complete order " + i);
            Console.WriteLine("PASS " + checks + " independent checks");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL after " + checks + " checks: " + e.Message);
            return 1;
        }
    }
}
