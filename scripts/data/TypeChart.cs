using System;
using Godot;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class TypeChart : Resource
{
    [Export] public Godot.Collections.Dictionary<StringName, float> Multipliers { get; set; } = new();

    public TypeEffectiveness ToEffective()
    {
        var t = new TypeEffectiveness();
        foreach (var kv in Multipliers)
        {
            // key format: "ATTACKER->DEFENDER" (e.g. "Holy->Demon")
            var parts = kv.Key.ToString().Split("->");
            if (parts.Length != 2) continue;
            if (!Enum.TryParse<CreatureType>(parts[0], out var a)) continue;
            if (!Enum.TryParse<CreatureType>(parts[1], out var d)) continue;
            t.Set(a, d, kv.Value);
        }
        return t;
    }
}
