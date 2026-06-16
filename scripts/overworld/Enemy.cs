using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.battle;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Enemy : Area2D
{
	[Export] public EncounterEntry Entry;
	[Export] public float IdleAfterEscapeSeconds = 7.0f;
	[Export] public float ReTriggerCooldownSeconds = 1.5f;

	private ulong _idleUntilMs = 0;
	private bool _onCooldown = false;

	public override void _Ready()
	{
		var bm = GetNode<BattleManager>("/root/BattleManager");
		if (bm.LastBattleEntry != null && bm.LastBattleEntry == Entry)
		{
			if (bm.LastBattleOutcome == (int)BattleState.Victory)
			{
				QueueFree();
				return;
			}
			if (bm.LastBattleOutcome == (int)BattleState.Escaped)
			{
				_idleUntilMs = Time.GetTicksMsec() + (ulong)(IdleAfterEscapeSeconds * 1000);
			}
		}
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_onCooldown) return;
		if (Time.GetTicksMsec() < _idleUntilMs) return;
		if (body is not Player) return;
		if (Entry == null) return;
		if (GetNode<autoload.Party>("/root/Party").Active == null) return;
		_onCooldown = true;
		CallDeferred(MethodName.TriggerEncounter);
		var timer = GetTree().CreateTimer(ReTriggerCooldownSeconds);
		timer.Timeout += () => { if (IsInstanceValid(this)) _onCooldown = false; };
	}

	private void TriggerEncounter()
	{
		if (!IsInstanceValid(this)) return;
		GetNode<EncounterSystem>("/root/EncounterSystem").Trigger(Entry);
	}
}
