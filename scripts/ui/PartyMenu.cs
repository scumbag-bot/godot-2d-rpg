using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.ui;

public partial class PartyMenu : Control
{
    [Export] public VBoxContainer SlotContainer;
    [Export] public Button BackButton;

    public override void _Ready()
    {
        Visible = false;
        if (BackButton != null) BackButton.Pressed += OnBackPressed;
        Rebuild();
    }

    private void OnBackPressed()
    {
        Visible = false;
        var actionMenu = GetNodeOrNull<Control>("/root/Battle/ActionMenu");
        if (actionMenu != null) actionMenu.Visible = true;
    }

    public void Rebuild()
    {
        if (SlotContainer == null) return;
        foreach (var child in SlotContainer.GetChildren()) child.QueueFree();
        var party = GetNode<Party>("/root/Party");
        for (int i = 0; i < party.Members.Count; i++)
        {
            var c = party.Members[i];
            var row = new HBoxContainer();
            var label = new Label
            {
                Text = (i == party.ActiveIndex ? "▶ " : "  ") + c.Species.DisplayName + $" Lv{c.Level} HP {c.CurrentHp}/{c.MaxHp}"
            };
            row.AddChild(label);
            SlotContainer.AddChild(row);
        }
        for (int i = party.Members.Count; i < Party.MaxSize; i++)
        {
            var row = new HBoxContainer();
            var label = new Label
            {
                Text = "  — empty —",
                Modulate = new Color(0.6f, 0.6f, 0.6f)
            };
            row.AddChild(label);
            SlotContainer.AddChild(row);
        }
    }
}
