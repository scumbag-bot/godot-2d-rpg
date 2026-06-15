# Visual Novel Dialog Box — Design

**Date:** 2026-06-15
**Status:** Draft, awaiting user review
**Engine:** Godot 4.6 (C# / .NET)
**Scope:** Replace the current full-screen opaque dialog panel with a bottom-anchored visual-novel-style dialog box.

## 1. Overview

The current `DialogBox` is a full-screen opaque `Panel` with a single `Label` — it blocks the overworld entirely, has no typewriter, no portrait, no nameplate, and no advance indicator. The player cannot see the world behind it. The original 2026-06-14 spec already calls for a VN-style box (portrait + text + ▶ arrow + coroutine typewriter), but the implementation never delivered it.

This spec rebuilds the dialog system to match that description: a bottom-anchored panel that overlays (but does not hide) the overworld, with a per-line character portrait, name label, typewriter text reveal, and a click/key advance indicator. The player presses any key or clicks to advance — first press skips the typewriter, second press moves to the next line.

## 2. Goals

- Bottom-anchored panel; overworld remains visible and dimmed above it.
- Character portrait (placeholder square) on the left of the panel.
- Speaker name label (nameplate) on the top-left of the text area.
- Typewriter text reveal at a fixed character rate.
- ▶ / ▼ advance indicator appears only when the current line is fully revealed.
- Advance on any key OR mouse click; first press during typewriter reveals the full line, second press moves to the next line.
- All existing NPC dialogs (1 NPC in `Town` per the 2026-06-14 spec) continue to work after data migration.

## 3. Non-Goals (this pass)

- Auto-advance (timer-based) — out of scope.
- Skip-read / hold-to-skip — out of scope.
- Backlog / history view — out of scope.
- Per-character expression swap mid-line — out of scope.
- Audio (voice, SFX) — out of scope.
- Localization — out of scope.
- Multiple portrait frames per speaker — out of scope; one portrait per speaker per line.
- Multi-speaker scenes with per-line portrait changes — out of scope for this pass (the data model supports it but the 1-NPC-per-scene v1 reality does not require it).

## 4. Data Model

### `DialogLine` (new, `scripts/runtime/DialogLine.cs`)

```csharp
public readonly record struct DialogLine(
    string SpeakerName,
    Texture2D SpeakerPortrait,
    string Text
);
```

A pure value type. Lives in `scripts/runtime/` because it is part of the runtime data model, not Godot-dependent UI.

### `Npc` (modified)

`Npc.cs` currently exports:

```csharp
[Export] public string[] Lines;
[Export] public DialogBox DialogBoxRef;  // unused at runtime
```

Replace `Lines` with:

```csharp
[Export] public DialogLine[] Lines;
```

Remove the unused `DialogBoxRef` export. Each `DialogLine` carries its own name, portrait, and text, so a single NPC with 3 lines from 3 different speakers is representable (not used in v1, but supported).

## 5. Components

### `DialogPlayer` (autoload, modified)

`scripts/autoload/DialogPlayer.cs` — currently a coroutine that pushes `LineShown(text)` then `Timer 1.5s` per line. Replace with an advance-driven model.

**Public API:**

```csharp
public bool IsActive { get; private set; }

[Signal] public delegate void DialogStartedEventHandler();
[Signal] public delegate void LineChangedEventHandler(string name, Texture2D portrait, string text);
[Signal] public delegate void DialogFinishedEventHandler();

public async void Play(DialogLine[] lines);
```

**Internal flow (`Play`):**

1. Reject re-entry: if `IsActive`, return.
2. Set `IsActive = true`.
3. Emit `DialogStarted`.
4. For each `line` in `lines`:
   - Emit `LineChanged(line.SpeakerName, line.SpeakerPortrait, line.Text)`.
   - Await `DialogBox.Instance.AdvancePressed` signal.
5. Emit `DialogFinished`.
6. Set `IsActive = false`.

**Notes:**

- `DialogPlayer` holds a reference to the active `DialogBox` via a `DialogBox.Register(this)` call from the box's `_Ready` (see §5.3). This removes the need for `Overworld` to wire signals per scene.
- The 1.5s `Timer` and the old `LineShown(string)` signal are removed.

### `DialogBox` (UI Control, modified)

`scripts/ui/DialogBox.cs` + `scenes/ui/DialogBox.tscn`. The single instance lives as a child of every overworld scene (`Town`, `Route1`, `Cave`, `BossRoom`), instanced as before.

**Signals:**

```csharp
[Signal] public delegate void AdvancePressedEventHandler();
```

**Public API:**

```csharp
public static DialogBox Instance { get; private set; }  // set in _Ready, cleared in _ExitTree

public void ShowLine(string name, Texture2D portrait, string text);
public void RevealInstant();
public void Hide();
```

**Internal state:**

- `_typewriterTween: Tween` — the active typewriter tween, nullable.
- `_isFullyRevealed: bool` — true once the current line's full text is shown.
- `_pendingName, _pendingPortrait, _pendingText` — cached so `RevealInstant` can re-show on the same data if called after reveal.
- `_isDialogActive: bool` — true between `DialogStarted` and `DialogFinished`, gates input.

**`_Ready`:**

- Set `Instance = this`.
- Set `Visible = false`.
- `DialogPlayer dialogPlayer = GetNode<DialogPlayer>("/root/DialogPlayer");`
- `dialogPlayer.DialogStarted += OnDialogStarted;`
- `dialogPlayer.LineChanged += OnLineChanged;`
- `dialogPlayer.DialogFinished += OnDialogFinished;`
- Subscribe `_input` via `SetProcessInput(true)`.

**`OnDialogStarted()`:** `Visible = true; _isDialogActive = true;`

**`OnLineChanged(name, portrait, text)`:**
- Set `_pendingName`, `_pendingPortrait`, `_pendingText`.
- Set nameplate label text, portrait texture (null → hide portrait TextureRect).
- Set `TextLabel.Text = ""`.
- Set `_isFullyRevealed = false`.
- Show advance arrow hidden.
- Create typewriter tween: `TextLabel.CreateTween()` bound to the label, `TweenProperty(TextLabel, "text", text, duration)` using `trans=Tween.TransitionType.Linear` and a custom method is awkward for text — see §5.3.1 for the actual mechanism.
- On tween `Finished` signal: set `_isFullyRevealed = true`, show advance arrow.

**`RevealInstant()`:**
- If `_typewriterTween` is valid and running: `_typewriterTween.Kill()`, set `TextLabel.Text = _pendingText`, set `_isFullyRevealed = true`, show advance arrow.
- If already fully revealed: no-op (input handler will emit `AdvancePressed` separately).

**`_Input(InputEvent @event)`:**
- If `!_isDialogActive` return.
- If event is `InputEventKey` with `Pressed && !Echo` OR `InputEventMouseButton` with `Pressed && ButtonIndex == MouseButton.Left`:
  - If `!_isFullyRevealed`: call `RealInstant()`; `GetViewport().SetInputAsHandled()`.
  - Else: emit `AdvancePressed`; `GetViewport().SetInputAsHandled()`.

**`OnDialogFinished()`:** `_isDialogActive = false; Visible = false;` Clear `_typewriterTween`.

**`Hide()`:** same as OnDialogFinished for the manual hide case (currently unused externally; kept for completeness).

#### 5.3.1 Typewriter mechanism

Godot's `Tween` does not animate `Label.Text` directly (it's a string property and `TweenProperty` interpolates numeric properties). The standard pattern is one of:

