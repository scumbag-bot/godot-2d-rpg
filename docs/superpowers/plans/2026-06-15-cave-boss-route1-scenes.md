# Cave, BossRoom, Route1 Scenes + Scene Transition Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the missing 3 scenes (Route1, Cave, BossRoom), a generic `WarpZone` Area2D trigger, and a fade-transition system in `SceneManager`. Player can walk from Town → Route1 → Cave → BossRoom and back.

**Architecture:** `WarpZone` Area2D on scene edges calls `SceneManager.WarpTo(path, pos)`. `SceneManager` runs a 200ms fade-out, swaps scene, restores player position from `PendingPlayerPosition`, fades back in. New `Player._Ready` reads pending position and sets `GlobalPosition` if set.

**Tech Stack:** Godot 4.6, C# (.NET 8). Reuses existing autoloads (`SceneManager`), existing data (encounter tables), and existing scripts (`Overworld.cs`, `Enemy.cs`, `Npc.cs`, `Player.cs`).

**Reference spec:** `docs/superpowers/specs/2026-06-14-rpg-game-design.md` (sections 7 "Overworld + Encounter Flow", 8 "UI"). Reference quest plan: `docs/superpowers/plans/2026-06-15-quest-system.md` (stage 3 "cave_enter" needs Cave scene; stage 4 "boss" needs BossRoom scene).

---

## File Structure

**New files:**
```
scripts/overworld/WarpZone.cs
scenes/Route1.tscn
scenes/Cave.tscn
scenes/BossRoom.tscn
resources/data/encounters/SingleImp.tres
resources/data/encounters/SingleSalamander.tres
resources/data/encounters/SingleWraith.tres
resources/data/encounters/SingleSuccubus.tres
resources/data/encounters/SingleBear.tres
resources/data/encounters/SingleTreant.tres
```

**Modified files:**
```
scripts/autoload/SceneManager.cs   (add WarpTo + PendingPlayerPosition)
scripts/overworld/Player.cs       (read PendingPlayerPosition in _Ready)
scenes/Town.tscn                 (add north WarpZone to Route1)
```

**Boundaries:**
- `WarpZone` is scene-agnostic — single script, parameterized by `TargetScenePath` and `TargetPosition`.
- `SceneManager.WarpTo` is the only scene-swap entry point (replaces direct `ChangeSceneToFile` calls).
- Player position is set AFTER the scene swap; the new scene's `Player._Ready` consumes the pending value.
- No `ChangeSceneToFile` calls from anywhere else after this plan.

---

## Phase A: Scene Transition System

### Task A1: Add `WarpTo` + fade transition to `SceneManager`

**Files:**
- Modify: `scripts/autoload/SceneManager.cs`

- [ ] **Step 1: Read current SceneManager**

```bash
Get-Content "C:\Users\Adovan\Documents\rpg-game\scripts\autoload\SceneManager.cs"
```

- [ ] **Step 2: Replace SceneManager.cs with extended version**

