using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.ui;

public partial class ItemMenu : Control
{
    [Export] public VBoxContainer SlotContainer;

    public override void _Ready()
    {
        Visible = false;
    }

    public void Show()
    {
        Visible = true;
        if (SlotContainer == null) return;
        foreach (var child in SlotContainer.GetChildren()) child.QueueFree();
        var items = new[] { ("Potion", "+20 HP"), ("Revive", "50% HP from 0") };
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
    }
}
