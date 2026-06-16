using System.Collections.Generic;
using Godot;

namespace rpg_game.scripts.autoload;

public partial class GameState : Node
{
    public enum Mode { Title, Overworld, Battle, Dialog }

    public Mode Current { get; private set; } = Mode.Title;

    private Dictionary<string, int> _inventory = new()
    {
        { "Dummy Potion", 2 },
    };

    public Dictionary<string, int> Inventory => _inventory;

    [Signal] public delegate void ModeChangedEventHandler(int mode);

    public override void _Ready()
    {
        if (_inventory == null) _inventory = new Dictionary<string, int>();
        if (_inventory.Count == 0) _inventory["Dummy Potion"] = 2;
    }

    public void SetMode(Mode mode)
    {
        Current = mode;
        EmitSignal(SignalName.ModeChanged, (int)mode);
    }
}