```csharp
using Godot;

namespace rpg_game.scripts.autoload;

public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }
    public Vector2? PendingPlayerPosition { get; set; }

    private bool _isWarping;

    public override void _Ready()
    {
        Instance = this;
    }

    public void GotoScene(string path)
    {
        GetTree().ChangeSceneToFile(path);
    }

    public void GotoOverworld()
    {
        GotoScene("res://scenes/Town.tscn");
    }

    public void GotoBattle()
    {
        GotoScene("res://scenes/Battle.tscn");
    }

    public void GotoTitle()
    {
        GotoScene("res://scenes/TitleScreen.tscn");
    }

    public async void WarpTo(string scenePath, Vector2 targetPosition)
    {
        if (_isWarping) return;
        _isWarping = true;

        var overlay = CreateFadeOverlay();
        var tweenOut = overlay.CreateTween();
        tweenOut.TweenProperty(overlay, "modulate:a", 1.0f, 0.2);
        await ToSignal(tweenOut, Tween.SignalName.Finished);

        PendingPlayerPosition = targetPosition;
        GetTree().ChangeSceneToFile(scenePath);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var tweenIn = overlay.CreateTween();
        tweenIn.TweenProperty(overlay, "modulate:a", 0.0f, 0.2);
        await ToSignal(tweenIn, Tween.SignalName.Finished);
        overlay.QueueFree();

        _isWarping = false;
    }

    private CanvasLayer CreateFadeOverlay()
    {
        var layer = new CanvasLayer { Layer = 100 };
        var rect = new ColorRect
        {
            Name = "WarpFade",
            Color = new Color(0, 0, 0, 1),
            Modulate = new Color(1, 1, 1, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.AnchorRight = 1.0;
        rect.AnchorBottom = 1.0;
        layer.AddChild(rect);
        GetTree().Root.AddChild(layer);
        return layer;
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add scripts/autoload/SceneManager.cs
git commit -m "feat(autoload): add WarpTo + fade transition + PendingPlayerPosition"
```

---

### Task A2: Read `PendingPlayerPosition` in `Player._Ready`

**Files:**
- Modify: `scripts/overworld/Player.cs`

- [ ] **Step 1: Read current Player.cs**

Look at the `_Ready` method (around line 14-25).

- [ ] **Step 2: Modify `_Ready` to consume pending position**

After the existing sprite setup, at the END of `_Ready`, add (inside the method, after the `Sprite.Play` call):

```csharp
var sm = GetNodeOrNull<autoload.SceneManager>("/root/SceneManager");
if (sm != null && sm.PendingPlayerPosition.HasValue)
{
    GlobalPosition = sm.PendingPlayerPosition.Value;
    sm.PendingPlayerPosition = null;
}
```

The final `_Ready` should look like:

```csharp
public override void _Ready()
{
    _sprite = GetNodeOrNull<AnimatedSprite2D>(SpritePath);
    if (_sprite == null)
    {
        var names = new System.Text.StringBuilder();
        foreach (var c in GetChildren()) names.Append(c.Name).Append(", ");
        GD.PushError($"Player: AnimatedSprite2D not found at '{SpritePath}'. Children: {names}");
        return;
    }
    if (_sprite.SpriteFrames == null)
    {
        GD.PushError("Player: Sprite.SpriteFrames is null");
        return;
    }
    var anims = _sprite.SpriteFrames.GetAnimationNames();
    GD.Print($"Player: sprite OK, anims=[{string.Join(",", anims)}]");
    if (_sprite.SpriteFrames.HasAnimation("idle_down"))
    {
        _sprite.Play("idle_down");
        _currentAnim = "idle_down";
    }

    var sm = GetNodeOrNull<autoload.SceneManager>("/root/SceneManager");
    if (sm != null && sm.PendingPlayerPosition.HasValue)
    {
        GlobalPosition = sm.PendingPlayerPosition.Value;
        sm.PendingPlayerPosition = null;
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build rpg-game.sln -c Debug` — 0 errors.

- [ ] **Step 4: Commit**

```bash
git add scripts/overworld/Player.cs
git commit -m "feat(overworld): Player reads PendingPlayerPosition from SceneManager on _Ready"
```

---

### Task A3: `WarpZone` Area2D script

**Files:**
- Create: `scripts/overworld/WarpZone.cs`

- [ ] **Step 1: Implement**

```csharp
using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.overworld;

public partial class WarpZone : Area2D
{
    [Export] public string TargetScenePath = "";
    [Export] public Vector2 TargetPosition = Vector2.Zero;
    [Export] public bool OneShot = true;

    private bool _triggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_triggered && OneShot) return;
        if (body is not Player) return;
        if (string.IsNullOrEmpty(TargetScenePath)) return;

        var dialog = GetNodeOrNull<DialogPlayer>("/root/DialogPlayer");
        if (dialog != null && dialog.IsActive) return;

        _triggered = true;
        GetNode<SceneManager>("/root/SceneManager").WarpTo(TargetScenePath, TargetPosition);
    }
}
```

