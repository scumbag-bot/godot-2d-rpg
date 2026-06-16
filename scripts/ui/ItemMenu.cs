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
        var items = new (string Name, string Desc)[]
        {
            ("Potion", "+20 HP"),
            ("Revive", "50% HP from 0"),
        };
        if (items.Length == 0)
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
        foreach (var (name, desc) in items)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = name });
            row.AddChild(new Label { Text = "  " + desc });
            var btn = new Button { Text = "Use" };
            var capturedName = name;
            btn.Pressed += () => UseItem(capturedName);
            row.AddChild(btn);
            SlotContainer.AddChild(row);
        }
    }

    private void UseItem(string name)
    {
        var party = GetNode<Party>("/root/Party");
        var active = party.Active;
        if (active == null) return;
        if (name == "Potion") active.Heal(20);
        if (name == "Revive" && active.IsFainted) active.Heal(active.MaxHp / 2);
        Visible = false;
        var actionMenu = GetNodeOrNull<Control>("/root/Battle/ActionMenu");
        if (actionMenu != null) actionMenu.Visible = true;
    }
}
