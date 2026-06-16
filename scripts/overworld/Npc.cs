using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Npc : Area2D
{
    [Export] public Godot.Collections.Array<DialogLine> Lines;
    [Export] public StringName NpcId = "";
    [Export] public StringName ActiveQuestId = "main";

    private bool _awaitingDialogCompletion;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        GetNode<DialogPlayer>("/root/DialogPlayer").DialogFinished += OnDialogFinished;
    }

    public override void _ExitTree()
    {
        var dp = GetNodeOrNull<DialogPlayer>("/root/DialogPlayer");
        if (dp != null) dp.DialogFinished -= OnDialogFinished;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player) return;
        if (GetNode<DialogPlayer>("/root/DialogPlayer").IsActive) return;

        var quest = ResolveActiveQuest();
        if (quest != null)
        {
            var stageIdx = GetNode<QuestStore>("/root/QuestStore").GetStage(ActiveQuestId);
            if (stageIdx >= 0 && stageIdx < quest.Stages.Count)
            {
                var stage = quest.Stages[stageIdx];
                var entry = FindEntryForNpc(stage, NpcId);
                if (entry != null && entry.Lines != null && entry.Lines.Count > 0)
                {
                    var arr = new DialogLine[entry.Lines.Count];
                    for (int i = 0; i < entry.Lines.Count; i++) arr[i] = entry.Lines[i];
                    _awaitingDialogCompletion = stage.AdvanceOn == QuestStage.AdvanceTrigger.NpcContact
                        && stage.TargetNpcId == NpcId;
                    GetNode<DialogPlayer>("/root/DialogPlayer").Play(arr);
                    return;
                }
            }
        }

        if (Lines != null && Lines.Count > 0)
        {
            var arr = new DialogLine[Lines.Count];
            for (int i = 0; i < Lines.Count; i++) arr[i] = Lines[i];
            GetNode<DialogPlayer>("/root/DialogPlayer").Play(arr);
        }
    }

    private void OnDialogFinished()
    {
        if (!_awaitingDialogCompletion) return;
        _awaitingDialogCompletion = false;
        GetNode<QuestStore>("/root/QuestStore").Advance(ActiveQuestId);
    }

    private QuestData ResolveActiveQuest()
    {
        if (ActiveQuestId.IsEmpty) return null;
        return GD.Load<QuestData>("res://resources/data/quests/MainQuest.tres");
    }

    private static DialogPerNpcEntry FindEntryForNpc(QuestStage stage, StringName npcId)
    {
        if (stage.DialogPerNpc == null) return null;
        foreach (var e in stage.DialogPerNpc)
            if (e != null && e.NpcId == npcId) return e;
        return null;
    }
}