- [ ] **Step 2: Verify build + commit**

```bash
dotnet build rpg-game.sln -c Debug
git add scripts/overworld/WarpZone.cs
git commit -m "feat(overworld): add WarpZone Area2D trigger for scene transitions"
```

---

## Phase B: Standalone EncounterEntry .tres files

The plan needs 6 more `EncounterEntry.tres` files (one per unique wild/boss enemy) so each `Enemy` node in a scene can reference a single-entry resource.

- [ ] **Step 1: Read existing `SingleWolf.tres` for format reference**

```bash
Get-Content "C:\Users\Adovan\Documents\rpg-game\resources\data\encounters\SingleWolf.tres"
```

- [ ] **Step 2: Create 6 new .tres files**

For each, use the template:

```
[gd_resource type="Resource" script_class="EncounterEntry" load_steps=3 format=3]

[ext_resource type="Script" path="res://scripts/data/EncounterEntry.cs" id="1_entry"]
[ext_resource type="Resource" path="res://resources/data/creatures/<SPECIES>.tres" id="2_species"]

[resource]
script = ExtResource("1_entry")
Species = ExtResource("2_species")
Level = <N>
CaptureEligible = <true/false>
```

Files to create:

| File | Species | Level | CaptureEligible |
|---|---|---|---|
| `SingleImp.tres` | Imp | 4 | true |
| `SingleSalamander.tres` | Salamander | 5 | true |
| `SingleWraith.tres` | Wraith | 6 | true |
| `SingleSuccubus.tres` | Succubus | 7 | true |
| `SingleBear.tres` | Bear | 6 | true |
| `SingleTreant.tres` | Treant | 10 | false |

Create the `load_steps` count: 3 (1 ext_resource × 2 + the resource itself = 3). Already have 1 ext_resource for the script and 1 for the species, so 3 is correct.

- [ ] **Step 3: Commit**

```bash
git add resources/data/encounters/
git commit -m "content: add 6 single-entry encounter .tres for Route1, Cave, BossRoom"
```

---

## Phase C: Route1 Scene

### Task C1: `Route1.tscn`

**Files:**
- Create: `scenes/Route1.tscn`

- [ ] **Step 1: Author the .tscn**

