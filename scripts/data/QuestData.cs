using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class QuestData : Resource
{
    [Export] public StringName Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Godot.Collections.Array<QuestStage> Stages { get; set; } = new();
    [Export] public int StartStage { get; set; } = 0;
    [Export] public StringName NextQuestId { get; set; } = "";
}
