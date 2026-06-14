using Godot;
using rpg_game.scripts.battle;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class MoveData : Resource
{
    [Export] public StringName Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public CreatureType Type { get; set; } = CreatureType.Beast;
    [Export] public MoveCategory Category { get; set; } = MoveCategory.Physical;
    [Export] public int Power { get; set; } = 40;
    [Export] public int Accuracy { get; set; } = 100;
    [Export] public int Pp { get; set; } = 20;
    [Export] public string Description { get; set; } = "";

    public MoveDataLite ToLite() => new(DisplayName, Type, Category, Power, Accuracy);
}