```
[gd_scene load_steps=12 format=3 uid="uid://b0route1001"]

[ext_resource type="Script" path="res://scripts/overworld/Overworld.cs" id="1_ow"]
[ext_resource type="PackedScene" path="res://scenes/ui/DialogBox.tscn" id="2_db"]
[ext_resource type="Script" path="res://scripts/overworld/Player.cs" id="3_pl"]
[ext_resource type="Script" path="res://scripts/overworld/Enemy.cs" id="4_en"]
[ext_resource type="Script" path="res://scripts/overworld/WarpZone.cs" id="5_wz"]
[ext_resource type="Resource" path="res://resources/data/encounters/SingleWolf.tres" id="6_wolf"]
[ext_resource type="Resource" path="res://resources/data/encounters/SingleImp.tres" id="7_imp"]
[ext_resource type="Resource" path="res://resources/data/encounters/SingleSalamander.tres" id="8_sal"]

[sub_resource type="RectangleShape2D" id="RectShape_enemy"]
size = Vector2(32, 32)

[sub_resource type="RectangleShape2D" id="RectShape_warp"]
size = Vector2(800, 32)

[node name="Route1" type="Node2D"]
script = ExtResource("1_ow")

[node name="Background" type="ColorRect" parent="."]
offset_right = 800.0
offset_bottom = 800.0
color = Color(0.25, 0.45, 0.25, 1)

[node name="TileMapLayer" type="TileMapLayer" parent="."]

[node name="Player" type="CharacterBody2D" parent="."]
position = Vector2(400, 700)
script = ExtResource("3_pl")
Speed = 80
SpritePath = NodePath("Sprite")

[node name="Sprite" type="AnimatedSprite2D" parent="Player"]

[node name="CollisionShape2D" type="CollisionShape2D" parent="Player"]

[node name="Camera2D" type="Camera2D" parent="Player"]

[node name="Enemy" type="Area2D" parent="."]
position = Vector2(200, 400)
script = ExtResource("4_en")
Entry = ExtResource("6_wolf")

[node name="Sprite" type="ColorRect" parent="Enemy"]
offset_left = -8.0
offset_top = -8.0
offset_right = 8.0
offset_bottom = 8.0
color = Color(0.8, 0.2, 0.2, 1)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Enemy"]
shape = SubResource("RectShape_enemy")

[node name="Enemy2" type="Area2D" parent="."]
position = Vector2(500, 350)
script = ExtResource("4_en")
Entry = ExtResource("7_imp")

[node name="Sprite" type="ColorRect" parent="Enemy2"]
offset_left = -8.0
offset_top = -8.0
offset_right = 8.0
offset_bottom = 8.0
color = Color(0.8, 0.2, 0.2, 1)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Enemy2"]
shape = SubResource("RectShape_enemy")

[node name="Enemy3" type="Area2D" parent="."]
position = Vector2(650, 250)
script = ExtResource("4_en")
Entry = ExtResource("8_sal")

[node name="Sprite" type="ColorRect" parent="Enemy3"]
offset_left = -8.0
offset_top = -8.0
offset_right = 8.0
offset_bottom = 8.0
color = Color(0.8, 0.2, 0.2, 1)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Enemy3"]
shape = SubResource("RectShape_enemy")

[node name="WarpToCave" type="Area2D" parent="."]
position = Vector2(400, 50)
script = ExtResource("5_wz")
TargetScenePath = "res://scenes/Cave.tscn"
TargetPosition = Vector2(400, 700)

[node name="Sprite" type="ColorRect" parent="WarpToCave"]
offset_left = -400.0
offset_top = -16.0
offset_right = 400.0
offset_bottom = 16.0
color = Color(0.1, 0.1, 0.1, 0.5)

[node name="CollisionShape2D" type="CollisionShape2D" parent="WarpToCave"]
shape = SubResource("RectShape_warp")

[node name="WarpToTown" type="Area2D" parent="."]
position = Vector2(400, 780)
script = ExtResource("5_wz")
TargetScenePath = "res://scenes/Town.tscn"
TargetPosition = Vector2(400, 100)

[node name="Sprite" type="ColorRect" parent="WarpToTown"]
offset_left = -400.0
offset_top = -16.0
offset_right = 400.0
offset_bottom = 16.0
color = Color(0.1, 0.1, 0.1, 0.5)

[node name="CollisionShape2D" type="CollisionShape2D" parent="WarpToTown"]
shape = SubResource("RectShape_warp")

[node name="DialogBox" parent="." instance=ExtResource("2_db")]
visible = false
```

Note: The Player's `Sprite` AnimatedSprite2D has no `sprite_frames` set here — the user will either copy the SpriteFrames from Town or create a fresh one in the editor. The Player._Ready has logging that will surface this in the Output panel.

If the user wants the same SpriteFrames as Town, they can:
1. Open Route1 in the editor
2. Select Player → Sprite (AnimatedSprite2D)
3. Inspector → Sprite Frames → drag the same SpriteFrames_orypj from the FileSystem (or copy from Town)
4. OR: in Player._Ready add a fallback that loads a shared SpriteFrames resource

- [ ] **Step 2: Verify build**

Run: `dotnet build rpg-game.sln -c Debug` — 0 errors.

- [ ] **Step 3: Commit**

```bash
git add scenes/Route1.tscn
git commit -m "feat(scene): add Route1 with 3 wild enemies + warp to Cave/Town"
```

