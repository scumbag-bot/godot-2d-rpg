using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.ui;

public partial class DialogBox : Control
{
    [Signal] public delegate void AdvancePressedEventHandler();

    public static DialogBox Instance { get; private set; }

    [Export] public Label TextLabel;
    [Export] public Label NameLabel;
    [Export] public TextureRect Portrait;
    [Export] public Label AdvanceArrow;

    private const float TypewriterSecondsPerChar = 0.03f;

    private Tween _typewriterTween;
    private bool _isFullyRevealed;
    private bool _isDialogActive;
    private string _pendingText = "";

    public override void _Ready()
    {
        Instance = this;
        Visible = false;
        if (AdvanceArrow != null) AdvanceArrow.Visible = false;

        var dp = GetNode<DialogPlayer>("/root/DialogPlayer");
        dp.DialogStarted += OnDialogStarted;
        dp.LineChanged += OnLineChanged;
        dp.DialogFinished += OnDialogFinished;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowLine(string name, Texture2D portrait, string text)
    {
        if (NameLabel != null) NameLabel.Text = string.IsNullOrEmpty(name) ? "" : name;
        if (Portrait != null)
        {
            Portrait.Texture = portrait;
            Portrait.Visible = portrait != null;
        }
        _pendingText = text ?? "";
        if (TextLabel != null)
        {
            TextLabel.Text = _pendingText;
            TextLabel.VisibleCharacters = 0;
        }
        _isFullyRevealed = false;
        if (AdvanceArrow != null) AdvanceArrow.Visible = false;

        _typewriterTween?.Kill();
        if (TextLabel != null && _pendingText.Length > 0)
        {
            var tween = TextLabel.CreateTween();
            tween.TweenProperty(TextLabel, "visible_characters", _pendingText.Length, _pendingText.Length * TypewriterSecondsPerChar);
            tween.Finished += OnTypewriterFinished;
            _typewriterTween = tween;
        }
        else
        {
            _isFullyRevealed = true;
            if (AdvanceArrow != null) AdvanceArrow.Visible = true;
        }
    }

    public void RevealInstant()
    {
        if (_isFullyRevealed) return;
        _typewriterTween?.Kill();
        _typewriterTween = null;
        if (TextLabel != null) TextLabel.VisibleCharacters = -1;
        _isFullyRevealed = true;
        if (AdvanceArrow != null) AdvanceArrow.Visible = true;
    }

    public void Hide()
    {
        _isDialogActive = false;
        _typewriterTween?.Kill();
        _typewriterTween = null;
        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isDialogActive) return;
        var isKey = @event is InputEventKey ke && ke.Pressed && !ke.Echo;
        var isClick = @event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left;
        if (!isKey && !isClick) return;

        if (!_isFullyRevealed)
        {
            RevealInstant();
        }
        else
        {
            EmitSignal(SignalName.AdvancePressed);
        }
        GetViewport().SetInputAsHandled();
    }

    private void OnDialogStarted()
    {
        _isDialogActive = true;
        Visible = true;
    }

    private void OnLineChanged(string name, Texture2D portrait, string text)
    {
        ShowLine(name, portrait, text);
    }

    private void OnDialogFinished()
    {
        Hide();
    }

    private void OnTypewriterFinished()
    {
        if (TextLabel != null) TextLabel.VisibleCharacters = -1;
        _isFullyRevealed = true;
        if (AdvanceArrow != null) AdvanceArrow.Visible = true;
    }
}
