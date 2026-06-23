using NUnit.Framework;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class QuestStateBoundsTests
{
    [Test]
    public void AdvanceWithBounds_WithinRange_IncrementsAndReturnsTrue()
    {
        var s = new QuestState();
        s.SetStage("main", 0);
        var result = s.AdvanceWithBounds("main", 5);
        Assert.That(result, Is.True);
        Assert.That(s.GetStage("main"), Is.EqualTo(1));
    }

    [Test]
    public void AdvanceWithBounds_AtLastStage_ReturnsFalse_DoesNotOvershoot()
    {
        var s = new QuestState();
        s.SetStage("main", 4);
        var result = s.AdvanceWithBounds("main", 4);
        Assert.That(result, Is.False);
        Assert.That(s.GetStage("main"), Is.EqualTo(4));
    }

    [Test]
    public void AdvanceWithBounds_PastLastStage_ReturnsFalse()
    {
        var s = new QuestState();
        s.SetStage("main", 5);
        var result = s.AdvanceWithBounds("main", 4);
        Assert.That(result, Is.False);
        Assert.That(s.GetStage("main"), Is.EqualTo(5));
    }

    [Test]
    public void SetStageGuarded_NegativeValue_ReturnsFalse_DoesNotSet()
    {
        var s = new QuestState();
        s.SetStage("main", 0);
        var result = s.SetStageGuarded("main", -99);
        Assert.That(result, Is.False);
        Assert.That(s.GetStage("main"), Is.EqualTo(0));
    }

    [Test]
    public void SetStageGuarded_ValidValue_ReturnsTrueAndSets()
    {
        var s = new QuestState();
        var result = s.SetStageGuarded("main", 3);
        Assert.That(result, Is.True);
        Assert.That(s.GetStage("main"), Is.EqualTo(3));
    }

    [Test]
    public void SetStageGuarded_MinusOne_ReturnsTrue()
    {
        var s = new QuestState();
        var result = s.SetStageGuarded("main", -1);
        Assert.That(result, Is.True);
        Assert.That(s.GetStage("main"), Is.EqualTo(-1));
    }
}