---

## Phase D: Cave Scene

### Task D1: `Cave.tscn`

**Files:**
- Create: `scenes/Cave.tscn`

- [ ] **Step 1: Author the .tscn**

```
[gd_scene load_steps=12 format=3 uid="uid://b0cave001"]

[ext_resource type="Script" path="res://scripts/overworld/Overworld.cs" id="1_ow"]
[ext_resource type="PackedScene" path="res://scenes/ui/DialogBox.tscn" id="2_db"]
[ext_resource type="Script" path="res://scripts/overworld/Player.cs" id="3_pl"]
[ext_resource type="Script" path="res://scripts/overworld/Enemy.cs" id="4_en"]
[ext_resource type="Script" path="res://scripts/overworld/WarpZone.cs" id="5_wz"]
[ext_resource type="Resource" path="res://resources/data/encounters/SingleWraith.tres" id="6_wraith"]
[ext_resource type="Resource" path="res://resources/data/encounters/SingleSuccubus.tres" id="7_succ"]
[ext_resource type="Resource" path="res://resources/data/encounters/SingleBear.tres" id="8_bear"]

[sub_resource type="RectangleShape2D" id="RectShape_enemy"]
size = Vector2(32, 32)

[sub_resource type="RectangleShape2D" id="RectShape_warp"]
size = Vector2(800, 32)

[node name="Cave" type="Node2D"]
script = ExtResource("1_ow")

[node name="Background" type="ColorRect" parent="."]
offset_right = 800.0
offset_bottom = 800.0
color = Color(0.12, 0.10, 0.15, 1)

[node name="TileMapLayer" type="TileMapLayer" parent="."]

[node name="Player" type="CharacterBody2D" parent="."]
position = Vector2(400, 700)
script = ExtResource("3_pl")
Speed = 80
SpritePath = NodePath("Sprite")

[node name="Sprite" type="AnimatedSprite2D" parent="Player"]

[node name="CollisionShape2D" type="CollisionShape2D" parent="Player"]

[node name="Camera2D" type="Camera2D" parent="Player"]

[node name="Enemy" type="Area2D" parent="."]
position = Vector2(250, 450)
script = ExtResource("4_en")
Entry = ExtResource("6_wraith")

[node name="Sprite" type="ColorRect" parent="Enemy"]
offset_left = -8.0
offset_top = -8.0
offset_right = 8.0
offset_bottom = 8.0
color = Color(0.7, 0.5, 0.8, 1)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Enemy"]
shape = SubResource("RectShape_enemy")

[node name="Enemy2" type="Area2D" parent="."]
position = Vector2(550, 380)
script = ExtResource("4_en")
Entry = ExtResource("7_succ")

[node name="Sprite" type="ColorRect" parent="Enemy2"]
offset_left = -8.0
offset_top = -8.0
offset_right = 8.0
offset_bottom = 8.0
color = Color(0.7, 0.5, 0.8, 1)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Enemy2"]
shape = SubResource("RectShape_enemy")

[node name="Enemy3" type="Area2D" parent="."]
position = Vector2(400, 250)
script = ExtResource("4_en")
Entry = ExtResource("8_bear")

[node name="Sprite" type="ColorRect" parent="Enemy3"]
offset_left = -8.0
offset_top = -8.0
offset_right = 8.0
offset_bottom = 8.0
color = Color(0.7, 0.5, 0.8, 1)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Enemy3"]
shape = SubResource("RectShape_enemy")

[node name="WarpToBoss" type="Area2D" parent="."]
position = Vector2(400, 50)
script = ExtResource("5_wz")
TargetScenePath = "res://scenes/BossRoom.tscn"
TargetPosition = Vector2(400, 600)

[node name="Sprite" type="ColorRect" parent="WarpToBoss"]
offset_left = -400.0
offset_top = -16.0
offset_right = 400.0
offset_bottom = 16.0
color = Color(0.05, 0.05, 0.05, 0.7)

[node name="CollisionShape2D" type="CollisionShape2D" parent="WarpToBoss"]
shape = SubResource("RectShape_warp")

[node name="WarpToRoute1" type="Area2D" parent="."]
position = Vector2(400, 780)
script = ExtResource("5_wz")
TargetScenePath = "res://scenes/Route1.tscn"
TargetPosition = Vector2(400, 100)

[node name="Sprite" type="ColorRect" parent="WarpToRoute1"]
offset_left = -400.0
offset_top = -16.0
offset_right = 400.0
offset_bottom = 16.0
color = Color(0.05, 0.05, 0.05, 0.7)

[node name="CollisionShape2D" type="CollisionShape2D" parent="WarpToRoute1"]
shape = SubResource("RectShape_warp")

[node name="DialogBox" parent="." instance=ExtResource("2_db")]
visible = false
```

