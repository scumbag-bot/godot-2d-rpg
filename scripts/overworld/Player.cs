using Godot;

namespace rpg_game.scripts.overworld;

public partial class Player : CharacterBody2D
{
    [Export] public int Speed = 80;

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Velocity = input * Speed;
        MoveAndSlide();
    }
}
