using System.Collections.Generic;

namespace rpg_game.scripts.battle;

public class BattleLog
{
    private readonly List<string> _lines = new();
    public IReadOnlyList<string> Lines => _lines;

    public void Append(string line) => _lines.Add(line);
    public void Clear() => _lines.Clear();
}
