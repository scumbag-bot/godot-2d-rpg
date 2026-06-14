using rpg_game.scripts.runtime;

namespace rpg_game.scripts.battle;

public abstract record BattleAction(BattleSide Side);

public record AttackAction(BattleSide Side, MoveDataLite Move) : BattleAction(Side);

public record SwitchAction(BattleSide Side, int NewIndex) : BattleAction(Side);

public record UseItemAction(BattleSide Side, string ItemId) : BattleAction(Side);

public record RunAction(BattleSide Side) : BattleAction(Side);

public record MoveDataLite(
    string Name,
    CreatureType Type,
    MoveCategory Category,
    int Power,
    int Accuracy);
