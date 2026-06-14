using NUnit.Framework;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class StatsTests
{
    [Test]
    public void Compute_AppliesPokemonGen3Formula()
    {
        // base=50, level=10, iv=0, ev=0
        // (2*50 + 0 + 0) * 10 / 100 + 5 = 10 + 5 = 15
        var s = Stats.Compute(baseStat: 50, level: 10, iv: 0, ev: 0);
        Assert.That(s, Is.EqualTo(15));
    }

    [Test]
    public void ComputeHp_AddsLevelAndTen()
    {
        // (2*50 + 0) * 10 / 100 + 10 + 10 = 10 + 10 + 10 = 30
        var s = Stats.ComputeHp(baseStat: 50, level: 10, iv: 0, ev: 0);
        Assert.That(s, Is.EqualTo(30));
    }

    [Test]
    public void Compute_GrowsWithLevel()
    {
        var low = Stats.Compute(50, 5, 0, 0);
        var high = Stats.Compute(50, 50, 0, 0);
        Assert.That(high, Is.GreaterThan(low));
    }
}
