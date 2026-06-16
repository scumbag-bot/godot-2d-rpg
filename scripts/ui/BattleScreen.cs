using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.battle;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.ui;

public partial class BattleScreen : Control
{
    [Export] public TextureRect EnemySprite;
    [Export] public TextureRect PlayerSprite;
    [Export] public Label EnemyNameLabel;
    [Export] public Label PlayerNameLabel;
    [Export] public HealthBar EnemyHpBar;
    [Export] public HealthBar PlayerHpBar;
    [Export] public Label MessageLabel;
    [Export] public Control ActionMenu;
    [Export] public Control MovePicker;
    [Export] public VBoxContainer MoveList;
    [Export] public PartyMenu PartyMenuRef;
    [Export] public ItemMenu ItemMenuRef;

    public override void _Ready()
    {
        var bm = GetNode<BattleManager>("/root/BattleManager");
        bm.BattleStarted += OnBattleStarted;
        bm.HpChanged += OnHpChanged;
        bm.Message += OnMessage;
        bm.BattleEnded += OnBattleEnded;
        bm.StateChanged += OnStateChanged;
        GetNode<Button>("ActionMenu/FightButton").Pressed += OpenMovePicker;
        GetNode<Button>("ActionMenu/CreatureButton").Pressed += OpenParty;
        GetNode<Button>("ActionMenu/ItemButton").Pressed += OpenItems;
        GetNode<Button>("ActionMenu/RunButton").Pressed += () => bm.Run();
    }

    public override void _ExitTree()
    {
        var bm = GetNodeOrNull<BattleManager>("/root/BattleManager");
        if (bm != null)
        {
            bm.BattleStarted -= OnBattleStarted;
            bm.HpChanged -= OnHpChanged;
            bm.Message -= OnMessage;
            bm.BattleEnded -= OnBattleEnded;
            bm.StateChanged -= OnStateChanged;
        }
    }

    private void OnBattleStarted(bool isWild)
    {
        ShowActionMenu();
    }

    private void OnStateChanged(int state)
    {
        if (state == (int)BattleState.PlayerTurn) ShowActionMenu();
    }

    private void OnHpChanged(int side, int hp, int hpMax)
    {
        if (side == 0) PlayerHpBar.SetValues(hp, hpMax);
        else EnemyHpBar.SetValues(hp, hpMax);
    }

    private void OnMessage(string text)
    {
        MessageLabel.Text = text;
    }

    private void OnBattleEnded(int outcome)
    {
        var sm = GetNode<SceneManager>("/root/SceneManager");
        if (outcome == (int)BattleState.Defeat)
        {
            MessageLabel.Text = "You blacked out...";
            var party = GetNode<Party>("/root/Party");
            foreach (var m in party.Members) m.Heal(m.MaxHp / 2);
            GetTree().CreateTimer(1.5).Timeout += () => sm.GotoScene("res://scenes/Town.tscn");
            return;
        }
        MessageLabel.Text = outcome == (int)BattleState.Victory ? "Victory!"
            : outcome == (int)BattleState.Escaped ? "Got away!" : "...";
        GetTree().CreateTimer(1.5).Timeout += sm.ReturnFromBattle;
    }

    private void ShowActionMenu()
    {
        ActionMenu.Visible = true;
        MovePicker.Visible = false;
        PartyMenuRef.Visible = false;
        ItemMenuRef.Visible = false;
    }

    private void OpenMovePicker()
    {
        ActionMenu.Visible = false;
        MovePicker.Visible = true;
        foreach (var c in MoveList.GetChildren()) c.QueueFree();
        var active = GetNode<Party>("/root/Party").Active;
        for (int i = 0; i < active.Species.Types.Count; i++)
        {
            var t = active.Species.Types[i];
            var move = new MoveDataLite($"Strike-{i}", t, MoveCategory.Physical, 40, 100);
            var btn = new Button { Text = move.Name };
            btn.Pressed += () => { GetNode<BattleManager>("/root/BattleManager").PlayerAttack(move); };
            MoveList.AddChild(btn);
        }
    }

    private void OpenParty()
    {
        PartyMenuRef.Rebuild();
        PartyMenuRef.Visible = true;
        ActionMenu.Visible = false;
    }

    private void OpenItems()
    {
        ItemMenuRef.Show();
        ActionMenu.Visible = false;
    }
}
