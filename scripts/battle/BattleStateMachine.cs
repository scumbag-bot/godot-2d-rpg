using System;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.battle;

public class BattleStateMachine
{
    public BattleState State { get; private set; } = BattleState.Intro;
    private readonly Random _rng = new();

    public BattleSide[] ComputeTurnOrder(CreatureInstance player, CreatureInstance enemy)
    {
        int ps = Stats.Compute(player.Species.BaseStats.Speed, player.Level, 0, 0);
        int es = Stats.Compute(enemy.Species.BaseStats.Speed, enemy.Level, 0, 0);
        if (ps == es) return _rng.Next(0, 2) == 0
            ? new[] { BattleSide.Player, BattleSide.Enemy }
            : new[] { BattleSide.Enemy, BattleSide.Player };
        return ps > es
            ? new[] { BattleSide.Player, BattleSide.Enemy }
            : new[] { BattleSide.Enemy, BattleSide.Player };
    }

    public DamageResult ResolveSingle(
        CreatureInstance attacker,
        CreatureInstance defender,
        MoveDataLite move,
        TypeEffectiveness chart,
        Func<float> rng)
    {
        State = BattleState.Resolving;
        var aStats = attacker.Species.BaseStats;
        var dStats = defender.Species.BaseStats;
        int atk = move.Category == MoveCategory.Special ? aStats.SpAtk : aStats.Attack;
        int def = move.Category == MoveCategory.Special ? dStats.SpDef : dStats.Defense;
        int dmg = DamageCalculator.Compute(
            attacker.Level, atk, def, move.Power, move.Type, move.Category,
            attacker.Species.Types, defender.Species.Types, chart, rng);
        defender.TakeDamage(dmg);
        if (!defender.IsFainted)
        {
            State = BattleState.TurnEnd;
        }
        return new DamageResult(dmg, defender.IsFainted);
    }
}

public record DamageResult(int Damage, bool TargetFainted);
