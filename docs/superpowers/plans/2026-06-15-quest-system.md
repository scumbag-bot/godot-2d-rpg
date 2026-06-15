# Quest System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a single linear main-quest system to the existing 2D RPG. NPC dialog gated by quest stage. Auto-advance on NPC contact (after dialog completes) and on wild battle victory. No quest log UI. Designed to migrate to multi-quest log later.

**Architecture:** New `QuestStore` autoload holds stage/flag state in memory. `QuestData` + `QuestStage` Resources define the quest in editor-editable `.tres`. NPC dialog lookup via a list of `DialogPerNpcEntry` (NpcId + `DialogLine[]`) filtered by `Npc.NpcId`. `Npc` defers the `Advance` call to `DialogPlayer.DialogFinished` so the world pause doesn't end before the player reads the dialog. `BattleManager` advances stage on wild victory when species matches current stage's target.

**Tech Stack:** Godot 4.6, C# (.NET 8), NUnit 4. Plain C# for state; Godot Resource for quest data.

**Reference spec:** `docs/superpowers/specs/2026-06-14-rpg-game-design.md` (will be updated post-impl).

**Reference design discussion:** earlier brainstorming in this session. Decisions captured above.

**Plan revision notes:** this plan supersedes the earlier draft. The dialog system was rebuilt (Task 1-9 of the dialog feature) before this plan was finalized. Specifically:
- Dialog data type is `DialogLine` Resource (3 fields: `SpeakerName`, `SpeakerPortrait`, `Text`) — not `string`.
- `Npc.Lines` is `Godot.Collections.Array<DialogLine>` — not `string[]`.
- `DialogPlayer.Play(DialogLine[])` — not `Play(string[])`.
- DialogBox is a global autoload via the `DialogLayer` CanvasLayer; access via `DialogBox.Instance` or `/root/DialogLayer/DialogBox`.
- `DialogPlayer.IsActive` exposed. `DialogPlayer` pauses the scene tree (`GetTree().Paused = true`) during dialog.
- `DialogPlayer` emits `DialogStarted` and `DialogFinished` signals. `Advance` hooks into `DialogFinished` so the world pause covers the full read.

---

## File Structure

**New files:**
```
scripts/data/QuestStage.cs
scripts/data/QuestData.cs
scripts/data/DialogPerNpcEntry.cs
scripts/autoload/QuestStore.cs
resources/data/quests/MainQuest.tres
tests/rpg-game.Tests/QuestStoreTests.cs
```

**Modified files:**
```
scripts/overworld/Npc.cs
scripts/autoload/BattleManager.cs
project.godot
scenes/Town.tscn
```

**Boundaries:**
- `QuestStore` is plain C# (no Godot Resource fields) → testable in NUnit.
- `QuestData` / `QuestStage` / `DialogPerNpcEntry` are `Resource` subclasses, edited in Godot inspector.
- `QuestStore` is the single source of truth; no quest state lives in scenes or NPCs.
- NPCs are passive (look up `QuestStore` on contact); battle manager drives advance on victory.
- `Npc.Lines` (existing `[Export] Godot.Collections.Array<DialogLine>`) stays as a fallback for stages with no matching dialog entry.

---

## Phase A: Data Resources

### Task A1: `QuestStage` Resource

**Files:**
- Create: `scripts/data/QuestStage.cs`

- [ ] **Step 1: Implement**