- [ ] **Step 2: Verify build + commit**

```bash
dotnet build rpg-game.sln -c Debug
git add scenes/Cave.tscn
git commit -m "feat(scene): add Cave with 3 wild enemies + warp to BossRoom/Route1"
```

---

## Phase E: BossRoom Scene

### Task E1: `BossRoom.tscn`

**Files:**
- Create: `scenes/BossRoom.tscn`

- [ ] **Step 1: Author the .tscn**

```
[gd_scene load_steps=8 format=3 uid="uid://b0boss001"]

[ext_resource type="Script" path="res://scripts/overworld/Overworld.cs" id="1_ow"]
[ext_resource type="PackedScene" path="res://scenes/ui/DialogBox.tscn" id="2_db"]
[ext_resource type="Script" path="res://scripts/overworld/Player.cs" id="3_pl"]
[ext_resource type="Script" path="res://scripts/overworld/Enemy.cs" id="4_en"]
[ext_resource type="Script" path="res://scripts/overworld/WarpZone.cs" id="5_wz"]
[ext_resource type="Resource" path="res://resources/data/encounters/SingleTreant.tres" id="6_treant"]

[sub_resource type="RectangleShape2D" id="RectShape_enemy"]
size = Vector2(48, 48)

[sub_resource type="RectangleShape2D" id="RectShape_warp"]
size = Vector2(800, 32)

[node name="BossRoom" type="Node2D"]
script = ExtResource("1_ow")

[node name="Background" type="ColorRect" parent="."]
offset_right = 800.0
offset_bottom = 800.0
color = Color(0.15, 0.05, 0.05, 1)

[node name="TileMapLayer" type="TileMapLayer" parent="."]

[node name="Player" type="CharacterBody2D" parent="."]
position = Vector2(400, 700)
script = ExtResource("3_pl")
Speed = 80
SpritePath = NodePath("Sprite")

[node name="Sprite" type="AnimatedSprite2D" parent="Player"]

[node name="CollisionShape2D" type="CollisionShape2D" parent="Player"]

[node name="Camera2D" type="Camera2D" parent="Player"]

[node name="Boss" type="Area2D" parent="."]
position = Vector2(400, 350)
script = ExtResource("4_en")
Entry = ExtResource("6_treant")

[node name="Sprite" type="ColorRect" parent="Boss"]
offset_left = -16.0
offset_top = -16.0
offset_right = 16.0
offset_bottom = 16.0
color = Color(0.4, 0.1, 0.6, 1)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Boss"]
shape = SubResource("RectShape_enemy")

[node name="WarpToCave" type="Area2D" parent="."]
position = Vector2(400, 780)
script = ExtResource("5_wz")
TargetScenePath = "res://scenes/Cave.tscn"
TargetPosition = Vector2(400, 100)

[node name="Sprite" type="ColorRect" parent="WarpToCave"]
offset_left = -400.0
offset_top = -16.0
offset_right = 400.0
offset_bottom = 16.0
color = Color(0.05, 0.05, 0.05, 0.7)

[node name="CollisionShape2D" type="CollisionShape2D" parent="WarpToCave"]
shape = SubResource("RectShape_warp")

[node name="DialogBox" parent="." instance=ExtResource("2_db")]
visible = false
```

