# AGENTS.md

2D creature-collecting RPG in Godot 4.6 (C#/.NET 8). Small vertical slice, single developer.

## Environment Quirks

- **No Godot editor in this env.** You cannot F5 or open the project UI. Runtime smoke tests are run by the user; you verify via `dotnet build` + `dotnet test` and the checklist in `SMOKE-TEST-F5.md`.
- **No CI workflows** (no `.github/`). All verification is local.
- **No README.md.** Specs/plans live in `docs/superpowers/`.
- **`RPG-Game.csproj.old`** is a stale leftover from the SDK bump (4.6.0 → 4.6.3). Leave it alone unless asked.

## Build & Test

```bash
# Build (from repo root, Windows PowerShell 5.1)
dotnet build rpg-game.sln -c Debug
# 0 errors, ~30 CS8618 nullable warnings tolerated (pre-existing pattern in Godot Resources)

# Test
dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj
# 18/18 expected (6 test files: Smoke, Stats, TypeEffectiveness, DamageCalculator, CreatureInstance, BattleStateMachine, BattleLog)

# Full runtime verification (user runs in Godot editor)
# See SMOKE-TEST-F5.md for the 13-point wiring checklist
```

Tests live in `tests/rpg-game.Tests/` and are excluded from the main project via `<DefaultItemExcludes>$(DefaultItemExcludes);tests/**/*</DefaultItemExcludes>` in `rpg-game.csproj`.

## Architecture

- **Solution:** `rpg-game.sln` (single project: `rpg-game.csproj`, AssemblyName `RPG-Game`).
- **Language split:** pure C# in `scripts/runtime/` + `scripts/battle/` (testable without Godot), Godot-dependent code in `scripts/autoload/`, `scripts/overworld/`, `scripts/ui/`, `scripts/data/` (Resources).
- **Namespace convention:** `rpg_game.scripts.{runtime,battle,autoload,overworld,ui,data,util}`. AssemblyName is `RPG-Game` (capital G) but root namespace is `rpg_game` (lowercase) — don't be confused.
- **6 autoloads** registered in `project.godot` (order matters: SceneManager, GameState, Party, EncounterSystem, BattleManager, DialogPlayer). Access via `/root/Foo` or `autoload.Foo` namespace. Each uses the `public static Instance { get; private set; }` singleton pattern.
- **Resource-driven data.** All game data lives in `resources/data/` as `.tres` (TypeChart, 8 species, 6 moves, 3 encounter tables, 6 single-entry). Never hardcode stats/types/encounters in C# — add/edit a `.tres`.
- **Scenes as composition roots.** `scenes/Player.tscn` is the canonical player prefab — instanced in Town, Route1, Cave, BossRoom. Don't re-inline Player nodes; use the prefab.
- **Main scene:** `uid://b0title001` → `scenes/TitleScreen.tscn`. New Game → `SceneManager.GotoOverworld()` → `Town.tscn` (NOT the unused `Overworld.tscn`).

## Key APIs (don't reinvent)

- `SceneManager.WarpTo(scenePath, targetPosition)` — fade + scene change + set `PendingPlayerPosition`. Player reads it in `_Ready` and teleports. Use for all scene transitions; do not call `GetTree().ChangeSceneToFile` directly.
- `BattleManager.StartWild(entry)` — `entry.Species.ToLite()` + `entry.Level` builds the enemy. Returns to Town on defeat (heals party to MaxHp/2), Overworld on victory/escape.
- `Party.Add(creature)` — auto-capture happens in `Overworld.OnBattleEnded` (1.5s after win, fainted enemy → new `CreatureInstance`).
- `EncounterSystem.EncounterTriggered` — fired by Enemy Area2D overlaps. `Overworld.cs` subscribes.
- `DialogPlayer.LineShown` / `DialogPlayer.DialogFinished` — consumed by `Overworld.cs` for NPC dialog flow.
- `WarpZone` Area2D component — drop in scene, set `TargetScenePath` + `TargetPosition` (+ optional `OneShot`). Triggers `SceneManager.WarpTo`.

## Conventions

- **Plan before code for non-trivial features.** This repo uses a brainstorming → spec → plan → subagent-execute flow. Specs at `docs/superpowers/specs/`, plans at `docs/superpowers/plans/`. For new features, follow the same pattern (see `using-superpowers` skill).
- **Scene file format:** Godot 4.6 `.tscn` files are hand-edited text — preferred over the editor when you know the structure. Always preserve the user's existing `format=4` + `unique_ids` style when editing their scenes.
- **Resource file format:** `.tres` is also hand-edited. Keep `ext_resource` IDs unique per file (e.g. `1_ow`, `2_db`, `3_pl`).
- **Scene transitions = `WarpTo`, not `ChangeSceneToFile`** (preserves PendingPlayerPosition + fade).
- **Battle: `Stats` is pure data, `Stats.Compute(base, level)` for level-scaled values.** `BattleStateMachine.cs` currently passes base stats directly — known P3 minor bug if you touch this.
- **SpriteFrames convention:** player uses `walk_{up,down,left,right}` and `idle_{up,down,left,right}` animation names. Add to `resources/images/actors/actors_1.tres` and assign once on the Sprite node in `scenes/Player.tscn` (all 4 instances inherit).

## Known Traps

- **`Player.SpritePath` must be `NodePath("Sprite")`** (the AnimatedSprite2D child). Was empty in early Town.tscn → silent animation failure. If walking sprites don't show, check this first.
- **Player.cs has leftover `GD.Print` debug logging** (lines 30, 76) from a prior debug commit. Don't add more — just remove these when you next touch the file.
- **`scenes/Overworld.tscn` is an empty unused template.** `SceneManager.GotoOverworld()` points at `Town.tscn` directly. Don't put content in Overworld.tscn expecting it to be loaded.
- **DefaultItemExcludes is `tests/**/*`** — if you add code under `tests/`, it won't be compiled into the main assembly. That's correct (tests project references the main project).
- **Nullable warnings CS8618** on Godot Resource fields are pre-existing and ignored. Don't fix them in unrelated PRs.

## Repo-Specific Workflow

- **Always work on a branch for new features or non-trivial fixes.** Branch name format: `feat/{slug}`, `fix/{slug}`, `refactor/{slug}`. Create the branch from `main` before the first commit, do the work, then merge back to `main` (fast-forward or `--no-ff`) when done. Don't commit feature work directly to `main` — it floods the history and makes the WIP mess worse if interrupted. Small one-line fixes or doc tweaks can go directly to `main`.
- **Plan format:** `docs/superpowers/plans/YYYY-MM-DD-{slug}.md` — sequence of small tasks, each scoped to one commit/agent. Follow this format if you write a new plan.
- **Test format:** NUnit 4 (`[TestFixture]`, `[Test]`). Pure C# only — don't put Godot scene code in unit tests; that's what the smoke test checklist is for.
- **Commit style:** short imperative subject, optional scope prefix (e.g. `feat(quest):`, `refactor(scene):`, `fix(player):`). Body only if commit is non-obvious.
- **Scene file UIDs:** use `uid://` only if you need a stable cross-scene reference (rare). Inline `[ext_resource type="PackedScene" path="res://..." id="X"]` is fine.
- **No audio, no save/load, no mobile/web, no localization, no evolution, no status effects, no quest log UI in v1** — explicit out-of-scope decisions. Don't add them unless asked.

## Where to Look First

- Spec: `docs/superpowers/specs/2026-06-14-rpg-game-design.md`
- Plans: `docs/superpowers/plans/*.md`
- Verification: `SMOKE-TEST-F5.md`
- Autoloads: `scripts/autoload/` (read these first to understand game state flow)
- Battle logic: `scripts/battle/` + `scripts/runtime/`
