using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.ui;

public partial class ItemMenu : Control
{
    [Export] public VBoxContainer SlotContainer;
    [Export] public Button BackButton;

    public override void _Ready()
    {
        Visible = false;
        if (BackButton != null) BackButton.Pressed += OnBackPressed;
    }

    private void OnBackPressed()
    {
        Visible = false;
        var actionMenu = GetNodeOrNull<Control>("/root/Battle/ActionMenu");
        if (actionMenu != null) actionMenu.Visible = true;
    }

    public void Show()
    {
        Visible = true;
        if (SlotContainer == null) return;
        foreach (var child in SlotContainer.GetChildren()) child.QueueFree();
        var inv = GetNode<GameState>("/root/GameState").Inventory;
        if (inv.Count == 0)
        {
            var row = new HBoxContainer();
            var label = new Label
            {
                Text = "  — no items —",
                Modulate = new Color(0.6f, 0.6f, 0.6f)
            };
            row.AddChild(label);
            SlotContainer.AddChild(row);
            return;
        }
        foreach (var (name, count) in inv)
        {
            var (displayName, desc) = GetItemInfo(name);
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = $"{displayName} x{count}" });
            row.AddChild(new Label { Text = "  " + desc });
            var btn = new Button { Text = "Use" };
            var capturedName = name;
            btn.Pressed += () => UseItem(capturedName);
            row.AddChild(btn);
            SlotContainer.AddChild(row);
        }
    }

    private (string Name, string Desc) GetItemInfo(string name)
    {
        return name switch
        {
            "Dummy Potion" => ("Dummy Potion", "+5 HP"),
            "Potion" => ("Potion", "+20 HP"),
            "Revive" => ("Revive", "50% HP from 0"),
            _ => (name, ""),
        };
    }

    private void UseItem(string name)
    {
        var gs = GetNode<GameState>("/root/GameState");
        if (!gs.Inventory.ContainsKey(name) || gs.Inventory[name] <= 0) return;
        var party = GetNode<Party>("/root/Party");
        var active = party.Active;
        if (active == null) return;
        int heal = name switch
        {
            "Dummy Potion" => 5,
            "Potion" => 20,
            "Revive" => 0,
            _ => 0,
        };
        if (name == "Revive" && active.IsFainted) active.Heal(active.MaxHp / 2);
        else if (heal > 0) active.Heal(heal);
        gs.Inventory[name]--;
        if (gs.Inventory[name] <= 0) gs.Inventory.Remove(name);
        Show();
        if (gs.Inventory.Count == 0) OnBackPressed();
    }
}
