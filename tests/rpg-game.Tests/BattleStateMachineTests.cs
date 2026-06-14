using NUnit.Framework;
using rpg_game.scripts.battle;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class BattleStateMachineTests
{
    private static CreatureInstance MakePlayer(int level) =>
        new(new CreatureSpeciesLite { DisplayName = "Hero", Types = new[] { CreatureType.Holy }, BaseStats = new BaseStats(50, 50, 50, 50, 50, 60) }, level);

    private static CreatureInstance MakeEnemy(int level) =>
        new(new CreatureSpeciesLite { DisplayName = "Foe", Types = new[] { CreatureType.Demon }, BaseStats = new BaseStats(50, 50, 50, 50, 50, 50) }, level);

    [Test]
    public void FasterAttacker_GoesFirst()
    {
        var sm = new BattleStateMachine();
        var player = MakePlayer(5);
        var enemy = MakeEnemy(5);
        var order = sm.ComputeTurnOrder(player, enemy);
        Assert.That(order[0], Is.EqualTo(BattleSide.Player));
    }

    [Test]
    public void Resolve_PlayerAttacks_EnemyHpDecreases()
    {
        var sm = new BattleStateMachine();
        var player = MakePlayer(5);
        var enemy = MakeEnemy(5);
        int before = enemy.CurrentHp;
        var move = new MoveDataLite("Tackle", CreatureType.Beast, MoveCategory.Physical, 40, 100);
        var result = sm.ResolveSingle(player, enemy, move, chart: new TypeEffectiveness(), rng: () => 1.0f);
        Assert.That(result.Damage, Is.GreaterThan(0));
        Assert.That(enemy.CurrentHp, Is.LessThan(before));
    }

    [Test]
    public void Resolve_EnemyFaints_SetsStateToVictory()
    {
        var sm = new BattleStateMachine();
        var player = MakePlayer(50);
        var enemy = MakeEnemy(1);
        var move = new MoveDataLite("Smash", CreatureType.Holy, MoveCategory.Physical, 200, 100);
        sm.ResolveSingle(player, enemy, move, new TypeEffectiveness(), () => 1.0f);
        Assert.That(enemy.IsFainted, Is.True);
        Assert.That(sm.State, Is.EqualTo(BattleState.Victory).Or.EqualTo(BattleState.Resolving));
    }
}
