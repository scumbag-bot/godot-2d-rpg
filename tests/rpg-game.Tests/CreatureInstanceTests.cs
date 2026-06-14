using NUnit.Framework;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class CreatureInstanceTests
{
    private static CreatureInstance Make(int level = 5)
    {
        var species = new CreatureSpeciesLite
        {
            DisplayName = "Wolf",
            Types = new[] { CreatureType.Beast },
            BaseStats = new BaseStats(40, 50, 40, 30, 30, 60),
        };
        return new CreatureInstance(species, level);
    }

    [Test]
    public void NewInstance_StartsAtFullHp()
    {
        var c = Make(level: 10);
        Assert.That(c.CurrentHp, Is.EqualTo(c.MaxHp));
        Assert.That(c.CurrentHp, Is.GreaterThan(0));
    }

    [Test]
    public void TakeDamage_ReducesHp()
    {
        var c = Make();
        int before = c.CurrentHp;
        c.TakeDamage(5);
        Assert.That(c.CurrentHp, Is.EqualTo(before - 5));
    }

    [Test]
    public void TakeDamage_CannotGoBelowZero()
    {
        var c = Make();
        c.TakeDamage(9999);
        Assert.That(c.CurrentHp, Is.EqualTo(0));
        Assert.That(c.IsFainted, Is.True);
    }

    [Test]
    public void GainExperience_TriggersLevelUp()
    {
        var c = Make(level: 2);
        int targetExp = CreatureInstance.ExpToNext(2);
        bool leveled = c.GainExperience(targetExp);
        Assert.That(leveled, Is.True);
        Assert.That(c.Level, Is.EqualTo(3));
    }

    [Test]
    public void ExpToNext_GrowsCubically()
    {
        Assert.That(CreatureInstance.ExpToNext(2), Is.EqualTo(8));
        Assert.That(CreatureInstance.ExpToNext(5), Is.EqualTo(125));
    }
}
