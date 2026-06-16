# Quest Authoring Guide

How to add or modify quests in this game. Aimed at designers + the future you.

## Mental model

A **quest** = ordered list of **stages**. `QuestStore` (autoload) tracks the current stage index per quest by `StringName` id.

| Stage value | Meaning |
|---|---|
| `-1` | Quest never started |
| `0..N-1` | Currently on stage N |
| `>= N` (overshoot) | Terminal — no consumer |

Each stage decides:
1. **Who talks to which NPC and what they say** (`DialogPerNpc` — optional per stage).
2. **What action advances the stage** (`AdvanceOn` + target field).

A stage with no `DialogPerNpc` entry for an NPC = NPC plays its `Lines` fallback. A stage that nobody can advance = quest stalls there until manually moved.

## Files involved

| File | What it owns |
|---|---|
| `resources/data/quests/MainQuest.tres` | The quest data (stages, dialog, triggers). |
| `scripts/data/QuestPaths.cs` | The `const string Main` path. Hardcoded for v1 (only one quest). |
| `scripts/data/QuestData.cs` | Resource: `Id`, `DisplayName`, `Stages[]`, `StartStage`, `NextQuestId`. |
| `scripts/data/QuestStage.cs` | Resource: `Id`, `DialogPerNpc[]`, `AdvanceOn`, `TargetNpcId`/`TargetSpeciesId`/`TargetAreaId`. |
| `scripts/data/DialogPerNpcEntry.cs` | Resource: pairs `NpcId` with `DialogLine[]`. |
| `scripts/runtime/QuestState.cs` | Pure C# stage/flag store. Testable in NUnit. |
| `scripts/autoload/QuestStore.cs` | Node autoload wrapping `QuestState`. Exposes `StringName` API + signals. |
| `scripts/overworld/Npc.cs` | Reads current stage, plays matching `DialogPerNpcEntry`, deferred-advances on `DialogFinished`. |
| `scripts/overworld/TownStarter.cs` | Currently the only quest entry-trigger source (calls `SetStage("main", 0)`). |
| `scripts/autoload/BattleManager.cs` | `TryAdvanceOnBattleWin` — auto-advance on wild victory matching `TargetSpeciesId`. |

## Advance triggers

`AdvanceOn` is the enum `QuestStage.AdvanceTrigger`. Stored as int in `.tres`.

| Enum | Int | Target field | Triggered by |
|---|---|---|---|
| `None` | 0 | (none) | Stage never auto-advances. Use for terminal stages or manually-advanced stages. |
| `NpcContact` | 1 | `TargetNpcId` (StringName) | Player walks into an `Npc` whose `NpcId` matches AND the stage has a matching `DialogPerNpc` entry. Advance fires after the dialog finishes. |
| `BattleWin` | 2 | `TargetSpeciesId` (StringName) | Player wins a wild battle (`IsWild=true`) against a creature whose `Species.Id` matches. |
| `AreaEnter` | 3 | `TargetAreaId` (StringName) | **NOT IMPLEMENTED in v1.** Stage with this trigger never auto-advances. Plan was for a `WarpZone` or area marker to fire it; left out of scope. |

## How to add a new stage to MainQuest

