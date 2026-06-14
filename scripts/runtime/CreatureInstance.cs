using System;
using System.Collections.Generic;

namespace rpg_game.scripts.runtime;

public class CreatureInstance
{
    public CreatureSpeciesLite Species { get; }
    public int Level { get; private set; }
    public int Experience { get; private set; }
    public int CurrentHp { get; private set; }
    public int MaxHp => Stats.ComputeHp(Species.BaseStats.Hp, Level, 0, 0);
    public bool IsFainted => CurrentHp <= 0;

    public CreatureInstance(CreatureSpeciesLite species, int level)
    {
        Species = species;
        Level = level;
        CurrentHp = MaxHp;
    }

    public void TakeDamage(int amount)
    {
        CurrentHp = Math.Max(0, CurrentHp - amount);
    }

    public void Heal(int amount)
    {
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }

    public bool GainExperience(int amount)
    {
        Experience += amount;
        while (Experience >= ExpToNext(Level))
        {
            Experience -= ExpToNext(Level);
            Level++;
            CurrentHp = MaxHp;
            return true;
        }
        return false;
    }

    public static int ExpToNext(int level) => level * level * level;
}

public record CreatureSpeciesLite
{
    public string DisplayName { get; init; } = "";
    public IReadOnlyList<CreatureType> Types { get; init; } = Array.Empty<CreatureType>();
    public BaseStats BaseStats { get; init; } = new(0, 0, 0, 0, 0, 0);
}

public record BaseStats(int Hp, int Attack, int Defense, int SpAtk, int SpDef, int Speed);
