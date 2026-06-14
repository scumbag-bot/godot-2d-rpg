using System;
using System.Collections.Generic;
using System.Linq;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.battle;

public static class DamageCalculator
{
    public const float StabMultiplier = 1.5f;

    public static int Compute(
        int attackerLevel,
        int attackerAtk,
        int defenderDef,
        int movePower,
        CreatureType moveType,
        MoveCategory moveCategory,
        IReadOnlyList<CreatureType> attackerTypes,
        IReadOnlyList<CreatureType> defenderTypes,
        TypeEffectiveness chart,
        Func<float> rng)
    {
        if (moveCategory == MoveCategory.Status || movePower <= 0)
            return 0;
        if (defenderDef <= 0) defenderDef = 1;

        float baseDmg = ((2f * attackerLevel / 5f) + 2f) * movePower * attackerAtk / defenderDef / 50f + 2f;

        float stab = attackerTypes.Contains(moveType) ? StabMultiplier : 1.0f;
        float typeEff = chart.GetMultiplicative(moveType, defenderTypes);
        float roll = rng();

        float final = baseDmg * stab * typeEff * roll;
        return Math.Max(1, (int)Math.Floor(final));
    }
}
