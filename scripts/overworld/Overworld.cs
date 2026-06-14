using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;
using rpg_game.scripts.ui;

namespace rpg_game.scripts.overworld;

public partial class Overworld : Node2D
{
    public override void _Ready()
    {
        var es = GetNode<EncounterSystem>("/root/EncounterSystem");
        es.EncounterTriggered += OnEncounter;
        GetNode<GameState>("/root/GameState").SetMode(GameState.Mode.Overworld);

        var dialogBox = GetNodeOrNull<DialogBox>("DialogBox");
        if (dialogBox != null)
        {
            var dp = GetNode<DialogPlayer>("/root/DialogPlayer");
            dp.LineShown += text => dialogBox.ShowLine(text);
            dp.DialogFinished += () => dialogBox.Hide();
        }
    }

    private void OnEncounter(EncounterEntry entry)
    {
        GetNode<BattleManager>("/root/BattleManager").StartWild(entry);
        GetNode<SceneManager>("/root/SceneManager").GotoBattle();
    }
}
