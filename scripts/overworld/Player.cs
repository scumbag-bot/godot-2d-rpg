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
		_sprite = GetNodeOrNull<AnimatedSprite2D>(SpritePath);
		if (_sprite == null)
		{
			var names = new System.Text.StringBuilder();
			foreach (var c in GetChildren()) names.Append(c.Name).Append(", ");
			GD.PushError($"Player: AnimatedSprite2D not found at '{SpritePath}'. Children: {names}");
			return;
		}
		if (_sprite.SpriteFrames == null)
		{
			GD.PushError("Player: Sprite.SpriteFrames is null");
			return;
		}
		if (_sprite.SpriteFrames.HasAnimation("idle_down"))
		{
			_sprite.Play("idle_down");
			_currentAnim = "idle_down";
		}

		var sm = GetNodeOrNull<autoload.SceneManager>("/root/SceneManager");
		if (sm != null && sm.PendingPlayerPosition.HasValue)
		{
			GlobalPosition = sm.PendingPlayerPosition.Value;
			sm.PendingPlayerPosition = null;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetNode<autoload.DialogPlayer>("/root/DialogPlayer").IsActive)
		{
			Velocity = Vector2.Zero;
			UpdateAnimation(Vector2.Zero);
			MoveAndSlide();
			return;
		}
		var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = input * Speed;
		UpdateAnimation(input);
		MoveAndSlide();
	}

	private void UpdateAnimation(Vector2 input)
	{
		if (_sprite == null || _sprite.SpriteFrames == null) return;

		if (input != Vector2.Zero)
			_facing = input;

		bool moving = input != Vector2.Zero;
		string dir = _facing.X > 0.1f ? "right"
				   : _facing.X < -0.1f ? "left"
				   : _facing.Y > 0.1f ? "down"
				   : "up";

		string desired = (moving ? "walk_" : "idle_") + dir;

		if (!_sprite.SpriteFrames.HasAnimation(desired))
		{
			GD.PushWarning($"Player: animation '{desired}' missing");
			return;
		}

		if (_currentAnim != desired)
		{
			_sprite.Stop();
			_sprite.Play(desired);
			_currentAnim = desired;
		}
	}
}
