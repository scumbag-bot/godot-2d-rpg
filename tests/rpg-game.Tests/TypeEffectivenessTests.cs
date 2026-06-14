using NUnit.Framework;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class TypeEffectivenessTests
{
    [Test]
    public void DefaultEffectiveness_IsOne()
    {
        var t = new TypeEffectiveness();
        Assert.That(t.Get(CreatureType.Beast, CreatureType.Demon), Is.EqualTo(1.0f));
    }

    [Test]
    public void SetAndGet_Works()
    {
        var t = new TypeEffectiveness();
        t.Set(CreatureType.Holy, CreatureType.Demon, 2.0f);
        Assert.That(t.Get(CreatureType.Holy, CreatureType.Demon), Is.EqualTo(2.0f));
        // symmetry
        Assert.That(t.Get(CreatureType.Demon, CreatureType.Holy), Is.EqualTo(2.0f));
    }
}
