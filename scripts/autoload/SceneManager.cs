using Godot;

namespace rpg_game.scripts.autoload;

public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }
    public Vector2? PendingPlayerPosition { get; set; }

    private bool _isWarping;

    public override void _Ready()
    {
        Instance = this;
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

    public void GotoTitle()
    {
        GotoScene("res://scenes/TitleScreen.tscn");
    }

    public async void WarpTo(string scenePath, Vector2 targetPosition)
    {
        if (_isWarping) return;
        _isWarping = true;

        var overlay = CreateFadeOverlay();
        var tweenOut = overlay.CreateTween();
        tweenOut.TweenProperty(overlay, "modulate:a", 1.0f, 0.2);
        await ToSignal(tweenOut, Tween.SignalName.Finished);

        PendingPlayerPosition = targetPosition;
        GetTree().ChangeSceneToFile(scenePath);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var tweenIn = overlay.CreateTween();
        tweenIn.TweenProperty(overlay, "modulate:a", 0.0f, 0.2);
        await ToSignal(tweenIn, Tween.SignalName.Finished);
        overlay.QueueFree();

        _isWarping = false;
    }

    private CanvasLayer CreateFadeOverlay()
    {
        var layer = new CanvasLayer { Layer = 100 };
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
        return layer;
    }
}
