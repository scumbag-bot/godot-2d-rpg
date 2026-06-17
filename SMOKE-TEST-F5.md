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

## Quest System

| # | Check | Result |
|---|---|---|
| 1 | `MainQuest.tres` has 6 stages with AdvanceOn + target fields; sub_resources before [resource]; load_steps=19 | PASS |
| 2 | `SampleQuest.tres` has 2 stages (scout_intro, imp_bounty); load_steps=10 | PASS |
| 3 | `Npc.cs` exposes `[Export] ActiveQuestId` + `[Export] StartQuestOnFirstContact`; per-NPC quest id | PASS |
| 4 | `Npc.ResolveActiveQuest` scans `resources/data/quests/` for a .tres matching `ActiveQuestId` | PASS |
| 5 | `BattleManager.TryAdvanceOnBattleWin` scans all quests in the dir for matching BattleWin stage | PASS |
| 6 | `QuestPaths.cs` exposes `Main` (kept for docs/back-compat) + `QuestsDir` (used by scans) | PASS |
| 7 | `Town.tscn` Elder has `ActiveQuestId=&"main"` + `NpcId=&"elder"` + retains `Lines` fallback | PASS |
| 8 | `Town.tscn` Scout has `ActiveQuestId=&"sample"` + `NpcId=&"scout"` + `StartQuestOnFirstContact=true` | PASS |
| 9 | `Town.tscn` has new ImpEnemy Area2D referencing `SingleImp.tres` (so sample quest is reachable in Town) | PASS |
| 10 | `Npc._ExitTree` unsubscribes `DialogPlayer.DialogFinished` (no leak) | PASS |
| 11 | `QuestStore.Advance` only emits `StageAdvanced` when state actually changes | PASS |
| 12 | `dotnet test` -> 25/25 passed (18 existing + 7 QuestState tests) | PASS |

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

## Quest runtime path to validate in Godot editor

**Main quest:**

1. New Game -> Town. `QuestStore.GetStage("main")` returns -1.
2. Walk into Elder -> `TownStarter` fires, gifts Wolf + plays "You received a Wolf!" + "Press on, brave tamer." and calls `SetStage("main", 0)`. (Race: `Npc.OnBodyEntered` may also fire and play `DlgEntry_Elder_0` if signal subscription order favours it. Plan caveat #4.)
3. Walk into Elder again -> plays `DlgEntry_Elder_0` ("Brave tamer..." + "Take this Wolf..."). On dialog end, `Advance("main")` -> stage 1.
4. Walk into Wolf encounter, win battle -> `TryAdvanceOnBattleWin` matches `BattleWin` + `wolf` -> stage 2.
5. Walk into Elder -> plays `DlgEntry_Elder_2` ("You returned..." + "Seek the Treant..."). Stage advances to 3.
6. Walk into Cave (stage 3 `AreaEnter` target `cave`) -> not implemented in v1; stage 3 stays.
7. (Manual `SetStage("main", 4)` for testing) Battle Treant in BossRoom -> stage 5.
8. Walk into Elder -> plays `DlgEntry_Elder_5` ("The land is saved..."). Stage stays at 5 (`AdvanceOn = None`).

**Sample quest** (Bounty: Imp Trouble):

1. New Game -> Town. `QuestStore.GetStage("sample")` returns -1.
2. Walk into Scout NPC -> `StartQuestOnFirstContact` fires, `SetStage("sample", 0)`. Then `Npc` plays `DlgEntry_Scout_0` ("Imps are raiding..." + "Knock one down..."). On dialog end, `Advance("sample")` -> stage 1.
3. Walk into ImpEnemy (green sprite at (650, 500)) -> wild battle vs level-4 Imp. Win -> `TryAdvanceOnBattleWin` scans all quests, finds `SampleQuest`, matches `BattleWin` + `imp` -> stage 2 (past end). Sample quest complete.
4. Walk into Scout again -> no `DialogPerNpc` entry for stage 2 (out of range) -> falls back to `Lines` export ("Anything I can help with, traveler?").

## Build / test

```
dotnet build rpg-game.sln -c Debug   # 0 errors
dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj   # 25/25
```