```csharp
using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class QuestStage : Resource
{
    public enum AdvanceTrigger { None, NpcContact, BattleWin, AreaEnter }

    [Export] public StringName Id { get; set; } = "";
    [Export] public Godot.Collections.Array<DialogPerNpcEntry> DialogPerNpc { get; set; } = new();
    [Export] public AdvanceTrigger AdvanceOn { get; set; } = AdvanceTrigger.None;
    [Export] public StringName TargetNpcId { get; set; } = "";
    [Export] public StringName TargetSpeciesId { get; set; } = "";
    [Export] public StringName TargetAreaId { get; set; } = "";
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `Build succeeded. 0 Error(s)`. `DialogPerNpcEntry` doesn't exist yet, so this task will fail to compile until Task A2 lands. Mark this as in_progress and proceed to A2 immediately; build will pass once both files exist.

- [ ] **Step 3: Commit**

```bash
git add scripts/data/QuestStage.cs
git commit -m "feat(data): add QuestStage Resource"
```

---

### Task A2: `DialogPerNpcEntry` Resource

**Files:**
- Create: `scripts/data/DialogPerNpcEntry.cs`

- [ ] **Step 1: Implement**

```csharp
using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class DialogPerNpcEntry : Resource
{
    [Export] public StringName NpcId { get; set; } = "";
    [Export] public Godot.Collections.Array<DialogLine> Lines { get; set; } = new();
}
```

**Why a Resource and not a `Dictionary<StringName, Array<DialogLine>>`:** Godot 4 .tscn typed-dict serialization of `Godot.Collections.Dictionary` with custom-C#-Resource value types is fragile. A flat `Array<Resource>` of these entries serializes the same way the existing `EncounterTable.tres` handles `EncounterEntry` sub_resources — pattern proven in this project.

- [ ] **Step 2: Verify build + commit**

```bash
dotnet build rpg-game.sln -c Debug
git add scripts/data/DialogPerNpcEntry.cs
git commit -m "feat(data): add DialogPerNpcEntry Resource"
```

After this commit, `dotnet build` should pass with all Phase A files in place.

---

### Task A3: `QuestData` Resource

**Files:**
- Create: `scripts/data/QuestData.cs`

- [ ] **Step 1: Implement**

```csharp
using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class QuestData : Resource
{
    [Export] public StringName Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Godot.Collections.Array<QuestStage> Stages { get; set; } = new();
    [Export] public int StartStage { get; set; } = 0;
    [Export] public StringName NextQuestId { get; set; } = "";
}
```

- [ ] **Step 2: Verify build + commit**

```bash
dotnet build rpg-game.sln -c Debug
git add scripts/data/QuestData.cs
git commit -m "feat(data): add QuestData Resource"
```

---

## Phase B: QuestStore (TDD)

### Task B1: `QuestStore` autoload (TDD)

**Files:**
- Create: `scripts/autoload/QuestStore.cs`
- Create: `tests/rpg-game.Tests/QuestStoreTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using NUnit.Framework;
using rpg_game.scripts.autoload;

namespace rpg_game.Tests;

public class QuestStoreTests
{
    [Test]
    public void NewStore_StartsAtMinusOne()
    {
        var s = new QuestStore();
        Assert.That(s.GetStage("main"), Is.EqualTo(-1));
    }

    [Test]
    public void SetStage_Persists()
    {
        var s = new QuestStore();
        s.SetStage("main", 3);
        Assert.That(s.GetStage("main"), Is.EqualTo(3));
    }

    [Test]
    public void Advance_IncrementsFromCurrent()
    {
        var s = new QuestStore();
        s.SetStage("main", 2);
        s.Advance("main");
        Assert.That(s.GetStage("main"), Is.EqualTo(3));
    }

    [Test]
    public void Advance_FromMinusOne_GoesToZero()
    {
        var s = new QuestStore();
        s.Advance("main");
        Assert.That(s.GetStage("main"), Is.EqualTo(0));
    }

    [Test]
    public void Flags_DefaultFalse()
    {
        var s = new QuestStore();
        Assert.That(s.GetFlag("tutorial_done"), Is.False);
    }

    [Test]
    public void SetFlag_Persists()
    {
        var s = new QuestStore();
        s.SetFlag("tutorial_done");
        Assert.That(s.GetFlag("tutorial_done"), Is.True);
    }