Goal: insert a new "find the hidden grove" stage between current stage 2 (`elder_blessing`) and stage 3 (`cave_enter`). Player must enter area `grove` (won't actually fire — see AreaEnter caveat above). Skip the dialog.

Edit `resources/data/quests/MainQuest.tres`:

1. **Add a new `[sub_resource type="Resource" id="Stage25"]` block** above the final `[resource]` block. Use the next-available numeric id; here `Stage25` to indicate "between 2 and 3":

```
[sub_resource type="Resource" id="Stage25"]
script = ExtResource("2_qstage")
Id = &"find_grove"
DialogPerNpc = []
AdvanceOn = 3
TargetAreaId = &"grove"
```

2. **Update the root `Stages` array** to insert the new stage between Stage2 and Stage3:

```
Stages = [SubResource("Stage0"), SubResource("Stage1"), SubResource("Stage2"), SubResource("Stage25"), SubResource("Stage3"), SubResource("Stage4"), SubResource("Stage5")]
```

3. **Bump `load_steps`** on the header (line 1) by `+1` (was `19`, now `20`):

```
[gd_resource type="Resource" script_class="QuestData" load_steps=20 format=3]
```

4. Open the project in Godot. The editor will scan + reimport. If you broke the `.tres` syntax, Godot's resource inspector will refuse to load with an explicit error message.

## How to add a new quest entirely

V1 is wired for a single hardcoded quest path (`QuestPaths.Main`). To add a second quest, you'd need to:

1. Create `resources/data/quests/SideQuest1.tres` following the same structure as `MainQuest.tres` (new `Id = &"side1"`, new stages).
2. Add a const in `scripts/data/QuestPaths.cs`:
   ```csharp
   public const string Side1 = "res://resources/data/quests/SideQuest1.tres";
   ```
3. Decide who triggers which quest:
   - `Npc` currently hardcodes `MainQuestId = "main"` at `Npc.cs:11`. To make NPCs quest-pickable, restore the `[Export] public StringName ActiveQuestId` field (was dropped in commit `82acfac`) and load the matching quest path from a per-quest map.
   - `BattleManager.TryAdvanceOnBattleWin` only checks `MainQuest`. Either expand it to iterate all quests, or have each quest register its battle triggers somewhere.

This is **deferred to v1.1+**. Plan caveat #5: "Single hardcoded `main` quest key is fine for v1."

## How to add a new dialog line to an existing stage

1. Add a `DlgLine_X_N` `[sub_resource]` block. Increment `load_steps` by 1.
2. Add the new sub_resource id to the existing `DlgEntry_*` block's `Lines = [...]` array, in the order you want lines played.

Example — adding a third line to stage 2's Elder dialog:

```
[sub_resource type="Resource" id="DlgLine_2_2"]
script = ExtResource("4_dlgline")
SpeakerName = "Elder"
Text = "Beware the cave's whispers."

[sub_resource type="Resource" id="DlgEntry_Elder_2"]
script = ExtResource("3_dlgpne")
NpcId = &"elder"
Lines = [SubResource("DlgLine_2_0"), SubResource("DlgLine_2_1"), SubResource("DlgLine_2_2")]
```

## How to wire an NPC into a quest

1. **Pick an NpcId.** StringName, lowercase, snake_case. E.g. `&"blacksmith"`, `&"forest_guide"`. Must match what your quest stage references in `TargetNpcId` AND `DialogPerNpcEntry.NpcId`.

2. **In the scene's `.tscn`**, edit the `Npc` Area2D node. Add this line after `script = ExtResource("...")` and before `Lines = ...`:

```
NpcId = &"blacksmith"
```

(Example: see `scenes/Town.tscn:1934` for the Elder NPC.)

3. **In `MainQuest.tres`**, add a `DlgEntry_*` sub_resource with that `NpcId`:

```
[sub_resource type="Resource" id="DlgEntry_Blacksmith_3"]
script = ExtResource("3_dlgpne")
NpcId = &"blacksmith"
Lines = [SubResource("DlgLine_3_0"), SubResource("DlgLine_3_1")]
```

4. Reference that sub_resource in the relevant stage's `DialogPerNpc = [...]` array.

5. If you want walking into this NPC to *advance* the stage (not just play dialog), set `AdvanceOn = 1` and `TargetNpcId = &"blacksmith"` on the stage.

## How to wire a battle-win trigger

1. **Pick a target species.** Look in `resources/data/creatures/*.tres` for the `Id` field. E.g. `Wolf.tres:7` has `Id = &"wolf"`, `Treant.tres:7` has `Id = &"treant"`.

2. **On the stage, set:**

```
AdvanceOn = 2
TargetSpeciesId = &"wolf"
```

3. **Ensure that species can spawn in a wild encounter** the player can reach at this stage. Wild encounters are defined in `resources/data/encounters/*.tres` and placed via `Enemy` nodes in scenes (e.g. `scenes/Town.tscn` `Enemy` node referencing `SingleWolf.tres`).

4. The advance fires automatically when the player wins. Capture happens regardless of quest state (auto-capture is unrelated to `TryAdvanceOnBattleWin`).

## How to start a quest (the entry-trigger problem)

`QuestStore.GetStage("main")` starts at `-1`. Nothing in `Npc.OnBodyEntered` advances `-1 → 0` — `Npc` short-circuits to its `Lines` fallback at stage `-1`. So **a quest must be explicitly started from somewhere**.

Current production wiring: `scripts/overworld/TownStarter.cs:30` calls `GetNode<QuestStore>("/root/QuestStore").SetStage("main", 0);` immediately after gifting the player the starter Wolf. That's the only entry trigger.

If you want a new quest (or a new way to start MainQuest) to be entry-triggered:
- Find a one-time event (e.g. scene `_Ready`, an `Area2D` body-enter, a button press).
- Call `GetNode<QuestStore>("/root/QuestStore").SetStage("<quest_id>", 0);` once.
- Guard against double-fire (TownStarter uses a `_given` bool field).

## How to programmatically check / set quest state

In any GDScript-equivalent C# code (anywhere that can `GetNode` an autoload):

```csharp
var qs = GetNode<QuestStore>("/root/QuestStore");

// Read
int stage = qs.GetStage("main");        // returns -1 if quest hasn't started
bool tutorialDone = qs.GetFlag("tutorial_done");

// Write
qs.SetStage("main", 3);                  // jumps to stage 3, emits StageAdvanced if changed
qs.Advance("main");                      // current + 1
qs.SetFlag("met_elder");                 // emits FlagSet if newly set
qs.ClearFlag("met_elder");

// Subscribe
qs.StageAdvanced += (questId, stage) => GD.Print($"{questId} now at {stage}");
qs.FlagSet += (flag) => GD.Print($"Flag {flag} set");
```

**Quest state lives only in memory.** No save/load in v1 — restarting wipes progress.

## Testing a new quest

Unit-test the data model on `QuestState` (pure C#) — see `tests/rpg-game.Tests/QuestStateTests.cs`. Stage transitions, flags, and the change-detection signal gate are all testable without Godot.

In the Godot editor (F5):

| Check | How |
|---|---|
| Quest starts | Talk to TownStarter NPC → `_mcp_game_helper` log should show `StageAdvanced(main, 0)` (or use `GD.Print` in your trigger). |
| Stage dialog plays | Walk into target NPC at the stage. If their `DialogPerNpc` entry is set up, you'll see those lines instead of the `Lines` fallback. |
| Stage advances on dialog finish | After the last line of the quest dialog, the stage should bump. Re-enter the same NPC; you should now get the *next* stage's dialog (or `Lines` fallback if next stage has none). |
| Stage advances on wild win | Defeat the `TargetSpeciesId` in a wild battle. Check via `editor_state` / `game_eval` or a `GD.Print` hook in `QuestStore.SetStage`. |
| Stage stalls correctly | If `AdvanceOn = None`, nothing should bump the stage. |
| AreaEnter does nothing | Stages with `AdvanceOn = 3` will *not* advance in v1 — confirmed by code review. Don't rely on this trigger. |

## Common gotchas

- **`StringName` literals.** Use `&"foo"` in `.tres` files. Quote-only `"foo"` is a `string` and will silently fail comparisons with `StringName` fields.
- **`load_steps` count.** Equals `ext_resource count + sub_resource count + 1` (for the root `[resource]` block). Mismatch produces an editor warning but won't fail. Always bump when you add/remove sub_resources.
- **Sub-resource ordering.** All `[sub_resource]` blocks must appear BEFORE the final `[resource]` block. Godot's parser is strict.
- **DialogPerNpc array of empty.** Use `DialogPerNpc = []` (empty) when a stage has no NPC dialog. Don't omit the field; the editor might complain.
- **Hardcoded stuff:** `Npc.cs:11` hardcodes `"main"` as the quest id, and `QuestPaths.Main` hardcodes the .tres path. Both are intentional v1 scope. Multi-quest support requires touching both.
- **No persistence.** `QuestStore` is in-memory only. Closing the game wipes all quest progress. AGENTS.md confirms save/load is out of v1 scope.
- **Quest dialog beats Lines fallback.** If a stage has a matching `DialogPerNpcEntry` for an NPC, `Npc.cs` uses that and *ignores* the `Lines` export for that visit. To always show the same line regardless of stage, leave `DialogPerNpc` empty for that NPC at every stage.

## Reference: existing MainQuest structure

```
Stage 0  intro             AdvanceOn=NpcContact(1)  TargetNpcId=elder    DlgEntry_Elder_0  (2 lines)
Stage 1  first_battle      AdvanceOn=BattleWin(2)   TargetSpeciesId=wolf  no dialog
Stage 2  elder_blessing    AdvanceOn=NpcContact(1)  TargetNpcId=elder    DlgEntry_Elder_2  (2 lines)
Stage 3  cave_enter        AdvanceOn=AreaEnter(3)   TargetAreaId=cave    no dialog          [STALLS — AreaEnter unimplemented]
Stage 4  boss              AdvanceOn=BattleWin(2)   TargetSpeciesId=treant  no dialog
Stage 5  complete          AdvanceOn=None(0)        (terminal)             DlgEntry_Elder_5  (1 line)
```

Path: `res://resources/data/quests/MainQuest.tres`. Const at `scripts/data/QuestPaths.cs:Main`.
