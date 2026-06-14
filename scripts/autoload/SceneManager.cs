using Godot;

namespace rpg_game.scripts.autoload;

public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void GotoScene(string path)
    {
        GetTree().ChangeSceneToFile(path);
    }

    public void GotoOverworld()
    {
        GotoScene("res://scenes/Town.tscn");
    }

    public void GotoBattle()
    {
        GotoScene("res://scenes/Battle.tscn");
    }

    public void GotoTitle()
    {
        GotoScene("res://scenes/TitleScreen.tscn");
    }
}
