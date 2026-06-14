using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class EncounterTable : Resource
{
    [Export] public StringName AreaId { get; set; } = "";
    [Export] public Godot.Collections.Array<EncounterEntry> Entries { get; set; } = new();
}
