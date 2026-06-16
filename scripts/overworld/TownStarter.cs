using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.overworld;

public partial class TownStarter : Node
{
    [Export] public NodePath StarterNpcPath;
    private bool _given = false;

    public override void _Ready()
    {
        var npc = GetNodeOrNull<Npc>(StarterNpcPath);
        if (npc != null) npc.BodyEntered += OnEnter;
    }

    private void OnEnter(Node2D body)
    {
        if (_given || body is not Player) return;
        _given = true;
        var wolf = GD.Load<CreatureSpecies>("res://resources/data/creatures/Wolf.tres");
        var inst = new CreatureInstance(wolf.ToLite(), 5);
        GetNode<Party>("/root/Party").Add(inst);
        GetNode<QuestStore>("/root/QuestStore").SetStage("main", 0);
        GetNode<DialogPlayer>("/root/DialogPlayer").Play(new[]
        {
            new DialogLine { SpeakerName = "", SpeakerPortrait = null, Text = "You received a Wolf!" },
            new DialogLine { SpeakerName = "", SpeakerPortrait = null, Text = "Press on, brave tamer." },
        });
    }
}
