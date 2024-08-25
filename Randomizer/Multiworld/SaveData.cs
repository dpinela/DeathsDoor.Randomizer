using CG = System.Collections.Generic;
using IO = System.IO;
using Json = Newtonsoft.Json;
using HL = HarmonyLib;
using UE = UnityEngine;

namespace DDoor.Randomizer.Multiworld;

internal record class SaveData(
    string ServerAddr,
    int PlayerId,
    int RandoId,
    string[] RemoteNicknames,
    CG.List<RemoteItem> RemoteItems)
{
    public static SaveData? Current;
}

[HL.HarmonyPatch]
internal static class SaveDataManager
{
    private static string FileLocation(string saveId) => IO.Path.Combine(
        UE.Application.persistentDataPath,
        "SAVEDATA",
        $"Save_{saveId}-DDRandoMultiworld.json"
    );

    [HL.HarmonyPatch(typeof(SaveSlot), nameof(SaveSlot.LoadSave))]
    [HL.HarmonyPrefix]
    private static void Load(SaveSlot __instance)
    {
        try
        {
            using var file = IO.File.OpenText(FileLocation(__instance.saveId));
            using var reader = new Json.JsonTextReader(file);
            var ser = Json.JsonSerializer.CreateDefault();
            SaveData.Current = ser.Deserialize<SaveData>(reader);
        }
        catch (IO.FileNotFoundException)
        {
            SaveData.Current = null;
        }
        catch (System.Exception err)
        {
            SaveData.Current = null;
            RandomizerPlugin.LogError($"Error loading MW data for save ID {__instance.saveId}: {err}");
        }
    }

    [HL.HarmonyPatch(typeof(GameSave), nameof(GameSave.Save))]
    [HL.HarmonyPostfix]
    private static void Save(GameSave __instance)
    {
        var save = SaveData.Current;
        if (save != null)
        {
            // Serialize the data into a buffer so that we don't destroy an existing
            // file if serialization fails.
            var json = Json.JsonConvert.SerializeObject(save);
            IO.File.WriteAllText(FileLocation(__instance.saveId), json);
        }
    }

    [HL.HarmonyPatch(typeof(SaveSlot), nameof(SaveSlot.EraseSave))]
    [HL.HarmonyPostfix]
    private static void Erase(SaveSlot __instance)
    {
        try
        {
            IO.File.Delete(FileLocation(__instance.saveId));
            RandomizerPlugin.LogInfo($"Saved MW data deleted for save ID {__instance.saveId}");
        }
        catch (IO.IOException)
        {
            RandomizerPlugin.LogInfo($"No saved MW data deleted for save ID {__instance.saveId}");
        }
    }
}