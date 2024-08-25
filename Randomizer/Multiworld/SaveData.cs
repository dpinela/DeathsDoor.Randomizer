using CG = System.Collections.Generic;

namespace DDoor.Randomizer.Multiworld;

internal record class SaveData(
    string ServerAddr,
    int PlayerId,
    int RandoId,
    string[] RemoteNicknames,
    CG.List<RemoteItem> RemoteItems) {}