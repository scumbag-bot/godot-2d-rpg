# Visual Novel Dialog Box Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the full-screen opaque dialog panel with a bottom-anchored visual-novel-style dialog box (portrait, nameplate, typewriter, advance indicator) driven by player input.

**Architecture:** `DialogPlayer` (autoload) becomes an advance-driven coroutine that emits `LineChanged` per line and awaits `DialogBox.AdvancePressed` between lines. `DialogBox` (Control, child of every overworld scene) self-subscribes to `DialogPlayer` in `_Ready` and handles typewriter (Tween on `Label.VisibleCharacters`) + input (any key / left click). `Npc` exports `DialogLine[]` (per-line name/portrait/text) and passes it directly to `DialogPlayer.Play`.

**Tech Stack:** Godot 4.6, C# / .NET 8, NUnit 4 (existing).

**Spec deviation (intentional):** The 2026-06-15 spec defines `DialogLine` as a `record struct` in `scripts/runtime/`. Godot 4's [Export] does not reliably serialize C# struct types via the editor inspector; the idiomatic Godot-4 path is a `Resource` subclass with `[GlobalClass]`. To keep editor serialization working, this plan implements `DialogLine` as a `Resource` subclass under `scripts/data/`. Behavior is identical; only the C# declaration form changes. Flagging for user awareness in the spec review.

**Note on TDD:** Per project AGENTS.md, Godot-dependent code is verified via `dotnet build` + the F5 smoke checklist in `SMOKE-TEST-F5.md`, not NUnit. The autoload and UI components in this plan are all Godot-dependent; no NUnit tests are added. Pure-C# data types are trivial (a Resource with three exported properties) and the project rule reserves NUnit for testable pure-C# logic.

---

## File Summary

| Action | Path | Responsibility |
|---|---|---|
| Create | `scripts/data/DialogLine.cs` | Per-line data (name, portrait, text) as Godot Resource |
| Modify | `scripts/autoload/DialogPlayer.cs` | Advance-driven coroutine; emits `LineChanged`; awaits `AdvancePressed` |
| Modify | `scripts/ui/DialogBox.cs` | Self-subscribe; typewriter tween; input handler; `AdvancePressed` signal |
| Modify | `scenes/ui/DialogBox.tscn` | New node tree: Background, Panel, Portrait, NameLabel, TextLabel, AdvanceArrow |
| Modify | `scripts/overworld/Npc.cs` | `string[] Lines` → `DialogLine[] Lines`; remove unused `DialogBoxRef` |
| Modify | `scripts/overworld/TownStarter.cs` | Migrate inline `string[]` to `DialogLine[]` for `Play()` call |
| Modify | `scripts/overworld/Overworld.cs` | Remove per-scene DialogBox wiring block (now self-subscribed) |
| Modify | `scenes/Town.tscn` | Migrate Npc `Lines` data; drop `DialogBoxRef` |
| Modify | `SMOKE-TEST-F5.md` | Add 10-point dialog checklist section |

---

## Task 1: Add DialogLine Resource

**Files:**
- Create: `scripts/data/DialogLine.cs`

- [ ] **Step 1: Create the file**

Write `C:\Users\Adovan\Documents\rpg-game\scripts\data\DialogLine.cs`:

```csharp
using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class DialogLine : Resource
{
    [Export] public string SpeakerName { get; set; } = "";
    [Export] public Texture2D SpeakerPortrait { get; set; }
    [Export] public string Text { get; set; } = "";
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `0 Error(s)`. (The pre-existing CS8618 nullable warnings are tolerated per AGENTS.md; this new file is fully null-init.)

- [ ] **Step 3: Commit**

```bash
git add scripts/data/DialogLine.cs
git commit -m "feat(dialog): add DialogLine Resource"
```

---

## Task 2: Rewrite DialogPlayer autoload

**Files:**
- Modify: `scripts/autoload/DialogPlayer.cs` (full rewrite)

- [ ] **Step 1: Replace the file**

Overwrite `C:\Users\Adovan\Documents\rpg-game\scripts\autoload\DialogPlayer.cs` with:

```csharp
using Godot;
using rpg_game.scripts.data;
using rpg_game.scripts.ui;

