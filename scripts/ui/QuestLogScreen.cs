using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.ui;

public partial class QuestLogScreen : CanvasLayer
{
    private bool _visible;
    private VBoxContainer _mainSection;
    private VBoxContainer _sideSection;
    private Label _mainHeader;
    private Label _sideHeader;
    private Label _closeHint;

    public override void _Ready()
    {
        Visible = false;
        _visible = false;
        Layer = 100;

        var bg = new ColorRect();
        bg.Color = new Color(0, 0, 0, 0.85f);
        bg.AnchorRight = 1.0f;
        bg.AnchorBottom = 1.0f;
        AddChild(bg);

        var panel = new Panel();
        panel.SetSize(new Vector2(500, 400));
        panel.Position = new Vector2(
            (GetViewport().GetVisibleRect().Size.X - 500) / 2,
            (GetViewport().GetVisibleRect().Size.Y - 400) / 2
        );
        AddChild(panel);

        var scroll = new ScrollContainer();
        scroll.SetSize(new Vector2(480, 340));
        scroll.Position = new Vector2(10, 10);
        panel.AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.SetSize(new Vector2(460, 0));
        scroll.AddChild(vbox);

        _mainHeader = CreateHeader("Main Quests");
        vbox.AddChild(_mainHeader);
        _mainSection = new VBoxContainer();
        vbox.AddChild(_mainSection);

        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 16);
        vbox.AddChild(spacer);

        _sideHeader = CreateHeader("Side Quests");
        vbox.AddChild(_sideHeader);
        _sideSection = new VBoxContainer();
        vbox.AddChild(_sideSection);

        _closeHint = new Label();
        _closeHint.Text = "Press Q or Tab to close";
        _closeHint.HorizontalAlignment = HorizontalAlignment.Center;
        _closeHint.AddThemeColorOverride("font_color", new Color(0.44f, 0.38f, 0.25f));
        _closeHint.AddThemeFontSizeOverride("font_size", 11);
        _closeHint.SetPosition(new Vector2(10, 365));
        panel.AddChild(_closeHint);

        SetProcessInput(true);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("quest_log"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Toggle()
    {
        _visible = !_visible;
        Visible = _visible;
        if (_visible) Refresh();
    }

    private void Refresh()
    {
        var tracker = GetNodeOrNull<QuestTracker>("/root/QuestTracker");
        if (tracker == null) return;
        var quests = tracker.GetAllQuests();

        ClearChildren(_mainSection);
        ClearChildren(_sideSection);

        foreach (var quest in quests)
        {
            var section = quest.QuestCategory == QuestData.Category.Main ? _mainSection : _sideSection;
            section.AddChild(CreateQuestEntry(quest, tracker));
        }

        _mainHeader.Visible = _mainSection.GetChildCount() > 0;
        _sideHeader.Visible = _sideSection.GetChildCount() > 0;
    }

    private Control CreateQuestEntry(QuestData quest, QuestTracker tracker)
    {
        var stage = tracker.GetQuestStage(quest.Id);
        var total = quest.Stages.Count;
        var complete = stage >= total;

        var container = new VBoxContainer();
        container.CustomMinimumSize = new Vector2(0, 48);

        var nameRow = new HBoxContainer();
        var nameLabel = new Label();
        nameLabel.Text = quest.DisplayName;
        nameLabel.AddThemeColorOverride("font_color", new Color(0.91f, 0.84f, 0.64f));
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameRow.AddChild(nameLabel);

        var stageLabel = new Label();
        if (complete)
        {
            stageLabel.Text = "Complete";
            stageLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.63f, 0.5f));
        }
        else if (stage >= 0)
        {
            stageLabel.Text = $"Stage {stage + 1}/{total}";
            stageLabel.AddThemeColorOverride("font_color", new Color(0.44f, 0.38f, 0.25f));
        }
        stageLabel.AddThemeFontSizeOverride("font_size", 11);
        nameRow.AddChild(stageLabel);
        container.AddChild(nameRow);

        if (!complete && stage >= 0 && stage < total)
        {
            var bar = new ColorRect();
            bar.CustomMinimumSize = new Vector2(0, 4);
            bar.Color = new Color(0.23f, 0.16f, 0.1f);
            container.AddChild(bar);

            var fillBar = new ColorRect();
            fillBar.CustomMinimumSize = new Vector2((float)(stage + 1) / total * 440, 4);
            fillBar.Color = new Color(0.75f, 0.63f, 0.38f);
            bar.AddChild(fillBar);

            if (!string.IsNullOrEmpty(quest.Stages[stage].ObjectiveText))
            {
                var objLabel = new Label();
                objLabel.Text = $"  > {quest.Stages[stage].ObjectiveText}";
                objLabel.AddThemeColorOverride("font_color", new Color(0.63f, 0.56f, 0.44f));
                objLabel.AddThemeFontSizeOverride("font_size", 11);
                container.AddChild(objLabel);
            }
        }

        var sep = new HSeparator();
        sep.CustomMinimumSize = new Vector2(0, 8);
        container.AddChild(sep);

        return container;
    }

    private Label CreateHeader(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", new Color(0.75f, 0.63f, 0.38f));
        label.AddThemeFontSizeOverride("font_size", 12);
        return label;
    }

    private static void ClearChildren(Container parent)
    {
        var children = parent.GetChildren();
        foreach (var child in children)
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}
