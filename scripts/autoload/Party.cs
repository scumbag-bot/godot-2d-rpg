using System.Collections.Generic;
using Godot;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.autoload;

public partial class Party : Node
{
    public const int MaxSize = 6;
    public List<CreatureInstance> Members { get; } = new();
    public int ActiveIndex { get; private set; } = 0;

    [Signal] public delegate void PartyChangedEventHandler();
    [Signal] public delegate void ActiveChangedEventHandler(int index);

    public CreatureInstance Active => (Members.Count > 0 && ActiveIndex < Members.Count) ? Members[ActiveIndex] : null;

    public void Add(CreatureInstance member)
    {
        if (Members.Count >= MaxSize) return;
        Members.Add(member);
        EmitSignal(SignalName.PartyChanged);
    }

    public void SetActive(int index)
    {
        if (index < 0 || index >= Members.Count) return;
        ActiveIndex = index;
        EmitSignal(SignalName.ActiveChanged, index);
    }

    public bool HasSpace() => Members.Count < MaxSize;
}