- **Option A (chosen):** Build a `StringBuilder` per line and call `TextLabel.Text =` inside a `Tween.TweenCallback` per character. This is simple but creates N callbacks per line.
- **Option B:** Use a `Timer` with `WaitTime = 0.03` and a counter. Simpler control flow, easier to cancel cleanly.
- **Option C:** Manual `Tween` on a `float VisibleCharacters` property via `Label.VisibleCharacters` (Godot 4 supports this — `Label.VisibleCharacters` is a numeric property of type int that tween CAN animate). Cleanest, idiomatic, easy to cancel.

**Decision: Option C.** Use `Label.VisibleCharacters`. Tween from 0 to `text.Length` linearly over `text.Length * 0.03` seconds. On `Finished` set `VisibleCharacters = -1` (or `text.Length`) and set `_isFullyRevealed = true`. To reveal instantly, kill the tween and set `VisibleCharacters = -1`.

This is the cleanest Godot-idiomatic approach, requires no callback soup, and cancellation is a single `_typewriterTween.Kill()`.

### Scene file `DialogBox.tscn` (rewritten)

**Node tree:**

```
DialogBox (Control)                # full screen, top_layer
├─ Background (ColorRect)          # full screen, Color(0,0,0,0.5), dim overlay
├─ Panel (Panel)                   # bottom-anchored, full width, height=200
│   ├─ Portrait (TextureRect)      # anchored left, expand mode keep, custom_minimum_size 96x96
│   ├─ NameLabel (Label)           # top-left of text area, nameplate styling
│   ├─ TextLabel (Label)           # fills text area, autowrap on, big font
│   └─ AdvanceArrow (Label)        # anchored bottom-right, text "▼", hidden initially
```

**Anchors & sizing:**

