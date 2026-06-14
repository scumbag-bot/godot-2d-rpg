# F5 — Code-level Smoke Verification

Godot editor not available in this environment, so runtime smoke test was
substituted with code-level wiring verification. Requires Godot 4.6 editor
for full play-through.

## Checklist

| # | Check | Result |
|---|---|---|
| 1 | TitleScreen `OnNewGamePressed` -> `res://scenes/Overworld.tscn` | PASS |
| 2 | Overworld subscribes `EncounterTriggered`, calls `BattleManager.StartWild` + `SceneManager.GotoBattle` | PASS |
| 3 | `BattleManager.StartWild` builds enemy from `entry.Species.ToLite()` + `entry.Level` | PASS |
| 4 | `BattleManager.PlayerAttack(MoveDataLite)`; `OpenMovePicker` builds `MoveDataLite` and wires button | PASS |
| 5 | `Party` autoload: `Members`, `Active`, `Add`, `HasSpace`; referenced as `/root/Party` | PASS |
| 6 | `OnBattleEnded` returns to Town on defeat, Overworld on victory/escape, 1.5s timer | PASS |
| 7 | Defeat heals all `party.Members` to `MaxHp/2`; `CreatureInstance.Heal` clamps to MaxHp | PASS |
| 8 | Auto-capture: `new CreatureInstance(EnemyInstance.Species, EnemyInstance.Level)` + `Party.Add` | PASS |
| 9 | `TypeChart.tres` loaded via `GD.Load`, `ToEffective()` parses 8 `Attacker->Defender` entries | PASS |
| 10 | All `.tres` resources present (8 species, 6 moves, 3 encounters, 1 type chart) | PASS |
| 11 | 6 autoloads registered in `project.godot` with `*res://` prefix | PASS |
| 12 | `dotnet build rpg-game.sln -c Debug` -> 0 errors (29 nullable warnings) | PASS |
| 13 | `dotnet test` -> 18/18 passed | PASS |

## Runtime paths to validate in Godot editor

1. Title -> New Game -> Overworld
2. Walk into Enemy Area2D -> wild battle starts
3. Use Fight -> enemy takes damage / auto-captures on faint
4. Lose battle -> defeat message, party healed to half HP, return to Town

## Build / test

```
dotnet build rpg-game.sln -c Debug   # 0 errors
dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj   # 18/18
```
