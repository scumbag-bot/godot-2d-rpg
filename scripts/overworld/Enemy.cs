using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Enemy : Area2D
{
    [Export] public EncounterEntry Entry;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player) return;
        if (Entry == null) return;
        if (GetNode<autoload.Party>("/root/Party").Active == null) return;
        GetNode<EncounterSystem>("/root/EncounterSystem").Trigger(Entry);
    }
}
