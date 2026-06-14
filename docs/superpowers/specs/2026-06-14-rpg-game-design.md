# 2D RPG Game — Initial Design

**Date:** 2026-06-14
**Status:** Draft, awaiting user review
**Engine:** Godot 4.6 (C# / .NET)
**Scope:** Vertical slice, ~2–4 hours of gameplay

## 1. Overview

A small, story-driven creature-collecting 2D RPG in the style of classic Pokémon. The player explores a linear medieval-fantasy region, encounters and recruits demons and animals, and battles humans, demons, and animals in turn-based combat. Pixel art (16/32-bit) aesthetic. Built greenfield from a freshly scaffolded Godot 4.6 project.

The deliverable for v1 is a single, complete play arc: one town, one route, one cave, one boss. All content is placeholder-colored sprites; design is the goal, art is swap-in later.

## 2. Goals

- Player can move on a top-down overworld, talk to one NPC, and trigger visible enemy encounters by running into them.
- Player can engage in turn-based battles with at minimum attack, switch creature, use one item, and run.
- Player can capture wild creatures via menu interaction after reducing their HP to 0 ("catch" — flow TBD with user).
- 6–10 creature species defined as data; player party is 6 slots.
- 1 boss battle at the end of the cave.
- Combat, switching, leveling, capture, and victory/defeat flows all wired end-to-end.

## 3. Non-Goals (v1)

- Save/load (session state only, no save files).
- Abilities and held items.
- Status effects (poison, burn, etc. — enum exists, no logic).
- More than one boss battle.
- Side quests, dialogue branches.
- World map, multiple regions.
- Music / SFX (audio bus structure in place, slots empty).
- Accessibility options.
- Crafting, farming, day/night cycle.

## 4. Architecture

### Folder layout

```
rpg-game/
  project.godot
  scripts/
    autoload/
      SceneManager.cs
      GameState.cs
      Party.cs
      EncounterSystem.cs
      BattleManager.cs
      DialogPlayer.cs
    data/
      CreatureSpecies.cs
      MoveData.cs
      TypeChart.cs
      EncounterEntry.cs
      EncounterTable.cs
    runtime/
      CreatureInstance.cs
      Stats.cs
      CreatureType.cs
      MoveCategory.cs
    battle/
      BattleState.cs
      BattleAction.cs
      BattleEvent.cs
    util/
      PlaceholderTexture.cs
  scenes/
    TitleScreen.tscn
    Overworld.tscn
    Battle.tscn
    ui/
      DialogBox.tscn
      HealthBar.tscn
      PartyMenu.tscn
      ItemMenu.tscn
  resources/
    data/
      creatures/  (one .tres per species)
      moves/      (one .tres per move)
      encounters/ (one .tres per area)
      TypeChart.tres
  assets/   (empty for v1; placeholder textures generated in code)
```

### Autoloads (singletons)

- `SceneManager`: scene swap with fade transition. Owns `current_scene` path.
- `GameState`: top-level game state (title / overworld / battle / dialog). Listens to signals from `BattleManager`, `DialogPlayer`.
- `Party`: wraps 6 `CreatureInstance` slots. Active index.
- `EncounterSystem`: holds the pending wild encounter when triggered; passes to `BattleManager`.
- `BattleManager`: owns battle state machine. Authoritative for combat logic. UI subscribes.
- `DialogPlayer`: pushes lines onto a `DialogBox` instance. Coroutine-driven typewriter text.

## 5. Data Model

### `CreatureType` (enum)

Six types: `Beast`, `Demon`, `Undead`, `Holy`, `Arcane`, `Elemental`. Each type has a representative placeholder color used in placeholder sprites and move icons.

### `MoveCategory` (enum)

`Physical`, `Special`, `Status`.

### `Stats` (struct)

Six ints: `hp`, `attack`, `defense`, `sp_atk`, `sp_def`, `speed`. Computed at instantiation time from species base + level.

### `CreatureSpecies` (Resource, `.tres`)

```
- id: StringName
- display_name: String
- types: CreatureType[]  // 1-2 entries
- base_stats: Stats
- learnset: Godot.Collections.Dictionary<int, StringName[]>  // level -> move ids
- front_sprite: Texture2D
- back_sprite: Texture2D
- placeholder_color: Color
```

### `MoveData` (Resource, `.tres`)

```
- id: StringName
- name: String
- type: CreatureType
- category: MoveCategory
- power: int  // 0 for status
- accuracy: int  // 0-100
- pp: int
- description: String
```

### `TypeChart` (single Resource, `TypeChart.tres`)

6×6 matrix of multipliers. Defaults: same type = 1.0 (STAB applied separately), neutral = 1.0, all others = 1.0 in v1. Tunable per-cell.

### `EncounterEntry` (Resource, `.tres`)

```
- species: CreatureSpecies
- level: int
- capture_eligible: bool  // true for wild, false for trainer
```

### `EncounterTable` (Resource, `.tres`)

```
- area_id: StringName
- entries: EncounterEntry[]
```

`EncounterTable` lists the possible encounters for an area. Selection is by designer placing `Enemy` nodes in the scene that each reference one entry. (No random rolling in v1.)

### `CreatureInstance` (plain C# class, runtime only)

```
- species: CreatureSpecies
- level: int
- experience: int
- current_hp: int
- moves: MoveData[4]
- status: CreatureStatus  // None for v1
```

### Stat computation

```
stat(other) = floor((2 * base + iv + floor(ev/4)) * level / 100) + 5
hp         = floor((2 * base + iv + floor(ev/4)) * level / 100) + level + 10
```

`iv` and `ev` are zero for v1. Field structure reserved in case they're added later.

### Experience

Simple: `exp_to_next = level^3`. Level up: restore HP to max, prompt to learn new move if slot free (else overwrite oldest).

## 6. Battle State Machine

`BattleManager` is the authority. `Battle.tscn` is the view; it listens to `BattleManager` signals.

### States

```
Intro
  → PlayerTurn ⇄ EnemyTurn (AI)
  → Resolving
  → TurnEnd
  → (if either side fainted) Fainted
       → (player) SwitchPrompt
       → (enemy) Victory
       → (player all dead) Defeat
  → (player ran) Escaped
```

- `Intro`: show enemy, play entry line, set turn order.
- `PlayerTurn`: open command menu (`Fight` / `Creature` / `Item` / `Run`).
  - `Fight` → move picker (filtered to known moves) → `Action_Player` queued.
  - `Creature` → open `PartyMenu` → switch active → end turn.
  - `Item` → open `ItemMenu` → apply (turn ends after use).
  - `Run` → 100% escape in v1 → `Escaped`.
- `EnemyTurn`: AI picks random move. Queued as `Action_Enemy`.
- Turn order: by `speed`. Higher goes first. Ties broken by random.
- `Resolving`: actions execute in order. Damage applied, type effectiveness logged, HP bars animated. No animations in v1 — direct tween.
- `TurnEnd`: status tick (no-op for v1), faint check.
- `Fainted`:
  - Enemy faint: experience awarded, level-up checks, then `Victory`.
  - Player faint: open `SwitchPrompt`. If party empty, `Defeat`.
- `Victory`: gain exp, return to overworld.
- `Defeat`: heal party to half-HP, return to last town.
- `Escaped`: return to overworld.

### Damage formula (Gen 3-ish)

```
dmg = ((2 * L / 5 + 2) * power * A / D / 50 + 2)
       * STAB * typeEff * random(0.85..1.0)
```

Where:
- `L` = attacker level
- `power` = move power
- `A` = attacker's relevant attack stat (Attack for Physical, Sp.Atk for Special)
- `D` = defender's relevant defense stat
- `STAB` = 1.5 if move type ∈ attacker types, else 1.0
- `typeEff` = lookup from `TypeChart`
- `random` = uniform in [0.85, 1.0]

Status moves skip damage; apply effect (no-op in v1).

### Capture flow (v1)

After battle, if enemy HP = 0 and `Enemy.capture_eligible = true`, open `CapturePrompt`:
- v1 ships with a single "Capture" action that succeeds if enemy HP = 0 and player has party space, else fails.
- On success: append `CreatureInstance` to `Party`. On fail: enemy escapes.

## 7. Overworld + Encounter Flow

- Top-down 2D, 16×16 tile grid, `TileMapLayer` for ground, decorations on a second layer.
- `Player` is a `CharacterBody2D` with an `AnimatedSprite2D` (4 frames × 4 directions = 16 frames total; placeholder colors).
- `Camera2D` follows player, clamped to map bounds.
- One overworld scene for v1: `Town` → `Route1` → `CaveEntrance` → `Cave` → `BossRoom`. Linear flow. No world map.
- **Enemies are visible, scene-placed sprites** (Pokémon Let's Go / Chrono Trigger style):
  - `Enemy` node = `CharacterBody2D` (or `StaticBody2D`) + `Sprite2D` + interaction `Area2D`
  - Each `Enemy` is a C# class extending `CharacterBody2D`, holding an `EncounterEntry` reference
  - On player collision OR `interact` button (e.g. `ui_accept`) while in range: emit `EncounterTriggered(enemy)`
  - `EncounterSystem` listens; on signal, builds `CreatureInstance` from entry, then calls `BattleManager.Start(enemy_instance)`
- One NPC in `Town` opens a `DialogBox` via `DialogPlayer`. Two-three placeholder lines introducing the player's quest.
- `SceneManager` handles the overworld ↔ battle swap with a 200ms fade.

## 8. UI

All UI is `Control` nodes with the default Godot theme in v1; theme override is a single-file swap later.

### Screens

- `TitleScreen.tscn`: title text + "New Game" button. Loads `Town` scene. No continue button in v1.
- `Overworld.tscn`: renders the active overworld map + `Player` + enemies + NPCs. No persistent HUD.
- `DialogBox.tscn`: portrait (placeholder square) + text + `▶` advance arrow. Emits `DialogFinished`. Shown/hidden by `DialogPlayer`.
- `Battle.tscn`: composed of:
  - `BattleBackground` (placeholder solid color)
  - `EnemyPanel`: front sprite + name + level label
  - `PlayerPanel`: back sprite + name + `HealthBar`
  - `ActionMenu`: 2×2 grid (`Fight`, `Creature`, `Item`, `Run`)
  - `MovePicker`: 2×2 list filtered to known moves, shows PP
  - `MessageLog`: bottom strip for combat text ("X used Y!", "It's super effective!")
- `PartyMenu.tscn`: 6-slot list, shows active marker, switch button.
- `ItemMenu.tscn`: simple list, single-use items in v1 (`Potion`: +20 HP, `Revive`: 50% HP from 0). Apply to active or selected slot.

### Wiring

`BattleManager` emits signals: `StateChanged(newState)`, `Message(text)`, `HpChanged(side, hp, hp_max)`, `ActionResolved`. `Battle.tscn` listens and updates UI. No business logic in UI nodes.

## 9. Asset / Placeholder Strategy

- All sprite slots are `Texture2D` properties. No code change required to swap to final art.
- `PlaceholderTexture` (C# static helper) generates:
  - `RectColor(w, h, color)` — solid rectangle
  - `LetterSprite(letter, color)` — letter on colored square
  - `CircleSprite(radius, color)` — filled circle
  - `Checkerboard(w, h, cell, c1, c2)` — debug pattern
- Each `CreatureSpecies.tres` gets a unique placeholder (color = species' primary type color, letter = first letter of name).
- Each `MoveData.tres` gets a placeholder icon (color = move's type color).
- Final sprite slot sizes (placeholders match exactly):
  - Overworld sprites: 16×16 (1× tile) or 32×32 (2× tile)
  - Battle front: 192×192
  - Battle back: 192×192
- Audio: silent. Audio bus layout is configured; dropping `.ogg` into `assets/audio/` works later.

## 10. Out-of-Scope (not in v1)

The following are not v1 work and do not need to be discussed again for v1:

- Save/load (session only)
- Abilities, held items
- Status effects (logic)
- Multiple bosses
- Side quests, dialogue branches
- World map, multiple regions
- Music / SFX
- Accessibility options
- Crafting, farming, day/night
- Evolution lines
- Multiplayer / trading
- Localization
- Mobile / web ports
- Modding hooks

## 11. Open Questions

1. **Capture trigger**: should the player choose to capture after every battle, or only from a menu in the overworld? v1 currently has post-battle `CapturePrompt` only.
2. **Defeat handling**: v1 heals party to half-HP and returns to town. Alternative: full wipe → respawn at title. Confirm preference.
3. **Number of starter creatures**: should the player choose 1 of 3 starters in `Town`, or receive 1 fixed starter? v1 default: receive 1 fixed starter.
4. **Creature list**: which 6–10 species ship in v1? Designer-driven; will be authored as `.tres` files in `resources/data/creatures/`.
5. **Move list**: how many moves total? Designer-driven; will be authored as `.tres` files in `resources/data/moves/`.

## 12. Approval

- Sections 1–7 of the brainstorming discussion were individually approved.
- Spec drafted and self-reviewed; awaiting user review.