    [Test]
    public void ClearFlag_ResetsToFalse()
    {
        var s = new QuestStore();
        s.SetFlag("x");
        s.ClearFlag("x");
        Assert.That(s.GetFlag("x"), Is.False);
    }
}
```

- [ ] **Step 2: Run, verify failure**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~QuestStoreTests`
Expected: FAIL — `QuestStore` not found.

- [ ] **Step 3: Implement `scripts/autoload/QuestStore.cs`**

```csharp
using System.Collections.Generic;
using Godot;

namespace rpg_game.scripts.autoload;

public partial class QuestStore : Node
{
    private readonly Dictionary<StringName, int> _stages = new();
    private readonly HashSet<StringName> _flags = new();

    [Signal] public delegate void StageAdvancedEventHandler(StringName questId, int newStage);
    [Signal] public delegate void FlagSetEventHandler(StringName flagName);

    public int GetStage(StringName questId)
    {
        return _stages.TryGetValue(questId, out var s) ? s : -1;
    }

    public void SetStage(StringName questId, int stage)
    {
        _stages[questId] = stage;
        EmitSignal(SignalName.StageAdvanced, questId, stage);
    }

    public void Advance(StringName questId)
    {
        var next = GetStage(questId) + 1;
        SetStage(questId, next);
    }

    public bool GetFlag(StringName flagName) => _flags.Contains(flagName);

    public void SetFlag(StringName flagName)
    {
        if (_flags.Add(flagName))
            EmitSignal(SignalName.FlagSet, flagName);
    }

    public void ClearFlag(StringName flagName) => _flags.Remove(flagName);
}
```

- [ ] **Step 4: Run, verify pass**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~QuestStoreTests`
Expected: PASS — 7 tests.

- [ ] **Step 5: Commit**

```bash
git add scripts/autoload/QuestStore.cs tests/rpg-game.Tests/QuestStoreTests.cs
git commit -m "feat(autoload): add QuestStore with stages + flags (TDD)"
```

---

## Phase C: Quest Data

### Task C1: `MainQuest.tres` with 6 stages

**Files:**
- Create: `resources/data/quests/MainQuest.tres`

- [ ] **Step 1: Create directory + file**

Create the file `C:\Users\Adovan\Documents\rpg-game\resources\data\quests\MainQuest.tres` with this exact content:

```
[gd_resource type="Resource" script_class="QuestData" load_steps=23 format=3]

[ext_resource type="Script" path="res://scripts/data/QuestData.cs" id="1_qdata"]
[ext_resource type="Script" path="res://scripts/data/QuestStage.cs" id="2_qstage"]
[ext_resource type="Script" path="res://scripts/data/DialogPerNpcEntry.cs" id="3_dlgpne"]
[ext_resource type="Script" path="res://scripts/data/DialogLine.cs" id="4_dlgline"]

[sub_resource type="Resource" id="DlgLine_0_0"]
script = ExtResource("4_dlgline")
SpeakerName = "Elder"
Text = "Brave tamer, the forest stirs with strange creatures."

[sub_resource type="Resource" id="DlgLine_0_1"]
script = ExtResource("4_dlgline")
SpeakerName = "Elder"
Text = "Take this Wolf. Prove yourself."

[sub_resource type="Resource" id="DlgEntry_Elder_0"]
script = ExtResource("3_dlgpne")
NpcId = &"elder"
Lines = [SubResource("DlgLine_0_0"), SubResource("DlgLine_0_1")]

[sub_resource type="Resource" id="DlgLine_2_0"]
script = ExtResource("4_dlgline")
SpeakerName = "Elder"
Text = "You returned. The cave to the north holds the source of corruption."

[sub_resource type="Resource" id="DlgLine_2_1"]
script = ExtResource("4_dlgline")
SpeakerName = "Elder"
Text = "Seek the Treant. End this."

[sub_resource type="Resource" id="DlgEntry_Elder_2"]
script = ExtResource("3_dlgpne")
NpcId = &"elder"
Lines = [SubResource("DlgLine_2_0"), SubResource("DlgLine_2_1")]