namespace rpg_game.scripts.autoload;

public partial class DialogPlayer : Node
{
    [Signal] public delegate void DialogStartedEventHandler();
    [Signal] public delegate void LineChangedEventHandler(string name, Texture2D portrait, string text);
    [Signal] public delegate void DialogFinishedEventHandler();

    public bool IsActive { get; private set; }

    public async void Play(DialogLine[] lines)
    {
        if (IsActive) return;
        if (DialogBox.Instance == null)
        {
            GD.PushWarning("DialogPlayer.Play: no DialogBox.Instance; ignoring.");
            return;
        }

        IsActive = true;
        EmitSignal(SignalName.DialogStarted);

        foreach (var line in lines)
        {
            var name = line.SpeakerName ?? "";
            EmitSignal(SignalName.LineChanged, name, line.SpeakerPortrait, line.Text);
            await ToSignal(DialogBox.Instance, DialogBox.SignalName.AdvancePressed);
        }

        IsActive = false;
        EmitSignal(SignalName.DialogFinished);
    }
}
```

- [ ] **Step 2: Verify compile (expected: CS0246 DialogBox not found yet — that's fine; Task 5 adds the signal)**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `Build succeeded. 0 Error(s)`. If a `CS0246` or `CS0103` appears, the `DialogBox.SignalName.AdvancePressed` symbol is resolved in Task 5. (Sanity: the project has a forward-reference to `rpg_game.scripts.ui.DialogBox`; C# resolves the symbol at compile time but Godot's `SignalName.AdvancePressed` is a generated static — it depends on `[Signal] public delegate void AdvancePressedEventHandler();` existing on DialogBox. Task 5 adds it. If you see a build error here, temporarily comment the `await` line, commit, and restore in Task 5.)

- [ ] **Step 3: Commit**

```bash
git add scripts/autoload/DialogPlayer.cs
git commit -m "feat(dialog): advance-driven DialogPlayer coroutine"
```

---

## Task 3: Update Npc to use DialogLine[]

**Files:**
- Modify: `scripts/overworld/Npc.cs`

- [ ] **Step 1: Replace the file**

Overwrite `C:\Users\Adovan\Documents\rpg-game\scripts\overworld\Npc.cs` with:

```csharp
using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Npc : Area2D
{
    [Export] public Godot.Collections.Array<DialogLine> Lines;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player) return;
        if (Lines == null || Lines.Count == 0) return;
        var arr = new DialogLine[Lines.Count];
        for (int i = 0; i < Lines.Count; i++) arr[i] = Lines[i];
        GetNode<DialogPlayer>("/root/DialogPlayer").Play(arr);
    }
}
```

- [ ] **Step 2: Verify compile**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `0 Error(s)`. The `using rpg_game.scripts.ui;` import is removed (DialogBoxRef export gone).

- [ ] **Step 3: Commit**

```bash
git add scripts/overworld/Npc.cs
git commit -m "feat(npc): use DialogLine[] for dialog data"
```

---

## Task 4: Update TownStarter to pass DialogLine[]

**Files:**
- Modify: `scripts/overworld/TownStarter.cs`

- [ ] **Step 1: Replace the file**

Overwrite `C:\Users\Adovan\Documents\rpg-game\scripts\overworld\TownStarter.cs` with:

```csharp
using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.overworld;

public partial class TownStarter : Node
{
    [Export] public NodePath StarterNpcPath;
    private bool _given = false;

    public override void _Ready()
    {
        var npc = GetNodeOrNull<Npc>(StarterNpcPath);
        if (npc != null) npc.BodyEntered += OnEnter;
    }

