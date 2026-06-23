using System.Collections.Generic;

namespace rpg_game.scripts.data;

public class EncounterPack
{
    public IReadOnlyList<EncounterEntry> Entries { get; }

    public EncounterPack(IReadOnlyList<EncounterEntry> entries)
    {
        Entries = entries;
    }

    public EncounterPack(EncounterEntry entry)
    {
        Entries = new[] { entry };
    }
}
