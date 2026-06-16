using Godot;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.autoload;

public partial class QuestStore : Node
{
    private readonly QuestState _state = new();

    [Signal] public delegate void StageAdvancedEventHandler(StringName questId, int newStage);
    [Signal] public delegate void FlagSetEventHandler(StringName flagName);

    public int GetStage(StringName questId) => _state.GetStage(questId.ToString());

    public void SetStage(StringName questId, int stage)
    {
        var id = questId.ToString();
        if (_state.SetStage(id, stage))
            EmitSignal(SignalName.StageAdvanced, questId, stage);
    }

    public void Advance(StringName questId)
    {
        var next = _state.GetStage(questId.ToString()) + 1;
        SetStage(questId, next);
    }

    public bool GetFlag(StringName flagName) => _state.GetFlag(flagName.ToString());

    public void SetFlag(StringName flagName)
    {
        if (_state.SetFlag(flagName.ToString()))
            EmitSignal(SignalName.FlagSet, flagName);
    }

    public void ClearFlag(StringName flagName) => _state.ClearFlag(flagName.ToString());
}
