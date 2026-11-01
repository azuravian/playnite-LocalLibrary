using System;
using LocalLibrary.Helpers;

public static class ReleaseUpdateMetadataTests
{
    static int checks;
    static void Valid(string label, string from, string to)
    {
        ReleaseUpdateMetadata result;
        checks++;
        if (!ReleaseUpdateMetadataParser.TryParse(label, out result) || result == null ||
            result.RequiredVersion != from || result.TargetVersion != to)
            throw new Exception("Unexpected update metadata: " + label);
    }
    static void Invalid(string label)
    {
        ReleaseUpdateMetadata result;
        checks++;
        if (ReleaseUpdateMetadataParser.TryParse(label, out result) || result != null)
            throw new Exception("Accepted malformed or ambiguous metadata: " + label);
    }
    public static int Main()
    {
        try
        {
            Valid("007.First.Light.update.1.3.0-1.3.2-ElAmigos", "1.3.0", "1.3.2");
            Valid("007.First.Light.update.1.2.2-1.3.0-ElAmigos", "1.2.2", "1.3.0");
            Valid("Patch v01.09 - v1.10", "01.09", "1.10");
            Valid("Game.Hotfix.7631656-ElAmigos", null, "7631656");
            Valid("update_v1.10", null, "1.10");
            Valid("GAME_UPDATE_V1.10", null, "1.10");
            Valid("Game-update-1.2", null, "1.2");
            Valid("Game patch 0-1.0", "0", "1.0");
            Valid("Game.UPDATE.1.9-1.10-Group", "1.9", "1.10");
            Valid("Game.UPDATE.1.0-1.0.0-Group", "1.0", "1.0.0");
            Valid("Game.UPDATE.1.2-release", null, "1.2");
            Valid("Game.UPDATE.1.2 description", null, "1.2");
            Valid("update.1.2.3.4.5", null, "1.2.3.4.5");
            Valid("update." + new string('9', 100), null, new string('9', 100));
            Valid("update.184467440737095516159-184467440737095516160", "184467440737095516159", "184467440737095516160");
            foreach (var label in new[] {
                null, "", " ", "007.First.Light.v1.3.0.MULTi14-ElAmigos", "Game.2026.MULTi14",
                "Game.update", "Game.update.", "Game.update.v", "Game.update.vv1.2",
                "Game.update.1..2", "Game.update.1.", "Game.update..1", "Game.update.1.2beta",
                "Game.update.1.2.3suffix", "Game.update.\u0661.2", "Game.update.1.-2",
                "Game.update.1.2.3. description", "Game.update.1.10-1.9-Group",
                "Game.update.2-1", "Game.update.1.2-1..3-Group", "Game.update.1.2-v1..3-Group",
                "Game.update.1.2-1.3beta", "Game.update.1.2-1.3.",
                "Game.update.1.2.patch.1.3", "Game.update.1.2 update.invalid",
                "Game.preupdate.1.2", "Game.updates.1.2", "Game.update1.2", new string('x', 1025)
            }) Invalid(label);
            for (int i = 0; i < 40; i++)
            {
                Valid("Game.update.1." + i + "-1." + (i + 1) + "-Group", "1." + i, "1." + (i + 1));
                Invalid("Game.update.1." + (i + 1) + "-1." + i + "-Group");
            }
            Console.WriteLine("PASS " + checks + " independent update metadata checks");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL after " + checks + " checks: " + e.Message);
            return 1;
        }
    }
}
