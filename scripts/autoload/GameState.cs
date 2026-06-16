using System.Collections.Generic;
using Godot;

namespace rpg_game.scripts.autoload;

public partial class GameState : Node
{
    public enum Mode { Title, Overworld, Battle, Dialog }

    public Mode Current { get; private set; } = Mode.Title;
    public Dictionary<string, int> Inventory { get; } = new()
    {
        { "Dummy Potion", 1 },
    };

    [Signal] public delegate void ModeChangedEventHandler(int mode);

    public void SetMode(Mode mode)
    {
        Current = mode;
        EmitSignal(SignalName.ModeChanged, (int)mode);
    }
}
