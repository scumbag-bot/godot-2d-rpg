using Godot;
using System.Threading.Tasks;

namespace rpg_game.scripts.autoload;

public partial class DialogPlayer : Node
{
    [Signal] public delegate void DialogFinishedEventHandler();
    [Signal] public delegate void LineShownEventHandler(string text);

    public bool IsActive { get; private set; }

    public async void Play(string[] lines)
    {
        if (IsActive) return;
        IsActive = true;
        foreach (var line in lines)
        {
            EmitSignal(SignalName.LineShown, line);
            await ToSignal(GetTree().CreateTimer(1.5), Timer.SignalName.Timeout);
        }
        IsActive = false;
        EmitSignal(SignalName.DialogFinished);
    }
}
