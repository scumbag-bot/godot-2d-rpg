using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Enemy : Area2D
{
    [Export] public EncounterEntry Entry;
    [Export] public float ReEnableDelay = 1.5f;

    public override void _Ready()
    {
        Monitoring = false;
        BodyEntered += OnBodyEntered;
        CallDeferred(MethodName.EnableMonitoring);
    }

    private void EnableMonitoring()
    {
        if (!IsInstanceValid(this)) return;
        Monitoring = true;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!Monitoring) return;
        if (body is not Player) return;
        if (Entry == null) return;
        if (GetNode<autoload.Party>("/root/Party").Active == null) return;
        Monitoring = false;
        CallDeferred(MethodName.TriggerEncounter);
        StartCooldown();
    }

    private void StartCooldown()
    {
        var timer = GetTree().CreateTimer(ReEnableDelay);
        timer.Timeout += EnableMonitoring;
    }

    private void TriggerEncounter()
    {
        if (!IsInstanceValid(this)) return;
        GetNode<EncounterSystem>("/root/EncounterSystem").Trigger(Entry);
    }
}
