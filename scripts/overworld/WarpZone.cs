using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.overworld;

public partial class WarpZone : Area2D
{
    [Export] public string TargetScenePath = "";
    [Export] public Vector2 TargetPosition = Vector2.Zero;
    [Export] public bool OneShot = true;

    private bool _triggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_triggered && OneShot) return;
        if (body is not Player) return;
        if (string.IsNullOrEmpty(TargetScenePath)) return;

        var dialog = GetNodeOrNull<DialogPlayer>("/root/DialogPlayer");
        if (dialog != null && dialog.IsActive) return;

        _triggered = true;
        GetNode<SceneManager>("/root/SceneManager").WarpTo(TargetScenePath, TargetPosition);
    }
}
