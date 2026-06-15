using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class DialogLine : Resource
{
    [Export] public string SpeakerName { get; set; } = "";
    [Export] public Texture2D SpeakerPortrait { get; set; }
    [Export] public string Text { get; set; } = "";
}