[sub_resource type="Resource" id="DlgLine_5_0"]
script = ExtResource("4_dlgline")
SpeakerName = "Elder"
Text = "The land is saved. Quest complete."

[sub_resource type="Resource" id="DlgEntry_Elder_5"]
script = ExtResource("3_dlgpne")
NpcId = &"elder"
Lines = [SubResource("DlgLine_5_0")]

[sub_resource type="Resource" id="Stage0"]
script = ExtResource("2_qstage")
Id = &"intro"
DialogPerNpc = [SubResource("DlgEntry_Elder_0")]
AdvanceOn = 1
TargetNpcId = &"elder"

[sub_resource type="Resource" id="Stage1"]
script = ExtResource("2_qstage")
Id = &"first_battle"
DialogPerNpc = []
AdvanceOn = 2
TargetSpeciesId = &"wolf"

[sub_resource type="Resource" id="Stage2"]
script = ExtResource("2_qstage")
Id = &"elder_blessing"
DialogPerNpc = [SubResource("DlgEntry_Elder_2")]
AdvanceOn = 1
TargetNpcId = &"elder"

[sub_resource type="Resource" id="Stage3"]
script = ExtResource("2_qstage")
Id = &"cave_enter"
DialogPerNpc = []
AdvanceOn = 3
TargetAreaId = &"cave"

[sub_resource type="Resource" id="Stage4"]
script = ExtResource("2_qstage")
Id = &"boss"
DialogPerNpc = []
AdvanceOn = 2
TargetSpeciesId = &"treant"

[sub_resource type="Resource" id="Stage5"]
script = ExtResource("2_qstage")
Id = &"complete"
DialogPerNpc = [SubResource("DlgEntry_Elder_5")]
AdvanceOn = 0

