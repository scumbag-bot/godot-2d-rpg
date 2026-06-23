using System.Collections.Generic;
using Godot;
using rpg_game.scripts.data;
using rpg_game.scripts.ui;

namespace rpg_game.scripts.autoload;

public partial class QuestTracker : Node
{
    private QuestLogScreen _logScreen;
    private Label _hudLabel;
    private Label _toastLabel;
    private Tween _toastTween;

    public override void _Ready()
    {
        var qs = GetNode<QuestStore>("/root/QuestStore");
        qs.StageAdvanced += OnStageAdvanced;
        qs.QuestCompleted += OnQuestCompleted;

        _hudLabel = new Label();
        _hudLabel.AnchorRight = 1.0f;
        _hudLabel.OffsetRight = -12;
        _hudLabel.OffsetTop = 12;
        _hudLabel.AddThemeColorOverride("font_color", new Color(0.91f, 0.84f, 0.64f));
        _hudLabel.AddThemeFontSizeOverride("font_size", 12);
        _hudLabel.Visible = false;
        AddChild(_hudLabel);

        _toastLabel = new Label();
        _toastLabel.AnchorLeft = 0.5f;
        _toastLabel.AnchorRight = 0.5f;
        _toastLabel.AnchorBottom = 1.0f;
        _toastLabel.OffsetBottom = -60;
        _toastLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _toastLabel.AddThemeColorOverride("font_color", new Color(0.91f, 0.84f, 0.64f));
        _toastLabel.AddThemeFontSizeOverride("font_size", 16);
        _toastLabel.Modulate = new Color(1, 1, 1, 0);
        AddChild(_toastLabel);

        CallDeferred(nameof(SpawnLogScreen));
        CallDeferred(nameof(RefreshHud));
    }

    private void SpawnLogScreen()
    {
        var scene = GD.Load<PackedScene>("res://scenes/QuestLogScreen.tscn");
        if (scene == null) return;
        _logScreen = scene.Instantiate<QuestLogScreen>();
        if (_logScreen != null) AddChild(_logScreen);
    }

    public void OnStageAdvanced(StringName questId, int newStage)
    {
        RefreshHud();
    }

    public void OnQuestCompleted(StringName questId)
    {
        var quest = LoadQuestData(questId.ToString());
        if (quest != null)
            ShowToast("Quest Complete!");
        RefreshHud();
    }

    public void OnBattleWin(EncounterPack pack)
    {
        if (pack?.Entries == null) return;
        foreach (var entry in pack.Entries)
        {
            if (entry?.Species == null) continue;
            TryAdvanceByTrigger(QuestStage.AdvanceTrigger.BattleWin, null, entry.Species.Id.ToString(), null);
        }
    }

    public void OnAreaEnter(StringName areaId)
    {
        TryAdvanceByTrigger(QuestStage.AdvanceTrigger.AreaEnter, null, null, areaId.ToString());
    }

    public void ToggleLog()
    {
        _logScreen?.Toggle();
    }

    public IReadOnlyList<QuestData> GetAllQuests()
    {
        var list = new List<QuestData>();
        var dir = DirAccess.Open(QuestPaths.QuestsDir);
        if (dir == null) return list;
        foreach (var fileName in dir.GetFiles())
        {
            if (!fileName.EndsWith(".tres")) continue;
            var quest = GD.Load<QuestData>($"{QuestPaths.QuestsDir}{fileName}");
            if (quest != null) list.Add(quest);
        }
        return list;
    }

    public int GetQuestStage(StringName questId)
    {
        return GetNode<QuestStore>("/root/QuestStore").GetStage(questId);
    }

    private void RefreshHud()
    {
        var quests = GetAllQuests();
        QuestData activeQuest = null;
        foreach (var q in quests)
        {
            var stage = GetNode<QuestStore>("/root/QuestStore").GetStage(q.Id);
            if (stage >= 0 && stage < q.Stages.Count)
            {
                if (activeQuest == null || (q.QuestCategory == QuestData.Category.Main && activeQuest.QuestCategory != QuestData.Category.Main))
                    activeQuest = q;
            }
        }

        if (activeQuest != null)
        {
            var stageIdx = GetNode<QuestStore>("/root/QuestStore").GetStage(activeQuest.Id);
            var obj = stageIdx >= 0 && stageIdx < activeQuest.Stages.Count
                ? activeQuest.Stages[stageIdx].ObjectiveText : "";
            _hudLabel.Text = $"{activeQuest.DisplayName}: {obj}";
            _hudLabel.Visible = true;
        }
        else
        {
            _hudLabel.Visible = false;
        }
    }

    private void ShowToast(string text)
    {
        _toastLabel.Text = text;
        _toastLabel.Modulate = new Color(1, 1, 1, 0);
        _toastTween?.Kill();
        _toastTween = CreateTween();
        _toastTween.TweenProperty(_toastLabel, "modulate", new Color(1, 1, 1, 1), 0.3f);
        _toastTween.TweenInterval(2.5f);
        _toastTween.TweenProperty(_toastLabel, "modulate", new Color(1, 1, 1, 0), 0.3f);
    }

    private void TryAdvanceByTrigger(QuestStage.AdvanceTrigger trigger, string npcId, string speciesId, string areaId)
    {
        var qs = GetNode<QuestStore>("/root/QuestStore");
        var dir = DirAccess.Open(QuestPaths.QuestsDir);
        if (dir == null) return;
        foreach (var fileName in dir.GetFiles())
        {
            if (!fileName.EndsWith(".tres")) continue;
            var quest = GD.Load<QuestData>($"{QuestPaths.QuestsDir}{fileName}");
            if (quest == null) continue;
            var stageIdx = qs.GetStage(quest.Id);
            if (stageIdx < 0 || stageIdx >= quest.Stages.Count) continue;
            var stage = quest.Stages[stageIdx];
            if (stage.AdvanceOn != trigger) continue;
            var match = trigger switch
            {
                QuestStage.AdvanceTrigger.NpcContact => stage.TargetNpcId.ToString() == npcId,
                QuestStage.AdvanceTrigger.BattleWin => stage.TargetSpeciesId.ToString() == speciesId,
                QuestStage.AdvanceTrigger.AreaEnter => stage.TargetAreaId.ToString() == areaId,
                _ => false
            };
            if (match) qs.Advance(quest.Id);
        }
    }

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
