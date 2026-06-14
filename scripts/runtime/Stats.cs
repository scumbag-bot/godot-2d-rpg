namespace rpg_game.scripts.runtime;

public readonly record struct Stats(
    int Hp,
    int Attack,
    int Defense,
    int SpAtk,
    int SpDef,
    int Speed)
{
    public static int Compute(int baseStat, int level, int iv, int ev)
    {
        return ((2 * baseStat + iv + ev / 4) * level) / 100 + 5;
    }

    public static int ComputeHp(int baseStat, int level, int iv, int ev)
    {
        return ((2 * baseStat + iv + ev / 4) * level) / 100 + level + 10;
    }

    public static Stats FromBase(int hp, int atk, int def, int spa, int spd, int spe, int level)
    {
        return new Stats(
            ComputeHp(hp, level, 0, 0),
            Compute(atk, level, 0, 0),
            Compute(def, level, 0, 0),
            Compute(spa, level, 0, 0),
            Compute(spd, level, 0, 0),
            Compute(spe, level, 0, 0));
    }
}
