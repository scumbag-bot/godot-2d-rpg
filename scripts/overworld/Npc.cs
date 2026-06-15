using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Npc : Area2D
{
    [Export] public Godot.Collections.Array<DialogLine> Lines;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        GetNode<DialogPlayer>("/root/DialogPlayer").DialogFinished += OnDialogFinished;
    }

    public override void _ExitTree()
    {
        var dp = GetNodeOrNull<DialogPlayer>("/root/DialogPlayer");
        if (dp != null) dp.DialogFinished -= OnDialogFinished;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player) return;
        if (Lines == null || Lines.Count == 0) return;
        var arr = new DialogLine[Lines.Count];
        for (int i = 0; i < Lines.Count; i++) arr[i] = Lines[i];
        GetNode<DialogPlayer>("/root/DialogPlayer").Play(arr);
    }
}
