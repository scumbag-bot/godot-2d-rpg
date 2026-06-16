using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Enemy : Area2D
{
    [Export] public EncounterEntry Entry;
    [Export] public float ReEnableDelay = 1.5f;

    private Godot.Collections.Array<Node2D> _initialBodies = new();
    private bool _initialConsumed = false;

    public override void _Ready()
    {
        GetTree().PhysicsFrame += CaptureAfterPhysics;
    }

    private void CaptureAfterPhysics()
    {
        if (!IsInstanceValid(this)) return;
        GetTree().PhysicsFrame -= CaptureAfterPhysics;
        _initialBodies = new(GetOverlappingBodies());
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!_initialConsumed)
        {
            _initialConsumed = true;
            if (_initialBodies.Contains(body)) return;
        }
        if (body is not Player) return;
        if (Entry == null) return;
        if (GetNode<autoload.Party>("/root/Party").Active == null) return;
        SetDeferred("monitoring", false);
        CallDeferred(MethodName.TriggerEncounter);
        StartCooldown();
    }

    private void StartCooldown()
    {
        var timer = GetTree().CreateTimer(ReEnableDelay);
        timer.Timeout += () => { if (IsInstanceValid(this)) SetDeferred("monitoring", true); };
    }

    private void TriggerEncounter()
    {
        if (!IsInstanceValid(this)) return;
        GetNode<EncounterSystem>("/root/EncounterSystem").Trigger(Entry);
    }
}
