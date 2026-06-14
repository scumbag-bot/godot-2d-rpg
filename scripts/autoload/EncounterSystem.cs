using Godot;
using rpg_game.scripts.data;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.autoload;

public partial class EncounterSystem : Node
{
    [Signal] public delegate void EncounterTriggeredEventHandler(EncounterEntry entry);

    public void Trigger(EncounterEntry entry)
    {
        EmitSignal(SignalName.EncounterTriggered, entry);
    }
}