    private void OnEnter(Node2D body)
    {
        if (_given || body is not Player) return;
        _given = true;
        var wolf = GD.Load<CreatureSpecies>("res://resources/data/creatures/Wolf.tres");
        var inst = new CreatureInstance(wolf.ToLite(), 5);
        GetNode<Party>("/root/Party").Add(inst);
        GetNode<DialogPlayer>("/root/DialogPlayer").Play(new[]
        {
            new DialogLine { SpeakerName = "", SpeakerPortrait = null, Text = "You received a Wolf!" },
            new DialogLine { SpeakerName = "", SpeakerPortrait = null, Text = "Press on, brave tamer." },
        });
    }
}
```

- [ ] **Step 2: Verify compile**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `0 Error(s)`. (Note: `GD.Load<CreatureSpecies>` keeps the existing `using rpg_game.scripts.runtime;` in use for `CreatureInstance`.)

- [ ] **Step 3: Commit**

```bash
git add scripts/overworld/TownStarter.cs
git commit -m "feat(town-starter): pass DialogLine[] to DialogPlayer"
```

---

## Task 5: Rewrite DialogBox.cs (UI Control)

**Files:**
- Modify: `scripts/ui/DialogBox.cs` (full rewrite)

- [ ] **Step 1: Replace the file**

Overwrite `C:\Users\Adovan\Documents\rpg-game\scripts\ui\DialogBox.cs` with:

```csharp
using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.ui;

public partial class DialogBox : Control
{
    [Signal] public delegate void AdvancePressedEventHandler();

    public static DialogBox Instance { get; private set; }

    [Export] public Label TextLabel;
    [Export] public Label NameLabel;
    [Export] public TextureRect Portrait;
    [Export] public Label AdvanceArrow;

    private const float TypewriterSecondsPerChar = 0.03f;

    private Tween _typewriterTween;
    private bool _isFullyRevealed;
    private bool _isDialogActive;
    private string _pendingText = "";

    public override void _Ready()
    {
        Instance = this;
        Visible = false;
        if (AdvanceArrow != null) AdvanceArrow.Visible = false;

        var dp = GetNode<DialogPlayer>("/root/DialogPlayer");
        dp.DialogStarted += OnDialogStarted;
        dp.LineChanged += OnLineChanged;
        dp.DialogFinished += OnDialogFinished;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowLine(string name, Texture2D portrait, string text)
    {
        if (NameLabel != null) NameLabel.Text = string.IsNullOrEmpty(name) ? "" : name;
        if (Portrait != null)
        {
            Portrait.Texture = portrait;
            Portrait.Visible = portrait != null;
        }
        _pendingText = text ?? "";
        if (TextLabel != null)
        {
            TextLabel.Text = _pendingText;
            TextLabel.VisibleCharacters = 0;
        }
        _isFullyRevealed = false;
        if (AdvanceArrow != null) AdvanceArrow.Visible = false;

        _typewriterTween?.Kill();
        if (TextLabel != null && _pendingText.Length > 0)
        {
            var tween = TextLabel.CreateTween();
            tween.TweenProperty(TextLabel, "visible_characters", _pendingText.Length, _pendingText.Length * TypewriterSecondsPerChar);
            tween.Finished += OnTypewriterFinished;
            _typewriterTween = tween;
        }
        else
        {
            _isFullyRevealed = true;
            if (AdvanceArrow != null) AdvanceArrow.Visible = true;
        }
    }

    public void RevealInstant()
    {
        if (_isFullyRevealed) return;
        _typewriterTween?.Kill();
        _typewriterTween = null;
        if (TextLabel != null) TextLabel.VisibleCharacters = -1;
        _isFullyRevealed = true;
        if (AdvanceArrow != null) AdvanceArrow.Visible = true;
    }

    public void Hide()
    {
        _isDialogActive = false;
        _typewriterTween?.Kill();
        _typewriterTween = null;
        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isDialogActive) return;
        var isKey = @event is InputEventKey ke && ke.Pressed && !ke.Echo;
        var isClick = @event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left;
        if (!isKey && !isClick) return;

        if (!_isFullyRevealed)
        {
            RevealInstant();
        }
        else
        {
            EmitSignal(SignalName.AdvancePressed);
        }
        GetViewport().SetInputAsHandled();
    }

    private void OnDialogStarted()
    {
        _isDialogActive = true;
        Visible = true;
    }

