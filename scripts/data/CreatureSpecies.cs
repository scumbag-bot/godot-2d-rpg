using System.Linq;
using Godot;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class CreatureSpecies : Resource
{
    [Export] public StringName Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Godot.Collections.Array<CreatureType> Types { get; set; } = new();
    [Export] public int BaseHp { get; set; } = 50;
    [Export] public int BaseAttack { get; set; } = 50;
    [Export] public int BaseDefense { get; set; } = 50;
    [Export] public int BaseSpAtk { get; set; } = 50;
    [Export] public int BaseSpDef { get; set; } = 50;
    [Export] public int BaseSpeed { get; set; } = 50;
    [Export] public Godot.Collections.Dictionary<int, StringName[]> Learnset { get; set; } = new();
    [Export] public Texture2D FrontSprite { get; set; }
    [Export] public Texture2D BackSprite { get; set; }
    [Export] public Color PlaceholderColor { get; set; } = new Color(1, 1, 1);

    public CreatureSpeciesLite ToLite() => new()
    {
        DisplayName = DisplayName,
        Types = Types.ToArray(),
        BaseStats = new BaseStats(BaseHp, BaseAttack, BaseDefense, BaseSpAtk, BaseSpDef, BaseSpeed)
    };
}
