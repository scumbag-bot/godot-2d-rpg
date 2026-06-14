using Godot;
using rpg_game.scripts.battle;
using rpg_game.scripts.data;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.autoload;

public partial class BattleManager : Node
{
    [Signal] public delegate void BattleStartedEventHandler(bool isWild);
    [Signal] public delegate void StateChangedEventHandler(int state);
    [Signal] public delegate void MessageEventHandler(string text);
    [Signal] public delegate void HpChangedEventHandler(int side, int hp, int hpMax);
    [Signal] public delegate void BattleEndedEventHandler(int outcome);

    public BattleStateMachine Fsm { get; private set; } = new();
    public CreatureInstance PlayerInstance { get; private set; }
    public CreatureInstance EnemyInstance { get; private set; }
    public bool IsWild { get; private set; }
    public EncounterEntry CurrentEntry { get; private set; }

    public void StartWild(EncounterEntry entry)
    {
        IsWild = true;
        CurrentEntry = entry;
        PlayerInstance = GetNode<Party>("/root/Party").Active;
        EnemyInstance = new CreatureInstance(entry.Species.ToLite(), entry.Level);
        Fsm = new BattleStateMachine();
        EmitSignal(SignalName.BattleStarted, true);
        EmitSignal(SignalName.HpChanged, 0, PlayerInstance.CurrentHp, PlayerInstance.MaxHp);
        EmitSignal(SignalName.HpChanged, 1, EnemyInstance.CurrentHp, EnemyInstance.MaxHp);
        EmitSignal(SignalName.Message, $"A wild {entry.Species.DisplayName} appeared!");
        EmitSignal(SignalName.StateChanged, (int)BattleState.PlayerTurn);
    }

    public void PlayerAttack(MoveDataLite move)
    {
        if (EnemyInstance == null || EnemyInstance.IsFainted) return;
        var chart = LoadChart();
        EmitSignal(SignalName.Message, $"{PlayerInstance.Species.DisplayName} used {move.Name}!");
        var result = Fsm.ResolveSingle(PlayerInstance, EnemyInstance, move, chart, Rng);
        EmitSignal(SignalName.HpChanged, 1, EnemyInstance.CurrentHp, EnemyInstance.MaxHp);
        if (result.Damage > 0) EmitSignal(SignalName.Message, $"It dealt {result.Damage} damage.");
        if (EnemyInstance.IsFainted)
        {
            EmitSignal(SignalName.Message, $"{EnemyInstance.Species.DisplayName} fainted!");
            if (IsWild && CurrentEntry.CaptureEligible && GetNode<Party>("/root/Party").HasSpace())
            {
                var captured = new CreatureInstance(EnemyInstance.Species, EnemyInstance.Level);
                GetNode<Party>("/root/Party").Add(captured);
                EmitSignal(SignalName.Message, $"{captured.Species.DisplayName} joined your party!");
            }
            EmitSignal(SignalName.BattleEnded, (int)BattleState.Victory);
            return;
        }
        EnemyTurn();
    }

    public void Run()
    {
        EmitSignal(SignalName.Message, "Got away safely!");
        EmitSignal(SignalName.BattleEnded, (int)BattleState.Escaped);
    }

    private void EnemyTurn()
    {
        var move = PickEnemyMove();
        EmitSignal(SignalName.Message, $"{EnemyInstance.Species.DisplayName} used {move.Name}!");
        var chart = LoadChart();
        var result = Fsm.ResolveSingle(EnemyInstance, PlayerInstance, move, chart, Rng);
        EmitSignal(SignalName.HpChanged, 0, PlayerInstance.CurrentHp, PlayerInstance.MaxHp);
        if (result.Damage > 0) EmitSignal(SignalName.Message, $"It dealt {result.Damage} damage.");
        if (PlayerInstance.IsFainted)
        {
            EmitSignal(SignalName.Message, $"{PlayerInstance.Species.DisplayName} fainted!");
            EmitSignal(SignalName.BattleEnded, (int)BattleState.Defeat);
            return;
        }
        EmitSignal(SignalName.StateChanged, (int)BattleState.PlayerTurn);
    }

    private MoveDataLite PickEnemyMove()
    {
        var type = EnemyInstance.Species.Types[0];
        return new MoveDataLite("Strike", type, MoveCategory.Physical, 40, 100);
    }

    private TypeEffectiveness LoadChart()
    {
        var chartRes = GD.Load<TypeChart>("res://resources/data/TypeChart.tres");
        return chartRes != null ? chartRes.ToEffective() : new TypeEffectiveness();
    }

    private static readonly System.Random _rng = new();
    private float Rng() => 0.85f + (float)_rng.NextDouble() * 0.15f;
}