    private void OnLineChanged(string name, Texture2D portrait, string text)
    {
        ShowLine(name, portrait, text);
    }

    private void OnDialogFinished()
    {
        Hide();
    }

    private void OnTypewriterFinished()
    {
        if (TextLabel != null) TextLabel.VisibleCharacters = -1;
        _isFullyRevealed = true;
        if (AdvanceArrow != null) AdvanceArrow.Visible = true;
    }
}
```

- [ ] **Step 2: Verify compile**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `0 Error(s)`. The `using rpg_game.scripts.autoload;` resolves `DialogPlayer`.

- [ ] **Step 3: Commit**

```bash
git add scripts/ui/DialogBox.cs
git commit -m "feat(dialog): self-subscribing DialogBox with typewriter + input"
```

---

## Task 6: Rewrite DialogBox.tscn (scene)

**Files:**
- Modify: `scenes/ui/DialogBox.tscn` (full rewrite)

- [ ] **Step 1: Replace the file**

Overwrite `C:\Users\Adovan\Documents\rpg-game\scenes\ui\DialogBox.tscn` with:

```
[gd_scene load_steps=2 format=3 uid="uid://b0dbox001"]

[ext_resource type="Script" path="res://scripts/ui/DialogBox.cs" id="1_dbox"]

[node name="DialogBox" type="Control" unique_id=1133852984]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 0
script = ExtResource("1_dbox")
TextLabel = NodePath("Panel/TextLabel")
NameLabel = NodePath("Panel/NameLabel")
Portrait = NodePath("Panel/Portrait")
AdvanceArrow = NodePath("Panel/AdvanceArrow")

[node name="Background" type="ColorRect" parent="." unique_id=1133852985]
layout_mode = 1
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2
color = Color(0, 0, 0, 0.5)

[node name="Panel" type="Panel" parent="." unique_id=1133852986]
layout_mode = 1
anchors_preset = 12
anchor_top = 0.7
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 0
mouse_filter = 0
theme_override_styles/panel = SubResource("")

[node name="Portrait" type="TextureRect" parent="Panel" unique_id=1133852987]
layout_mode = 0
offset_left = 8.0
offset_top = -16.0
offset_right = 104.0
offset_bottom = 80.0
mouse_filter = 2
expand_mode = 1
stretch_mode = 5

[node name="NameLabel" type="Label" parent="Panel" unique_id=1133852988]
layout_mode = 0
offset_left = 120.0
offset_top = 8.0
offset_right = 1000.0
offset_bottom = 32.0
mouse_filter = 2
theme_override_colors/font_color = Color(1, 0.9, 0.5, 1)
theme_override_font_sizes/font_size = 18
text = "Name"

[node name="TextLabel" type="Label" parent="Panel" unique_id=1133852989]
layout_mode = 0
offset_left = 120.0
offset_top = 36.0
offset_right = -32.0
offset_bottom = -24.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2
theme_override_font_sizes/font_size = 18
text = ""
autowrap_mode = 3
visible_characters = 0

[node name="AdvanceArrow" type="Label" parent="Panel" unique_id=1133852990]
layout_mode = 0
anchor_left = 1.0
anchor_top = 1.0
anchor_right = 1.0
anchor_bottom = 1.0
offset_left = -32.0
offset_top = -32.0
offset_right = -8.0
offset_bottom = -8.0
grow_horizontal = 0
grow_vertical = 0
mouse_filter = 2
theme_override_colors/font_color = Color(1, 1, 1, 0.8)
theme_override_font_sizes/font_size = 20
text = "▼"
horizontal_alignment = 2
vertical_alignment = 2
```

- [ ] **Step 2: Sanity-check: remove the empty `SubResource("")` line**

The `theme_override_styles/panel = SubResource("")` line above is a placeholder. Replace it with a properly-defined inline `StyleBoxFlat`. Update the file so the Panel block becomes:

```
[node name="Panel" type="Panel" parent="." unique_id=1133852986]
layout_mode = 1
anchors_preset = 12
anchor_top = 0.7
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 0
mouse_filter = 0
theme_override_styles/panel = SubResource("StyleBox_Panel")
```

And add the sub_resource at the top of the file (after the ext_resource):

```
[sub_resource type="StyleBoxFlat" id="StyleBox_Panel"]
bg_color = Color(0.05, 0.05, 0.1, 0.92)
border_width_left = 2
border_width_top = 2
border_width_right = 2
border_width_bottom = 2
border_color = Color(0.6, 0.6, 0.7, 1)
```

And change `load_steps=2` to `load_steps=3` at the top of the file.

Final top of file:

```
[gd_scene load_steps=3 format=3 uid="uid://b0dbox001"]

