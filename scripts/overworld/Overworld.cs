using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Overworld : Node2D
{
    public override void _Ready()
    {
        var es = GetNode<EncounterSystem>("/root/EncounterSystem");
        es.EncounterTriggered += OnEncounter;
        GetNode<GameState>("/root/GameState").SetMode(GameState.Mode.Overworld);
    }

    public override void _ExitTree()
    {
        var es = GetNodeOrNull<EncounterSystem>("/root/EncounterSystem");
        if (es != null) es.EncounterTriggered -= OnEncounter;
    }

    private void OnEncounter(EncounterEntry entry)
    {
        GetNode<BattleManager>("/root/BattleManager").StartWild(entry);
        GetNode<SceneManager>("/root/SceneManager").GotoBattle();
    }
}
