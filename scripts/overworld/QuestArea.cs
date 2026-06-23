using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.overworld;

public partial class QuestArea : Area2D
{
    [Export] public StringName TargetAreaId = "";

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player) return;
        GetNode<QuestTracker>("/root/QuestTracker").OnAreaEnter(TargetAreaId);
    }
}