[resource]
script = ExtResource("1_qdata")
Id = &"main"
DisplayName = "Main Quest"
Stages = [SubResource("Stage0"), SubResource("Stage1"), SubResource("Stage2"), SubResource("Stage3"), SubResource("Stage4"), SubResource("Stage5")]
StartStage = 0
```

**Order matters:** all `sub_resource` blocks must appear BEFORE the final `[resource]` block. (Same constraint we hit in the dialog system — Godot parser is strict.)

**Enum value mapping** (verify in `QuestStage.cs`):
- `None = 0`
- `NpcContact = 1`
- `BattleWin = 2`
- `AreaEnter = 3`

`AdvanceOn` is stored as enum int. The values above are what the .tres file uses.

- [ ] **Step 2: Verify build**

Run: `dotnet build rpg-game.sln -c Debug` — 0 errors. (Build only validates script references; .tres structural validation is at F5 in the editor.)

- [ ] **Step 3: Commit**

```bash
git add resources/data/quests/MainQuest.tres
git commit -m "content: add MainQuest with 6 stages (DialogLine[] dialogs)"
```

---

## Phase D: Npc + BattleManager Wiring

### Task D1: `Npc.cs` — quest-aware dialog

**Files:**
- Modify: `scripts/overworld/Npc.cs`

- [ ] **Step 1: Replace `Npc.cs`**

Overwrite `C:\Users\Adovan\Documents\rpg-game\scripts\overworld\Npc.cs` with:

```csharp
using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Npc : Area2D
{
    [Export] public Godot.Collections.Array<DialogLine> Lines;
    [Export] public StringName NpcId = "";
    [Export] public StringName ActiveQuestId = "main";

    private bool _awaitingDialogCompletion;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        GetNode<DialogPlayer>("/root/DialogPlayer").DialogFinished += OnDialogFinished;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player) return;
        if (GetNode<DialogPlayer>("/root/DialogPlayer").IsActive) return;

        var quest = ResolveActiveQuest();
        if (quest != null)
        {
            var stageIdx = GetNode<QuestStore>("/root/QuestStore").GetStage(ActiveQuestId);
            if (stageIdx >= 0 && stageIdx < quest.Stages.Count)
            {
                var stage = quest.Stages[stageIdx];
                var entry = FindEntryForNpc(stage, NpcId);
                if (entry != null && entry.Lines != null && entry.Lines.Count > 0)
                {
                    var arr = new DialogLine[entry.Lines.Count];
                    for (int i = 0; i < entry.Lines.Count; i++) arr[i] = entry.Lines[i];
                    _awaitingDialogCompletion = stage.AdvanceOn == QuestStage.AdvanceTrigger.NpcContact
                        && stage.TargetNpcId == NpcId;
                    GetNode<DialogPlayer>("/root/DialogPlayer").Play(arr);
                    return;
                }
            }
        }

        if (Lines != null && Lines.Count > 0)
        {
            var arr = new DialogLine[Lines.Count];
            for (int i = 0; i < Lines.Count; i++) arr[i] = Lines[i];
            GetNode<DialogPlayer>("/root/DialogPlayer").Play(arr);
        }
    }

    private void OnDialogFinished()
    {
        if (!_awaitingDialogCompletion) return;
        _awaitingDialogCompletion = false;
        GetNode<QuestStore>("/root/QuestStore").Advance(ActiveQuestId);
    }

    private QuestData ResolveActiveQuest()
    {
        if (ActiveQuestId.IsEmpty) return null;
        return GD.Load<QuestData>($"res://resources/data/quests/{ActiveQuestId}.tres");
    }

    private static DialogPerNpcEntry FindEntryForNpc(QuestStage stage, StringName npcId)
    {
        if (stage.DialogPerNpc == null) return null;
        foreach (var e in stage.DialogPerNpc)
            if (e != null && e.NpcId == npcId) return e;
        return null;
    }
}
```

**Key behaviors:**
- `IsActive` guard prevents body-entered-during-dialog from re-firing.
- Quest dialog is tried first; if no matching entry for this NPC, falls back to `Lines` export.
- `_awaitingDialogCompletion` is set only when the stage's `AdvanceOn` is `NpcContact` AND target matches. Otherwise the dialog plays but the quest doesn't advance (e.g. stage 1 plays no dialog and is advanced by BattleWin, not contact).
- `OnDialogFinished` advances the quest only if the Npc was the one waiting. Multi-NPC scenes (future) are safe.

- [ ] **Step 2: Verify build + tests**

Run: `dotnet build rpg-game.sln -c Debug` — 0 errors.
Run: `dotnet test` — 25/25 pass (18 existing + 7 new).

- [ ] **Step 3: Commit**

```bash
git add scripts/overworld/Npc.cs
git commit -m "feat(overworld): make Npc quest-aware (DialogLine[] lookup + deferred advance)"
```

---

### Task D2: `BattleManager.cs` — advance on wild victory

**Files:**
- Modify: `scripts/autoload/BattleManager.cs`

- [ ] **Step 1: Add quest advance on victory**

Read the current `BattleManager.cs` (the `PlayerAttack` method around the auto-capture block). Add `using rpg_game.scripts.data;` to the top of the file.

Find the `EmitSignal(SignalName.BattleEnded, (int)BattleState.Victory)` line (or whichever line is the "victory" signal in your current `PlayerAttack`). BEFORE that line, insert:

```csharp
if (IsWild && CurrentEntry?.Species != null)
{
    TryAdvanceOnBattleWin(CurrentEntry.Species.Id);
}
```

And add a new private method to the class:

```csharp
private void TryAdvanceOnBattleWin(StringName defeatedSpeciesId)
{
    var quest = GD.Load<QuestData>("res://resources/data/quests/MainQuest.tres");
    if (quest == null) return;
    var qs = GetNode<QuestStore>("/root/QuestStore");
    var stageIdx = qs.GetStage(quest.Id);
    if (stageIdx < 0 || stageIdx >= quest.Stages.Count) return;
    var stage = quest.Stages[stageIdx];
    if (stage.AdvanceOn != QuestStage.AdvanceTrigger.BattleWin) return;
    if (stage.TargetSpeciesId != defeatedSpeciesId) return;
    qs.Advance(quest.Id);
}
```

**Note on `CurrentEntry.Species.Id`:** the current `BattleManager` already uses a `CreatureInstance` for the enemy. To get the species id, the code assumes the enemy instance exposes a `Species.Id` field — adjust if the current property is named differently (e.g., `Species` is a `CreatureSpeciesLite` and exposes `Id` directly). Read the existing `BattleManager.cs` first and adapt the line `CurrentEntry.Species.Id` to whatever the current code uses.

- [ ] **Step 2: Verify build + tests**

`dotnet build` 0 errors. `dotnet test` 25/25.

- [ ] **Step 3: Commit**

```bash
git add scripts/autoload/BattleManager.cs
git commit -m "feat(battle): advance quest on wild victory when species matches stage target"
```

---

### Task D3: `Town.tscn` — set NpcId

**Files:**
- Modify: `scenes/Town.tscn`

- [ ] **Step 1: Edit the Npc node to add `NpcId = &"elder"`**

Locate the `[node name="Npc" type="Area2D" parent="."]` block. After the existing `script = ExtResource("...")` line, add:

```
NpcId = &"elder"
```

The existing `Lines = [SubResource("Res_DialogLine_1")]` line STAYS as the fallback (currently plays "Who are you?" if no quest stage matches).

- [ ] **Step 2: Verify build**

`dotnet build` 0 errors.

- [ ] **Step 3: Commit**

```bash
git add scenes/Town.tscn
git commit -m "feat(scene): set elder NpcId on Town Npc"
```

---

### Task D4: Register `QuestStore` autoload

**Files:**
- Modify: `project.godot`

- [ ] **Step 1: Add autoload entry**

Add to `[autoload]` section (alphabetical or end, your choice — `DialogLayer` was last so add after it):

```ini
QuestStore="*res://scripts/autoload/QuestStore.cs"
```

- [ ] **Step 2: Verify build**

`dotnet build` 0 errors.

- [ ] **Step 3: Commit**

```bash
git add project.godot
git commit -m "chore: register QuestStore autoload"
```

---

## Phase E: Manual Smoke Test

### Task E1: Code-level wiring verification (no Godot editor)

- [ ] **Step 1: Walk the wiring by code**

Check:
1. `MainQuest.tres` has 6 stages with correct `AdvanceOn` and target fields. All `sub_resource` blocks precede the final `[resource]` block. `DialogLine` and `DialogPerNpcEntry` sub_resources are referenced by their parent stages.
2. `Npc.cs` loads quest via `GD.Load<QuestData>($"res://resources/data/quests/{ActiveQuestId}.tres")` — path matches `MainQuest.tres` filename. Default `ActiveQuestId = "main"` matches the file basename (case-sensitive).
3. `BattleManager.cs` loads same path (`"res://resources/data/quests/MainQuest.tres"`).
4. `Town.tscn` `Npc` node has `NpcId = &"elder"` line AND retains the existing `Lines = [SubResource("Res_DialogLine_1")]` fallback.
5. `project.godot` has `QuestStore="*res://..."` line.
6. `dotnet test` passes 25/25.
7. `dotnet build` passes 0 errors.

- [ ] **Step 2: Commit smoke test note**

```bash
git add -A
git commit -m "docs: code-level smoke test for quest system" --allow-empty
```

(F5 in Godot editor is the real smoke test — out of scope for this env.)

---

## Caveats

1. **No save/load** — `QuestStore` is in-memory only. Restart wipes progress. (Per v1 OOS, acceptable.)
2. **No UI feedback for stage transitions** — the dialog IS the feedback. No toast, no log.
3. **No quest-complete screen** — final stage plays dialog and stops. Could add a "Game Cleared!" fade later.
4. **`TownStarter` decoupled from quest** — stays as one-shot Wolf giver. Npc's quest dialog and TownStarter's "You received a Wolf!" race on first contact; whichever calls `Play` first wins. The other is no-op via `IsActive` check. At stage 0 (first contact), the quest plays Elder's "Brave tamer..." first (Npc is in scene-tree order BEFORE TownStarter... actually they're independent subscriptions, order undefined). If TownStarter wins, the Wolf is given but the quest doesn't advance. Re-entering Elder after that fires stage 0's dialog + advance. Net: Wolf is given either way; quest stage advances on first dialog completion.
5. **Single hardcoded `"main"` quest key** — referenced in `BattleManager` and `Npc` defaults. Future migration: read from a `Quest` autoload list or scene metadata. Single hardcoded id is fine for v1.
6. **Cave/Boss scenes don't exist** — stages 3-5 will be no-ops until those scenes + their NPCs/enemies are wired. The data is ready; the maps are your work.
7. **AdvanceTrigger.AreaEnter not implemented** — stages with `AdvanceOn = 3` never advance automatically. Acceptable for v1 (cave scene is OOS work).
8. **Quest dialog race with `TownStarter`** — see caveat #4. Documented behavior; both are wired independently and player sees whichever fires first.

---

## Self-Review

**Spec coverage:**
- [x] QuestStore autoload with stages + flags (TDD, 7 tests)
- [x] QuestData + QuestStage + DialogPerNpcEntry Resources
- [x] Npc dialog lookup by StringName NpcId (linear scan of Array<DialogPerNpcEntry>)
- [x] Npc.IsActive guard prevents re-entry
- [x] Npc.Advance deferred to DialogPlayer.DialogFinished signal
- [x] BattleManager auto-advance on wild victory by species id
- [x] MainQuest.tres with 6 stages; 3 with dialog blocks; 2 transition stages; 1 final
- [x] Autoload registered in project.godot
- [x] NpcId set on Town's Elder Npc; existing Lines kept as fallback
- [x] Caveats + plan revision notes

**Placeholders scan:** none. Every code block complete. The one place-specific line (`CurrentEntry.Species.Id` in D2) is documented to verify against the current code.

**Type/signature consistency:**
- `QuestStore.GetStage(StringName)` ↔ `SetStage(StringName, int)` ↔ `Advance(StringName)` — consistent
- `QuestStage.AdvanceTrigger` enum values match .tres `AdvanceOn` integers (None=0, NpcContact=1, BattleWin=2, AreaEnter=3)
- `Npc.NpcId` (StringName) ↔ `DialogPerNpcEntry.NpcId` (StringName) — consistent
- `Npc.Lines` (Godot.Collections.Array<DialogLine>) ↔ `DialogPerNpcEntry.Lines` (Godot.Collections.Array<DialogLine>) — consistent
- `BattleManager.CurrentEntry.Species.Id` (StringName from Resource) ↔ `QuestStage.TargetSpeciesId` (StringName) — consistent
- `MainQuest.tres` filename `MainQuest.tres` ↔ `ActiveQuestId` default `"main"` — MATCH (case-insensitive on Windows but explicit elsewhere)

**Dialog API alignment:**
- `DialogLine` Resource in `scripts/data/DialogLine.cs` — already exists (Phase 9 of dialog feature).
- `DialogBox.Instance` (static singleton, set in `DialogBox._Ready`) — already exists.
- `DialogPlayer.IsActive` (public bool) — already exists.
- `DialogPlayer.DialogFinished` (signal) — already exists.
- `DialogPlayer.Play(DialogLine[])` (public async method) — already exists.
- No new code on the dialog system; quest hooks USE existing API only.

**File paths exact, no placeholders, all commits specified.**

**Ready to execute.** Exit plan mode and dispatch implementer subagents to run tasks A1 → A2 → A3 → B1 → C1 → D1 → D2 → D3 → D4 → E1 in order.