- [ ] **Step 2: Verify build + commit**

```bash
dotnet build rpg-game.sln -c Debug
git add scenes/BossRoom.tscn
git commit -m "feat(scene): add BossRoom with Treant boss + warp to Cave"
```

---

## Phase F: Wire Town exit

### Task F1: Add WarpZone at north end of Town

**Files:**
- Modify: `scenes/Town.tscn`

- [ ] **Step 1: Read current Town.tscn end**

Look at the last few nodes (after `DialogBox` and `TownStarter`).

- [ ] **Step 2: Append WarpZone to north end of Town**

Open the .tscn and add the following BEFORE the closing of the scene file (the last node). Place this AFTER `TownStarter`:

```
[node name="WarpToRoute1" type="Area2D" parent="."]
position = Vector2(400, 50)
script = ExtResource("5_wz")
TargetScenePath = "res://scenes/Route1.tscn"
TargetPosition = Vector2(400, 700)

[node name="Sprite" type="ColorRect" parent="WarpToRoute1"]
offset_left = -400.0
offset_top = -16.0
offset_right = 400.0
offset_bottom = 16.0
color = Color(0.1, 0.1, 0.1, 0.5)

[node name="CollisionShape2D" type="CollisionShape2D" parent="WarpToRoute1"]
shape = SubResource("RectShape_warp")
```

Also add to the `[sub_resource ...]` section (if not present) the `RectShape_warp`:

```
[sub_resource type="RectangleShape2D" id="RectShape_warp"]
size = Vector2(800, 32)
```

Also add to the top `[ext_resource ...]` block:

```
[ext_resource type="Script" path="res://scripts/overworld/WarpZone.cs" id="5_wz"]
```

(Replace the existing `id="5_en"` or add alongside. The Town.tscn already has `id="5_en"` for Enemy.cs. Use `id="9_wz"` to avoid collision.)

- [ ] **Step 3: Verify build + commit**

```bash
dotnet build rpg-game.sln -c Debug
git add scenes/Town.tscn
git commit -m "feat(scene): add WarpZone at north end of Town to Route1"
```

---

## Phase G: Manual Smoke Test

### Task G1: Code-level wiring verification

- [ ] **Step 1: Verify wiring**

Check:
1. `scripts/autoload/SceneManager.cs` has `WarpTo` + `PendingPlayerPosition`
2. `scripts/overworld/Player.cs` reads `PendingPlayerPosition` in `_Ready`
3. `scripts/overworld/WarpZone.cs` exists with TargetScenePath + TargetPosition
4. `Route1.tscn` has 3 Enemy nodes (Wolf, Imp, Salamander) + 2 WarpZones (Cave north, Town south)
5. `Cave.tscn` has 3 Enemy nodes (Wraith, Succubus, Bear) + 2 WarpZones (BossRoom north, Route1 south)
6. `BossRoom.tscn` has 1 Boss enemy (Treant) + 1 WarpZone (Cave south)
7. `Town.tscn` has 1 new WarpZone (Route1 north)
8. All `Enemy` nodes have `Entry` set to a standalone `.tres` file
9. `dotnet build` 0 errors
10. `dotnet test` 18/18

- [ ] **Step 2: Commit smoke test note**

```bash
git add -A
git commit -m "docs: code-level smoke test for scene transition system" --allow-empty
```

**Note**: User must open each new scene in the Godot editor and:
- Assign `SpriteFrames` to each Player's AnimatedSprite2D (copy from Town, or fresh)
- Paint the `TileMapLayer` with their TileSet (or leave the placeholder ColorRect background)
- Save the .tscn