- `DialogBox`: anchors_preset=15 (full rect), mouse_filter=Stop, top_layer=true.
- `Background`: anchors_preset=15 (full rect), color=Color(0,0,0,0.5).
- `Panel`: anchors_preset=12 (bottom wide), anchor_top=0.7, anchor_bottom=1.0, custom_minimum_size.y=200. StyleBoxFlat override: bg_color=Color(0.05,0.05,0.1,0.92), border_width_*=2, border_color=Color(0.6,0.6,0.7,1).
- `Portrait`: anchored to panel left, offset_left=8, offset_top=-16 (extends above panel), custom_minimum_size=(96,96), expand_mode=keep, stretch_mode=keep.
- `NameLabel`: anchored to panel top-left inside text area, offset_left=120, offset_top=8, text="Name", font size override, modulate=Color(1,0.9,0.5,1) (gold).
- `TextLabel`: anchored inside panel, anchor_left=0, anchor_right=1, anchor_top=0, anchor_bottom=1, offset_left=120, offset_right=-32, offset_top=36, offset_bottom=-24, autowrap_mode=3 (arbitrary), horizontal_alignment=0, vertical_alignment=0, font size override, visible_characters=-1.
- `AdvanceArrow`: anchored to panel bottom-right, offset_right=-16, offset_bottom=-8, text="▼", modulate=Color(1,1,1,0.8), visible=false.

**Theme:** No external theme file. All styling is inline overrides on the Panel and Labels. (Per AGENTS.md, theme override is a single-file swap later.)

**Script binding:** `DialogBox.cs` is attached to the root, with `[Export] TextLabel`, `[Export] NameLabel`, `[Export] Portrait`, `[Export] AdvanceArrow` node paths.

### `Overworld` (modified)

Remove the DialogBox wiring block in `_Ready`:

```csharp
// REMOVE:
var dialogBox = GetNodeOrNull<DialogBox>("DialogBox");
if (dialogBox != null) {
    var dp = GetNode<DialogPlayer>("/root/DialogPlayer");
    dp.LineShown += text => dialogBox.ShowLine(text);
    dp.DialogFinished += () => dialogBox.Hide();
}
```

DialogBox now self-subscribes via its own `_Ready`. The `using rpg_game.scripts.ui;` and the `DialogBox` import become unused — keep the import for clarity if `HealthBar` or other UI refs appear later (currently it doesn't), otherwise drop.

## 6. Data Migration

The single NPC in `Town.tscn` has `Lines` as `string[]` with current text. After this change, `Lines` becomes `DialogLine[]`. Migration in the scene file:

- Find the Npc node's `Lines = ["..."]` array.
- Replace with `Lines = [{SpeakerName="...", SpeakerPortrait=null, Text="..."}, ...]`.
- Use a placeholder speaker name (e.g. "Elder") and leave portrait null (the box handles null portrait by hiding the Portrait TextureRect).

If `SpeakerPortrait` is null at runtime, the `Portrait` TextureRect is hidden (`Visible = false`) and the text area expands to fill the left side. No placeholder square is required.

## 7. Error Handling

- **Re-entrant Play():** Silently ignored. `IsActive` is set at the start and cleared at the end.
- **DialogBox not in scene:** If a `Npc` is in a scene without a `DialogBox` child, `DialogPlayer.Play` will crash when awaiting `AdvancePressed` from a null `DialogBox.Instance`. Mitigations:
  - At `Play` start, check `DialogBox.Instance == null` → log a warning and return without setting `IsActive`. (Defensive, no crash.)
  - The existing 4 overworld scenes all have a `DialogBox` child, so this is a guardrail for future scenes.
- **Empty `Lines` array:** `Play([])` emits `DialogStarted` then `DialogFinished` immediately. No-op visually.
- **Tween outliving dialog:** If `DialogFinished` fires while the typewriter tween is running, the tween is killed and the box hides cleanly.
- **DialogBox freed mid-dialog:** `AdvancePressed` signal will never fire and the coroutine will hang. Acceptable for v1; a timeout could be added later. Mitigated by the fact that DialogBox is a child of the scene and only frees on scene change, and dialog always finishes before scene change.

## 8. Testing

**Automated (NUnit, `tests/rpg-game.Tests/`):** N/A. This change is UI and autoload wiring — the smoke-test checklist in `SMOKE-TEST-F5.md` is the verification path. No new pure-C# logic worth unit-testing.

**Manual smoke test (F5 checklist, add to `SMOKE-TEST-F5.md` after implementation):**

1. Boot game, reach Town.
2. Walk into the NPC. Dialog box appears at bottom of screen.
3. Overworld above the panel is dimmed but visible.
4. First line of NPC dialog shows: name "Elder" (or whatever was set), no portrait (or placeholder if set), text appears one character at a time.
5. Press any key mid-typing → full text appears instantly.
6. Press any key again → next line begins.
7. After last line, press key → dialog box hides.
8. Re-enter NPC → dialog replays from line 1.
9. Mouse click on the dialog box area also advances (test in step 5 + 6 with mouse).
10. `▶` / `▼` arrow visible only when line is fully revealed (or only when revealed, not during typing).

## 9. Out of Scope (re-stated for clarity)

Per project-wide v1 non-goals (`docs/superpowers/specs/2026-06-14-rpg-game-design.md` §10): save/load, audio, status effects, evolution, quest log UI, side quests, localization, mobile/web. The "no auto-advance / skip-read / backlog" decision for this pass is recorded in §3.

## 10. Approval

- Sections 1–5 of the brainstorming discussion were individually approved.
- Spec drafted and self-reviewed; awaiting user review.
