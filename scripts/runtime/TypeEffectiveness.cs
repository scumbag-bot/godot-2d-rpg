using System;
using System.Collections.Generic;

namespace rpg_game.scripts.runtime;

public class TypeEffectiveness
{
    private readonly float[,] _matrix;

    public TypeEffectiveness()
    {
        int n = Enum.GetValues<CreatureType>().Length;
        _matrix = new float[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                _matrix[i, j] = 1.0f;
    }

    public float Get(CreatureType attacker, CreatureType defender)
    {
        return _matrix[(int)attacker, (int)defender];
    }

    public void Set(CreatureType attacker, CreatureType defender, float multiplier)
    {
        _matrix[(int)attacker, (int)defender] = multiplier;
        _matrix[(int)defender, (int)attacker] = multiplier;
    }

    public float GetMultiplicative(CreatureType moveType, IReadOnlyList<CreatureType> defenderTypes)
    {
        float m = 1.0f;
        foreach (var t in defenderTypes) m *= Get(moveType, t);
        return m;
    }
}
