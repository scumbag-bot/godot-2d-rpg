using Godot;

namespace rpg_game.scripts.ui;

public partial class HealthBar : Control
{
    private int _hp;
    private int _hpMax;
    private Color _bg = new(0.1f, 0.1f, 0.1f);
    private Color _fg = new(0.2f, 0.8f, 0.2f);

    public void SetValues(int hp, int hpMax)
    {
        _hp = hp;
        _hpMax = Mathf.Max(1, hpMax);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(0, 0, Size.X, Size.Y);
        DrawRect(rect, _bg);
        float w = Size.X * ((float)_hp / _hpMax);
        DrawRect(new Rect2(0, 0, w, Size.Y), _fg);
    }
}
