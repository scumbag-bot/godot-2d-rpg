using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class QuestStage : Resource
{
    public enum AdvanceTrigger { None, NpcContact, BattleWin, AreaEnter }

    [Export] public StringName Id { get; set; } = "";
    [Export] public Godot.Collections.Array<DialogPerNpcEntry> DialogPerNpc { get; set; } = new();
    [Export] public AdvanceTrigger AdvanceOn { get; set; } = AdvanceTrigger.None;
    [Export] public StringName TargetNpcId { get; set; } = "";
    [Export] public StringName TargetSpeciesId { get; set; } = "";
    [Export] public StringName TargetAreaId { get; set; } = "";
}