F5 in Godot editor: Title → New Game → Town. Walk north → fade → Route1. Walk north again → fade → Cave. Walk north → fade → BossRoom. Boss at center. Walk south → back through chain.

---

## Caveats

1. **No TileSet in scenes.** The hand-written scenes have empty `TileMapLayer` and a `ColorRect` background placeholder. User must paint the tilemap in the editor.

2. **No SpriteFrames on Player in new scenes.** Each new scene's Player has a bare `AnimatedSprite2D`. User must assign `SpriteFrames_orypj` (or similar) from the FileSystem. Alternative: bake the SpriteFrames reference into the .tscn (hand-write a `sub_resource` SpriteFrames block — large, brittle). Simpler: assign in editor.

3. **BossRoom has no capture opportunity.** `SingleTreant.tres` has `CaptureEligible = false` per spec. Boss fight is win-only.

4. **No quest integration yet.** When the quest system is built, Cave entrance (stage 3) and Boss victory (stage 4) will need:
   - Cave WarpZone fires `QuestStore.Advance` on enter (or NPC at cave mouth)
   - Boss victory fires `QuestStore.Advance`
   - This is in the quest plan, not here.

5. **Fighting on warp edge.** If player walks from Route1 north and an Enemy Area2D is also at the north edge, the warp may fire before the battle. The WarpZone collision shape spans the full width of the screen; if Enemy Areas overlap, warps take precedence (WarpZone body entered, then Enemy body entered). Both trigger. Order not guaranteed. Acceptable for v1.

6. **No save/load of player position.** `PendingPlayerPosition` is in-memory. Restart wipes mid-warp state. Acceptable per v1 OOS.

7. **Fade overlay not visible for very fast warps.** If two warps fire in quick succession (player walks edge-to-edge), the second `WarpTo` is blocked by `_isWarping` guard. Falls back to teleport. Acceptable.

---

## Recommendation: Fix Caveats First vs Implement Straight

**Implement straight. Fix caveats inline as needed.**

| # | Caveat | Recommendation |
|---|---|---|
| 1 | No TileSet in scenes | **Defer to editor.** Scenes structurally complete; visual is editor task. |
| 2 | No SpriteFrames on new Players | **Defer to editor.** Same reason. |
| 3 | BossRoom no capture | **Correct per spec.** Keep. |
| 4 | No quest integration | **Separate plan** (quest-system plan). Build after this. |
| 5 | Warp + enemy edge collision | **Acceptable for v1.** Defer. |
| 6 | No save/load of position | **OOS for v1.** Defer. |
| 7 | Fast-warp edge case | **Acceptable for v1.** Defer. |

No blockers. Plan is ready to execute.

---

## Self-Review

**Spec coverage:**
- [x] WarpZone trigger (new component)
- [x] Scene transition with fade (SceneManager.WarpTo)
- [x] Route1 scene with 3 wild enemies from Route1.tres
- [x] Cave scene with 3 wild enemies from Cave.tres
- [x] BossRoom scene with Treant boss from BossRoom.tres
- [x] Town ↔ Route1 ↔ Cave ↔ BossRoom ↔ Cave loop
- [x] Player position restoration
- [x] Caveats + recommendation sections

**Type consistency:**
- `WarpZone.TargetScenePath` (string) ↔ `SceneManager.WarpTo(string, Vector2)` — consistent
- `WarpZone.TargetPosition` (Vector2) ↔ `PendingPlayerPosition` (Vector2?) — consistent
- All `Enemy.Entry` references standalone `.tres` files with valid EncounterEntry schema
- `SingleX.tres` ids in scene ext_resource blocks use unique alphanumeric suffixes (6_wolf, 7_imp, 8_sal, etc.) — no collisions

**Placeholders:** none. All code blocks complete. All file contents provided.

**Ready to execute.** Dispatch implementer subagents in order: A1 → A2 → A3 → Phase B → C1 → D1 → E1 → F1 → G1.
