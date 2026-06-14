using NUnit.Framework;
using rpg_game.scripts.battle;

namespace rpg_game.Tests;

public class BattleLogTests
{
    [Test]
    public void Append_StoresLinesInOrder()
    {
        var log = new BattleLog();
        log.Append("X used Y!");
        log.Append("It's super effective!");
        Assert.That(log.Lines, Is.EqualTo(new[] { "X used Y!", "It's super effective!" }));
    }
}
