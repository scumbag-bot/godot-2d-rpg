using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class DialogPerNpcEntry : Resource
{
    [Export] public StringName NpcId { get; set; } = "";
    [Export] public Godot.Collections.Array<DialogLine> Lines { get; set; } = new();
}
