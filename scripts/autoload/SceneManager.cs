using Godot;

namespace rpg_game.scripts.autoload;

public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }
    public Vector2? PendingPlayerPosition { get; set; }

    private bool _isWarping;
    private string _returnScenePath;
    private Vector2 _returnPlayerPosition;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void GotoScene(string path)
    {
        GetTree().ChangeSceneToFile(path);
    }

    public void GotoOverworld()
    {
        GotoScene("res://scenes/Town.tscn");
    }

    public void GotoBattle()
    {
        GotoScene("res://scenes/Battle.tscn");
    }

    public void BeginBattle(Vector2 playerPosition)
    {
        var current = GetTree().CurrentScene;
        if (current != null && !string.IsNullOrEmpty(current.SceneFilePath)
            && !current.SceneFilePath.EndsWith("Battle.tscn"))
        {
            _returnScenePath = current.SceneFilePath;
            _returnPlayerPosition = playerPosition;
        }
        GotoScene("res://scenes/Battle.tscn");
    }

    public void ReturnFromBattle()
    {
        if (string.IsNullOrEmpty(_returnScenePath))
        {
            GotoOverworld();
            return;
        }
        var path = _returnScenePath;
        var pos = _returnPlayerPosition + new Vector2(0, -32);
        _returnScenePath = null;
        _returnPlayerPosition = Vector2.Zero;
        WarpTo(path, pos);
    }

    public void GotoTitle()
    {
        GotoScene("res://scenes/TitleScreen.tscn");
    }

    public async void WarpTo(string scenePath, Vector2 targetPosition)
    {
        if (_isWarping) return;
        _isWarping = true;

        var (layer, rect) = CreateFadeOverlay();
        var tweenOut = rect.CreateTween();
        tweenOut.TweenProperty(rect, "modulate:a", 1.0f, 0.2);
        await ToSignal(tweenOut, Tween.SignalName.Finished);

        PendingPlayerPosition = targetPosition;
        GetTree().ChangeSceneToFile(scenePath);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var tweenIn = rect.CreateTween();
        tweenIn.TweenProperty(rect, "modulate:a", 0.0f, 0.2);
        await ToSignal(tweenIn, Tween.SignalName.Finished);
        layer.QueueFree();

        _isWarping = false;
    }

    private (CanvasLayer layer, ColorRect rect) CreateFadeOverlay()
    {
        var layer = new CanvasLayer { Layer = 100, ProcessMode = ProcessModeEnum.Always };
        var rect = new ColorRect
        {
            Name = "WarpFade",
            Color = new Color(0, 0, 0, 1),
            Modulate = new Color(1, 1, 1, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.AnchorRight = 1.0f;
        rect.AnchorBottom = 1.0f;
        layer.AddChild(rect);
        GetTree().Root.AddChild(layer);
        return (layer, rect);
    }
}