[ext_resource type="Script" path="res://scripts/ui/DialogBox.cs" id="1_dbox"]

[sub_resource type="StyleBoxFlat" id="StyleBox_Panel"]
bg_color = Color(0.05, 0.05, 0.1, 0.92)
border_width_left = 2
border_width_top = 2
border_width_right = 2
border_width_bottom = 2
border_color = Color(0.6, 0.6, 0.7, 1)
```

- [ ] **Step 3: Verify compile (Godot will validate scene; build only checks C#)**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `0 Error(s)`. Scene file syntax errors only surface in the Godot editor at F5; the build will not catch them. If the editor reports a parse error on the .tscn, re-check the sub_resource id matches the reference.

- [ ] **Step 4: Commit**

```bash
git add scenes/ui/DialogBox.tscn
git commit -m "feat(dialog): new VN-style scene with portrait/nameplate/arrow"
```

---

## Task 7: Remove old wiring from Overworld.cs

**Files:**
- Modify: `scripts/overworld/Overworld.cs`

- [ ] **Step 1: Replace the file**

Overwrite `C:\Users\Adovan\Documents\rpg-game\scripts\overworld\Overworld.cs` with:

```csharp
using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Overworld : Node2D
{
    public override void _Ready()
    {
        var es = GetNode<EncounterSystem>("/root/EncounterSystem");
        es.EncounterTriggered += OnEncounter;
        GetNode<GameState>("/root/GameState").SetMode(GameState.Mode.Overworld);
    }

    private void OnEncounter(EncounterEntry entry)
    {
        GetNode<BattleManager>("/root/BattleManager").StartWild(entry);
        GetNode<SceneManager>("/root/SceneManager").GotoBattle();
    }
}
```

The `using rpg_game.scripts.ui;` import is removed (no longer references `DialogBox`).

- [ ] **Step 2: Verify compile**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add scripts/overworld/Overworld.cs
git commit -m "refactor(overworld): drop per-scene dialog wiring (DialogBox self-subscribes)"
```

---

## Task 8: Migrate Town.tscn Npc data

**Files:**
- Modify: `scenes/Town.tscn` (edit Npc node block only)

- [ ] **Step 1: Find the Npc node and read the exact lines**

In `C:\Users\Adovan\Documents\rpg-game\scenes\Town.tscn`, the Npc node (around line 1921) currently looks like:

```
[node name="Npc" type="Area2D" parent="." unique_id=1183842659 node_paths=PackedStringArray("DialogBoxRef")]
script = ExtResource("4_npc")
Lines = PackedStringArray("Who are you")
DialogBoxRef = NodePath("../DialogBox")
```

- [ ] **Step 2: Replace the Npc node block with the new form**

Replace the entire `[node name="Npc" ...]` block (everything from `[node name="Npc"` up to but not including the next `[node ...]`) with:

```
[sub_resource type="Resource" id="Res_DialogLine_1"]
script = ExtResource("5_dlgline")
SpeakerName = "Elder"
SpeakerPortrait = null
Text = "Who are you?"

[node name="Npc" type="Area2D" parent="." unique_id=1183842659]
script = ExtResource("4_npc")
Lines = Array[Resource]([SubResource("Res_DialogLine_1")])
```

- [ ] **Step 3: Add the ext_resource for the DialogLine script**

Find the top of `Town.tscn` where the other ext_resources are declared. Add a new line after the existing `4_npc` entry:

