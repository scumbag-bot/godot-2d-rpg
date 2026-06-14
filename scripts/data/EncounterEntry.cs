using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class EncounterEntry : Resource
{
    [Export] public CreatureSpecies Species { get; set; }
    [Export] public int Level { get; set; } = 3;
    [Export] public bool CaptureEligible { get; set; } = true;
}
