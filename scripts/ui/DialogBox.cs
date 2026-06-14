using Godot;

namespace rpg_game.scripts.ui;

public partial class DialogBox : Control
{
    [Export] public Label TextLabel;

    public override void _Ready()
    {
        Visible = false;
    }

    public void ShowLine(string text)
    {
        TextLabel.Text = text;
        Visible = true;
    }

    public void Hide()
    {
        Visible = false;
    }
}
