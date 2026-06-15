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

## Dialog Box (visual novel style)

| # | Check | Result |
|---|---|---|
| 1 | DialogBox self-subscribes to DialogPlayer in _Ready (no Overworld wiring) | PASS |
| 2 | DialogLine Resource in scripts/data/ with SpeakerName, SpeakerPortrait, Text | PASS |
| 3 | DialogPlayer.Play awaits DialogBox.AdvancePressed between lines (no 1.5s timer) | PASS |
| 4 | Npc.Lines is `Godot.Collections.Array<DialogLine>`; passes to DialogPlayer | PASS |
| 5 | TownStarter passes DialogLine[] to DialogPlayer | PASS |
| 6 | Typewriter uses Tween on Label.visible_characters; kills cleanly on input | PASS |
| 7 | First input during typewriter reveals full line; second input emits AdvancePressed | PASS |
| 8 | AdvanceArrow visible only when line fully revealed | PASS |
| 9 | Portrait TextureRect hidden when SpeakerPortrait is null | PASS |
| 10 | DialogBox.Instance null guard in DialogPlayer.Play | PASS |

## Runtime paths to validate in Godot editor

1. Title -> New Game -> Overworld
2. Walk into Enemy Area2D -> wild battle starts
3. Use Fight -> enemy takes damage / auto-captures on faint
4. Lose battle -> defeat message, party healed to half HP, return to Town
5. Walk into Town NPC -> dialog panel appears at bottom, overworld dimmed above
6. First line text reveals character-by-character (~30ms/char)
7. Press any key mid-typing -> full text appears immediately
8. Press any key after text complete -> next line begins
9. ▼ arrow visible at bottom-right only when line fully revealed
10. After last line, press key -> dialog hides
11. Mouse click on dialog area also advances
12. Re-enter NPC -> dialog replays from line 1

## Build / test

```
dotnet build rpg-game.sln -c Debug   # 0 errors
dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj   # 18/18
```
