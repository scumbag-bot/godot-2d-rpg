using NUnit.Framework;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class QuestStoreTests
{
    [Test]
    public void NewStore_StartsAtMinusOne()
    {
        var s = new QuestState();
        Assert.That(s.GetStage("main"), Is.EqualTo(-1));
    }

    [Test]
    public void SetStage_Persists()
    {
        var s = new QuestState();
        s.SetStage("main", 3);
        Assert.That(s.GetStage("main"), Is.EqualTo(3));
    }

    [Test]
    public void Advance_IncrementsFromCurrent()
    {
        var s = new QuestState();
        s.SetStage("main", 2);
        s.Advance("main");
        Assert.That(s.GetStage("main"), Is.EqualTo(3));
    }

    [Test]
    public void Advance_FromMinusOne_GoesToZero()
    {
        var s = new QuestState();
        s.Advance("main");
        Assert.That(s.GetStage("main"), Is.EqualTo(0));
    }

    [Test]
    public void Flags_DefaultFalse()
    {
        var s = new QuestState();
        Assert.That(s.GetFlag("tutorial_done"), Is.False);
    }

    [Test]
    public void SetFlag_Persists()
    {
        var s = new QuestState();
        s.SetFlag("tutorial_done");
        Assert.That(s.GetFlag("tutorial_done"), Is.True);
    }

    [Test]
    public void ClearFlag_ResetsToFalse()
    {
        var s = new QuestState();
        s.SetFlag("x");
        s.ClearFlag("x");
        Assert.That(s.GetFlag("x"), Is.False);
    }
}
