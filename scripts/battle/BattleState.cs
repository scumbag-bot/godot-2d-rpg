namespace rpg_game.scripts.battle;

public enum BattleState
{
    Intro,
    PlayerTurn,
    EnemyTurn,
    Resolving,
    TurnEnd,
    Fainted,
    SwitchPrompt,
    Victory,
    Defeat,
    Escaped,
}

public enum BattleSide { Player, Enemy }
