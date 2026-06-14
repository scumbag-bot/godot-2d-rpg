using Godot;

namespace rpg_game.scripts.ui;

public partial class TitleScreen : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBox/NewGameButton").Pressed += OnNewGamePressed;
    }

    private void OnNewGamePressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Town.tscn");
    }
}