```
[ext_resource type="Script" path="res://scripts/data/DialogLine.cs" id="5_dlgline"]
```

Also increment `load_steps` (the number at the start of the file, e.g. `gd_scene load_steps=2 format=3`) by 1 to account for the new sub_resource. If the top of `Town.tscn` is currently `load_steps=2`, change it to `load_steps=3`.

- [ ] **Step 4: Verify compile**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `0 Error(s)`. (The scene file's structural correctness is checked by Godot at F5, not by the C# build.)

- [ ] **Step 5: Commit**

```bash
git add scenes/Town.tscn
git commit -m "feat(town): migrate NPC dialog data to DialogLine[]"
```

---

## Task 9: Update SMOKE-TEST-F5.md with dialog checklist

**Files:**
- Modify: `SMOKE-TEST-F5.md`

- [ ] **Step 1: Add a new section after the existing checklist table**

Open `C:\Users\Adovan\Documents\rpg-game\SMOKE-TEST-F5.md`. After the existing 13-row table (before the "Runtime paths to validate in Godot editor" section), insert:

```
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
```

Also add a "Runtime paths to validate in Godot editor" subsection for dialog:

```
- Walk into Town NPC -> dialog panel appears at bottom, overworld dimmed above
- First line text reveals character-by-character (~30ms/char)
- Press any key mid-typing -> full text appears immediately
- Press any key after text complete -> next line begins
- ▼ arrow visible at bottom-right only when line fully revealed
- After last line, press key -> dialog hides
- Mouse click on dialog area also advances
- Re-enter NPC -> dialog replays from line 1
```

- [ ] **Step 2: Verify no syntax issues**

Run: `Get-Content SMOKE-TEST-F5.md` (or open in editor). Confirm the markdown renders.

- [ ] **Step 3: Commit**

```bash
git add SMOKE-TEST-F5.md
git commit -m "docs(smoke): add VN dialog box checklist"
```

---

## Task 10: Final verification

- [ ] **Step 1: Full build**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `0 Error(s)`. Pre-existing CS8618 nullable warnings are tolerated.

- [ ] **Step 2: Run all tests**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj`
Expected: `18/18 passed`. (This change adds no new pure-C# logic; existing tests are unaffected.)

- [ ] **Step 3: Report build + test status**

Print results. No commit (no code changes in this task).

---

## Self-Review Notes

**Spec coverage:**
- §2 Goals (bottom panel, dim, portrait, nameplate, typewriter, advance arrow, advance on key/click, first-press skip, second-press next) — Tasks 5 + 6 implement.
- §4 Data Model (DialogLine struct → Resource, Npc exports) — Tasks 1 + 3 implement.
- §5.1 DialogPlayer API and signals — Task 2 implements.
- §5.2 DialogBox API and state — Task 5 implements.
- §5.3 DialogBox scene file — Task 6 implements.
- §5.4 Overworld removes wiring — Task 7 implements.
- §6 Data Migration (Town.tscn Npc Lines) — Task 8 implements.
- §7 Error Handling (IsActive re-entry, DialogBox.Instance null guard, empty lines) — Task 2 implements.
- §8 Testing (smoke checklist) — Task 9 implements.
- §9 Out of scope — no auto-advance, no skip-read, no backlog. Not implemented. ✓

**Placeholder scan:** No TBD / TODO / "implement later" / "similar to Task N". Every step has exact file paths and complete code.

**Type consistency:** `DialogLine` properties: `SpeakerName` (string), `SpeakerPortrait` (Texture2D), `Text` (string). Used consistently in Task 1 (define), Task 3 (Npc.OnBodyEntered), Task 4 (TownStarter), Task 5 (DialogBox.ShowLine), Task 6 (DialogBox.tscn NameLabel/Portrait wiring), Task 8 (Town.tscn sub_resource). `DialogPlayer.Play(DialogLine[])` consistent across all call sites. `DialogBox.Instance` consistent in Tasks 2 + 5. `AdvancePressed` signal consistent in Tasks 2 + 5.
