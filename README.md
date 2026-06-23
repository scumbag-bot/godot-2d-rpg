# RPG Game

A small creature-collecting 2D RPG built in Godot 4.6 (C# / .NET 8). Vertical slice: one town, two routes, one boss. Pixel-art placeholders, gameplay-first.

## Quick start

Requires Godot 4.6 with .NET 8 SDK.

```bash
# Build (from repo root, PowerShell 5.1)
dotnet build rpg-game.sln -c Debug

# Run all tests
dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj

# Play: open project.godot in Godot editor, press F5.
```

Runtime smoke checks (since the F5 editor isn't available in all environments): `SMOKE-TEST-F5.md`.

## Godot AI MCP setup

The [Godot AI](https://github.com/hi-godot/godot-ai) plugin connects AI assistants (Claude Code, Codex, etc.) to a live Godot editor over MCP — inspect scenes, create nodes, modify properties, run tests, all from a prompt.

**Requirements:** Godot 4.3+, [uv](https://docs.astral.sh/uv/) (Python package manager), and an MCP client.

```powershell
# 1. Install uv (Windows PowerShell)
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"

# 2. Copy addon into project
git clone https://github.com/hi-godot/godot-ai.git $env:TEMP\godot-ai
Copy-Item -Recurse "$env:TEMP\godot-ai\addons\godot_ai" "addons\"
Remove-Item -Recurse $env:TEMP\godot-ai

# 3. Open project.godot, enable plugin: Project > Project Settings > Plugins > Godot AI
# 4. Pick MCP client in the Godot AI dock, press Configure
```

The addon is gitignored — each developer installs it locally.

## Tech stack

- **Engine:** Godot 4.6 (C# / .NET 8)
- **Test framework:** NUnit 4 — 25 tests, pure C# only (no Godot-runtime coupling)
- **Data layer:** Resource-driven (`.tres`), editor-editable
- **No save/load, no audio, no mobile/web** — explicit v1 out-of-scope

## Project layout

```
rpg-game/
  scripts/
    data/       Resource types (QuestData, CreatureSpecies, MoveData, etc.)
    runtime/    Pure C#, no Godot deps (Stats, CreatureInstance, QuestState)
    battle/     Pure C# battle logic (BattleStateMachine, DamageCalculator)
    autoload/   Godot autoloads (QuestStore, BattleManager, Party, etc.)
    overworld/  Scene-level scripts (Player, Npc, Enemy, TownStarter)
    ui/         UI control scripts (BattleScreen, DialogBox, ItemMenu, etc.)
  scenes/       .tscn files (Town, Battle, Route1, Cave, BossRoom, TitleScreen)
  resources/
    data/       .tres gameplay data (creatures, moves, encounters, quests)
    images/     Placeholder sprites + import metadata
    tilesets/   Tile atlases
  tests/        NUnit test project
  docs/
    specs/      Design specs
    plans/      Implementation plans
    guides/     How-to guides (see quest-authoring.md)
```

**Conventions:** see [AGENTS.md](AGENTS.md) for the full workflow guide (test placement, commit style, feature-branch rules, out-of-scope decisions).

## Game content (current state)

- **8 species** as `.tres` (Wolf, Imp, Bear, Sylph, Salamander, Wraith, Treant, Succubus)
- **6 moves** as `.tres` (Strike variants + elementals)
- **3 encounter tables** + 6 single-entry encounters
- **1 type chart** (6 types)
- **4 scenes** playable: Town, Route1, Cave, BossRoom
- **2 quests:**
  - `MainQuest.tres` — 6-stage main quest (intro → first_battle → elder_blessing → cave_enter → boss → complete)
  - `SampleQuest.tres` — 2-stage side quest (Bounty: Imp Trouble)

## Quest system

A linear quest framework with 3 advance triggers:

| Trigger | Target | Fires when |
|---|---|---|
| `NpcContact` | `TargetNpcId` | Player walks into an NPC whose `NpcId` matches, then the quest dialog finishes |
| `BattleWin` | `TargetSpeciesId` | Player wins a wild battle against the species |
| `AreaEnter` | `TargetAreaId` | **Not implemented in v1.** Placeholder for future WarpZone hook |

Add new quests by dropping a `.tres` file in `resources/data/quests/`. Full authoring guide: [`docs/guides/quest-authoring.md`](docs/guides/quest-authoring.md).

## Out of scope (v1)

Per the design spec, the following are intentionally NOT in v1:
- Save/load (session only)
- Abilities, held items, status effects
- Multiple bosses
- Side quests beyond the sample, dialogue branches
- Music / SFX
- Crafting, farming, day/night cycle
- Evolution lines
- Mobile / web ports

## License

Not yet chosen. Add one before going public.
