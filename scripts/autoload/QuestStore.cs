using Godot;
using rpg_game.scripts.data;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.autoload;

public partial class QuestStore : Node
{
    private readonly QuestState _state = new();

    [Signal] public delegate void StageAdvancedEventHandler(StringName questId, int newStage);
    [Signal] public delegate void FlagSetEventHandler(StringName flagName);
    [Signal] public delegate void QuestCompletedEventHandler(StringName questId);

    public int GetStage(StringName questId) => _state.GetStage(questId.ToString());

    public void SetStage(StringName questId, int stage)
    {
        var id = questId.ToString();
        if (!_state.SetStageGuarded(id, stage)) return;
        EmitSignal(SignalName.StageAdvanced, questId, stage);

        var quest = LoadQuestData(id);
        if (quest != null && stage >= quest.Stages.Count)
            EmitSignal(SignalName.QuestCompleted, questId);
    }

    public void Advance(StringName questId)
    {
        var id = questId.ToString();
        var quest = LoadQuestData(id);
        if (quest == null) return;
        var maxStage = quest.Stages.Count - 1;
        var advanced = _state.AdvanceWithBounds(id, maxStage);
        if (advanced)
        {
            EmitSignal(SignalName.StageAdvanced, questId, _state.GetStage(id));
        }
        else
        {
            if (!quest.NextQuestId.IsEmpty && quest.NextQuestId.ToString().Length > 0)
                SetStage(quest.NextQuestId, 0);
            else
                EmitSignal(SignalName.QuestCompleted, questId);
        }
    }

    public bool GetFlag(StringName flagName) => _state.GetFlag(flagName.ToString());

    public void SetFlag(StringName flagName)
    {
        if (_state.SetFlag(flagName.ToString()))
            EmitSignal(SignalName.FlagSet, flagName);
    }

    public void ClearFlag(StringName flagName) => _state.ClearFlag(flagName.ToString());

    private QuestData LoadQuestData(string questId)
    {
        var dir = DirAccess.Open(QuestPaths.QuestsDir);
        if (dir == null) return null;
        foreach (var fileName in dir.GetFiles())
        {
            if (!fileName.EndsWith(".tres")) continue;
            var quest = GD.Load<QuestData>($"{QuestPaths.QuestsDir}{fileName}");
            if (quest != null && quest.Id.ToString() == questId) return quest;
        }
        return null;
    }
}
