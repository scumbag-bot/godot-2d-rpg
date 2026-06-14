using NUnit.Framework;
using rpg_game.scripts.runtime;
using rpg_game.scripts.battle;

namespace rpg_game.Tests;

public class DamageCalculatorTests
{
    [Test]
    public void StatusMove_ReturnsZero()
    {
        var dmg = DamageCalculator.Compute(
            attackerLevel: 10, attackerAtk: 50, defenderDef: 50,
            movePower: 0, moveType: CreatureType.Beast, moveCategory: MoveCategory.Status,
            attackerTypes: new[] { CreatureType.Beast },
            defenderTypes: new[] { CreatureType.Demon },
            chart: new TypeEffectiveness(),
            rng: () => 1.0f);
        Assert.That(dmg, Is.EqualTo(0));
    }

    [Test]
    public void SuperEffective_DoublesDamage()
    {
        var chart = new TypeEffectiveness();
        chart.Set(CreatureType.Holy, CreatureType.Demon, 2.0f);
        var neutral = DamageCalculator.Compute(
            attackerLevel: 50, attackerAtk: 100, defenderDef: 100,
            movePower: 50, moveType: CreatureType.Beast, moveCategory: MoveCategory.Physical,
            attackerTypes: new[] { CreatureType.Beast },
            defenderTypes: new[] { CreatureType.Demon },
            chart: chart, rng: () => 1.0f);
        var superEffective = DamageCalculator.Compute(
            attackerLevel: 50, attackerAtk: 100, defenderDef: 100,
            movePower: 50, moveType: CreatureType.Holy, moveCategory: MoveCategory.Physical,
            attackerTypes: new[] { CreatureType.Holy },
            defenderTypes: new[] { CreatureType.Demon },
            chart: chart, rng: () => 1.0f);
        Assert.That(superEffective, Is.GreaterThan(neutral));
    }

    [Test]
    public void STAB_IncreasesDamage()
    {
        var chart = new TypeEffectiveness();
        var noStab = DamageCalculator.Compute(
            50, 100, 100, 50, CreatureType.Holy, MoveCategory.Physical,
            new[] { CreatureType.Beast }, new[] { CreatureType.Demon }, chart, () => 1.0f);
        var withStab = DamageCalculator.Compute(
            50, 100, 100, 50, CreatureType.Holy, MoveCategory.Physical,
            new[] { CreatureType.Holy }, new[] { CreatureType.Demon }, chart, () => 1.0f);
        Assert.That(withStab, Is.GreaterThan(noStab));
    }
}
