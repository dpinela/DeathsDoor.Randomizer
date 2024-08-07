using CG = System.Collections.Generic;
using static System.Linq.Enumerable;
using Text = System.Text;
using IO = System.IO;
using Crypto = System.Security.Cryptography;
using RC = RandomizerCore;
using HL = HarmonyLib;

namespace DDoor.Randomizer;

[HL.HarmonyPatch]
internal static class PlacementsHash
{
    public static void Save(CG.List<CG.List<RC.RandoPlacement>[]> placements)
    {
        var h = FullHash(placements);
        var trunc = (uint)h[0] | ((uint)h[1] << 8) | ((uint)h[2] << 16) | ((uint)h[3] << 24);
        GameSave.currentSave.SetCountKey(hashSaveKey, (int)trunc);
    }

    private const string hashSaveKey = "Randomizer-hash";

    private static byte[] FullHash(CG.List<CG.List<RC.RandoPlacement>[]> placements) {
        var utf8 = new Text.UTF8Encoding();
        using var sha = Crypto.SHA256.Create();
        using var hstream = new Crypto.CryptoStream(IO.Stream.Null, sha, Crypto.CryptoStreamMode.Write);
        var sortedPlacements = placements
            .SelectMany(x => x)
            .SelectMany(x => x)
            .OrderBy(p => p.Location.Name)
            .ThenBy(p => p.Item.Name);
        var fieldSep = new byte[] {(byte)'\t'};
        var recordSep = new byte[] {(byte)'\f'};
        foreach (var p in sortedPlacements)
        {
            HashBytes(hstream, utf8.GetBytes(p.Item.Name));
            HashBytes(hstream, fieldSep);
            HashBytes(hstream, utf8.GetBytes(p.Location.Name));
            HashBytes(hstream, recordSep);
        }
        sha.TransformFinalBlock(new byte[] {}, 0, 0);
        return sha.Hash;
    }
    
    private static void HashBytes(Crypto.CryptoStream s, byte[] b)
    {
        // s.Write(b) isn't actually supported at runtime
        s.Write(b, 0, b.Length);
    }

    [HL.HarmonyPatch(typeof(DialogueManager), nameof(DialogueManager.GetLines))]
    [HL.HarmonyPrefix]
    private static bool ShowHashInTelephone(DialogueManager __instance, string id, ref string[] __result)
    {
        if (id != "phonebooth_1")
        {
            return true;
        }
        
        var hasHash = GameSave.GetSaveData().countKeys.TryGetValue(hashSaveKey, out var ihash);
        if (!hasHash)
        {
            return true;
        }
        var hash = (uint)ihash;
        var p1 = hash / 1_000_000_000;
        var p2 = (hash / 1_000_000) % 1_000;
        var p3 = (hash / 1_000) % 1_000;
        var p4 = hash % 1_000;
        __result = new string[]
        {
            string.Format("<i>This Rando Terminal has the number ({0}) {1:D3} {2:D3} {3:D3}.</i>", p1, p2, p3, p4)
        };
        return false;
    }
}