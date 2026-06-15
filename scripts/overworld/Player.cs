using Godot;

namespace rpg_game.scripts.overworld;

public partial class Player : CharacterBody2D
{
    [Export] public int Speed = 80;
    [Export] public NodePath SpritePath = "Sprite";

    private AnimatedSprite2D _sprite;
    private string _currentAnim = "";
    private Vector2 _facing = Vector2.Down;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>(SpritePath);
        if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation("idle_down"))
            _sprite.Play("idle_down");
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Velocity = input * Speed;
        UpdateAnimation(input);
        MoveAndSlide();
    }

    private void UpdateAnimation(Vector2 input)
    {
        if (_sprite == null) return;

        if (input != Vector2.Zero)
            _facing = input;

        bool moving = input != Vector2.Zero;
        string dir = _facing.X > 0.1f ? "right"
                   : _facing.X < -0.1f ? "left"
                   : _facing.Y > 0.1f ? "down"
                   : "up";

        string desired = (moving ? "walk_" : "idle_") + dir;

        if (_sprite.SpriteFrames == null || !_sprite.SpriteFrames.HasAnimation(desired))
            return;

        if (_currentAnim != desired)
        {
            _sprite.Play(desired);
            _currentAnim = desired;
        }
    }
}
