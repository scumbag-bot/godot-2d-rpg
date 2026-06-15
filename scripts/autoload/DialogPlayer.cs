using Godot;
using rpg_game.scripts.data;
using rpg_game.scripts.ui;

namespace rpg_game.scripts.autoload;

public partial class DialogPlayer : Node
{
    [Signal] public delegate void DialogStartedEventHandler();
    [Signal] public delegate void LineChangedEventHandler(string name, Texture2D portrait, string text);
    [Signal] public delegate void DialogFinishedEventHandler();

    public bool IsActive { get; private set; }

    public async void Play(DialogLine[] lines)
    {
        if (IsActive) return;
        if (DialogBox.Instance == null)
        {
            GD.PushWarning("DialogPlayer.Play: no DialogBox.Instance; ignoring.");
            return;
        }

        IsActive = true;
        EmitSignal(SignalName.DialogStarted);

        foreach (var line in lines)
        {
            var name = line.SpeakerName ?? "";
            EmitSignal(SignalName.LineChanged, name, line.SpeakerPortrait, line.Text);
            await ToSignal(DialogBox.Instance, DialogBox.SignalName.AdvancePressed);
        }

        IsActive = false;
        EmitSignal(SignalName.DialogFinished);
    }
}
