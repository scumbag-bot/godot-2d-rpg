using System.Collections.Generic;

namespace rpg_game.scripts.runtime;

public class QuestState
{
    private readonly Dictionary<string, int> _stages = new();
    private readonly HashSet<string> _flags = new();

    public int GetStage(string questId)
    {
        return _stages.TryGetValue(questId, out var s) ? s : -1;
    }

    public bool SetStage(string questId, int stage)
    {
        var old = GetStage(questId);
        _stages[questId] = stage;
        return old != stage;
    }

    public void Advance(string questId)
    {
        SetStage(questId, GetStage(questId) + 1);
    }

    public bool GetFlag(string flagName) => _flags.Contains(flagName);

    public bool SetFlag(string flagName) => _flags.Add(flagName);

    public bool ClearFlag(string flagName) => _flags.Remove(flagName);
}
