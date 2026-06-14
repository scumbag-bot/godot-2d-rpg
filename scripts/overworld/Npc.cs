using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.ui;

namespace rpg_game.scripts.overworld;

public partial class Npc : Area2D
{
    [Export] public string[] Lines;
    [Export] public DialogBox DialogBoxRef;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player && Lines != null && Lines.Length > 0)
        {
            GetNode<DialogPlayer>("/root/DialogPlayer").Play(Lines);
        }
    }
}
