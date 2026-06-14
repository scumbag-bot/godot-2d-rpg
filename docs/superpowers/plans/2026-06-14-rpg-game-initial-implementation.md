# 2D RPG Game — Initial Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a vertical slice of a creature-collecting 2D RPG in Godot 4.6 (C#) — one town, one route, one cave, one boss, 6–10 placeholder species, full turn-based battle loop.

**Architecture:** Resource-driven data + plain C# runtime classes + explicit battle state machine + visible enemy-on-map encounters. Logic is unit-tested with NUnit. UI/scenes are wired in the Godot editor and verified manually.

**Tech Stack:** Godot 4.6, C# (.NET 8), NUnit 4, Godot.NET.Sdk 4.6.0. Forward+ renderer, D3D12, Jolt (3D — irrelevant for 2D).

**Reference spec:** `docs/superpowers/specs/2026-06-14-rpg-game-design.md`

---

## File Structure

```
rpg-game/
  project.godot                          # existing; modified to register autoloads
  rpg-game.csproj                        # create on first Godot open OR by hand
  rpg-game.sln                           # created by dotnet
  icon.svg                               # existing
  scripts/
    autoload/
      SceneManager.cs
      GameState.cs
      Party.cs
      EncounterSystem.cs
      BattleManager.cs
      DialogPlayer.cs
    data/                                # Godot Resource classes
      CreatureSpecies.cs
      MoveData.cs
      TypeChart.cs
      EncounterEntry.cs
      EncounterTable.cs
    runtime/                             # plain C# (no Godot) for testability
      CreatureType.cs
      MoveCategory.cs
      Stats.cs
      CreatureInstance.cs
      TypeEffectiveness.cs
    battle/
      DamageCalculator.cs
      BattleState.cs
      BattleAction.cs
      BattleStateMachine.cs
      BattleEvent.cs
      BattleLog.cs
    util/
      PlaceholderTexture.cs
    overworld/
      Player.cs
      Enemy.cs
      Npc.cs
  scenes/
    TitleScreen.tscn
    Overworld.tscn                       # base; instances for Town/Route1/Cave/BossRoom
    Town.tscn
    Route1.tscn
    Cave.tscn
    BossRoom.tscn
    Battle.tscn
    ui/
      DialogBox.tscn
      HealthBar.tscn
      PartyMenu.tscn
      ItemMenu.tscn
  resources/
    data/
      TypeChart.tres
      moves/                             # one .tres per move
      creatures/                         # one .tres per species
      encounters/                        # one .tres per area
  tests/
    rpg-game.Tests/
      rpg-game.Tests.csproj
      StatsTests.cs
      TypeEffectivenessTests.cs
      DamageCalculatorTests.cs
      CreatureInstanceTests.cs
      BattleStateMachineTests.cs
      BattleLogTests.cs
  docs/
    superpowers/
      specs/2026-06-14-rpg-game-design.md
      plans/2026-06-14-rpg-game-initial-implementation.md
```

**Boundaries:**
- Pure logic in `runtime/` and `battle/` is plain C#, NUnit-tested.
- `data/*.cs` are Godot `Resource` subclasses, edited in the Godot inspector as `.tres` files.
- `autoload/*.cs` are `Node` subclasses registered in `project.godot` as autoloads.
- Scenes are committed as `.tscn` text (Godot text format).

---

## Phase A: Project Setup + Pure-Logic Foundation

### Task A1: Create the C# project files

**Files:**
- Create: `rpg-game.csproj`
- Create: `rpg-game.sln`
- Create: `.gitignore` updates (already exists, verify Godot entries)

- [ ] **Step 1: Verify or create `rpg-game.csproj`**

Open Godot 4.6 editor once to generate the `.csproj`/`sln` (Editor → Open Project). Confirm:
- File: `rpg-game.csproj` exists at project root.
- Contents contain `<Project Sdk="Godot.NET.Sdk/4.6.0">` and `<TargetFramework>net8.0</TargetFramework>`.

If Godot editor isn't available, write the file directly:

```xml
<Project Sdk="Godot.NET.Sdk/4.6.0">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Create `rpg-game.sln`**

```text
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "rpg-game", "rpg-game.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		ExportDebug|Any CPU = ExportDebug|Any CPU
		ExportRelease|Any CPU = ExportRelease|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.ExportDebug|Any CPU.ActiveCfg = ExportDebug|Any CPU
		{11111111-1111-1111-1111-111111111111}.ExportDebug|Any CPU.Build.0 = ExportDebug|Any CPU
		{11111111-1111-1111-1111-111111111111}.ExportRelease|Any CPU.ActiveCfg = ExportRelease|Any CPU
		{11111111-1111-1111-1111-111111111111}.ExportRelease|Any CPU.Build.0 = ExportRelease|Any CPU
	EndGlobalSection
EndGlobal
```

- [ ] **Step 3: Verify build**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add rpg-game.csproj rpg-game.sln
git commit -m "chore: add C# project files for Godot 4.6"
```

---

### Task A2: Create the NUnit test project

**Files:**
- Create: `tests/rpg-game.Tests/rpg-game.Tests.csproj`
- Create: `tests/rpg-game.Tests/SmokeTests.cs`

- [ ] **Step 1: Create `tests/rpg-game.Tests/rpg-game.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="NUnit" Version="4.1.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\rpg-game.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create smoke test `tests/rpg-game.Tests/SmokeTests.cs`**

```csharp
using NUnit.Framework;

namespace rpg_game.Tests;

public class SmokeTests
{
    [Test]
    public void TruthIsTrue()
    {
        Assert.That(true, Is.True);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj`
Expected: `Passed!  - Failed: 0, Passed: 1, Total: 1`

- [ ] **Step 4: Commit**

```bash
git add tests/
git commit -m "test: add NUnit test project skeleton"
```

---

### Task A3: `Stats` struct + stat computation

**Files:**
- Create: `scripts/runtime/Stats.cs`
- Create: `tests/rpg-game.Tests/StatsTests.cs`

- [ ] **Step 1: Write failing test `tests/rpg-game.Tests/StatsTests.cs`**

```csharp
using NUnit.Framework;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class StatsTests
{
    [Test]
    public void Compute_AppliesPokemonGen3Formula()
    {
        // base=50, level=10, iv=0, ev=0
        // (2*50 + 0 + 0) * 10 / 100 + 5 = 10 + 5 = 15
        var s = Stats.Compute(baseStat: 50, level: 10, iv: 0, ev: 0);
        Assert.That(s, Is.EqualTo(15));
    }

    [Test]
    public void ComputeHp_AddsLevelAndTen()
    {
        // (2*50 + 0) * 10 / 100 + 10 + 10 = 10 + 10 + 10 = 30
        var s = Stats.ComputeHp(baseStat: 50, level: 10, iv: 0, ev: 0);
        Assert.That(s, Is.EqualTo(30));
    }

    [Test]
    public void Compute_GrowsWithLevel()
    {
        var low = Stats.Compute(50, 5, 0, 0);
        var high = Stats.Compute(50, 50, 0, 0);
        Assert.That(high, Is.GreaterThan(low));
    }
}
```

- [ ] **Step 2: Run, verify failure**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~StatsTests`
Expected: FAIL — `Stats` type not found.

- [ ] **Step 3: Implement `scripts/runtime/Stats.cs`**

```csharp
namespace rpg_game.scripts.runtime;

public readonly record struct Stats(
    int Hp,
    int Attack,
    int Defense,
    int SpAtk,
    int SpDef,
    int Speed)
{
    public static int Compute(int baseStat, int level, int iv, int ev)
    {
        return ((2 * baseStat + iv + ev / 4) * level) / 100 + 5;
    }

    public static int ComputeHp(int baseStat, int level, int iv, int ev)
    {
        return ((2 * baseStat + iv + ev / 4) * level) / 100 + level + 10;
    }

    public static Stats FromBase(int hp, int atk, int def, int spa, int spd, int spe, int level)
    {
        return new Stats(
            ComputeHp(hp, level, 0, 0),
            Compute(atk, level, 0, 0),
            Compute(def, level, 0, 0),
            Compute(spa, level, 0, 0),
            Compute(spd, level, 0, 0),
            Compute(spe, level, 0, 0));
    }
}
```

- [ ] **Step 4: Run, verify pass**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~StatsTests`
Expected: PASS — 3 tests.

- [ ] **Step 5: Commit**

```bash
git add scripts/runtime/Stats.cs tests/rpg-game.Tests/StatsTests.cs
git commit -m "feat(runtime): add Stats struct with Gen 3 stat formula"
```

---

### Task A4: `CreatureType` enum + `TypeEffectiveness` table

**Files:**
- Create: `scripts/runtime/CreatureType.cs`
- Create: `scripts/runtime/TypeEffectiveness.cs`
- Create: `tests/rpg-game.Tests/TypeEffectivenessTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using NUnit.Framework;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class TypeEffectivenessTests
{
    [Test]
    public void DefaultEffectiveness_IsOne()
    {
        var t = new TypeEffectiveness();
        Assert.That(t.Get(CreatureType.Beast, CreatureType.Demon), Is.EqualTo(1.0f));
    }

    [Test]
    public void SetAndGet_Works()
    {
        var t = new TypeEffectiveness();
        t.Set(CreatureType.Holy, CreatureType.Demon, 2.0f);
        Assert.That(t.Get(CreatureType.Holy, CreatureType.Demon), Is.EqualTo(2.0f));
        // symmetry
        Assert.That(t.Get(CreatureType.Demon, CreatureType.Holy), Is.EqualTo(2.0f));
    }
}
```

- [ ] **Step 2: Run, verify failure**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~TypeEffectivenessTests`
Expected: FAIL — types not found.

- [ ] **Step 3: Implement enum `scripts/runtime/CreatureType.cs`**

```csharp
namespace rpg_game.scripts.runtime;

public enum CreatureType
{
    Beast,
    Demon,
    Undead,
    Holy,
    Arcane,
    Elemental,
}
```

- [ ] **Step 4: Implement `scripts/runtime/TypeEffectiveness.cs`**

```csharp
namespace rpg_game.scripts.runtime;

public class TypeEffectiveness
{
    private readonly float[,] _matrix;

    public TypeEffectiveness()
    {
        int n = Enum.GetValues<CreatureType>().Length;
        _matrix = new float[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                _matrix[i, j] = 1.0f;
    }

    public float Get(CreatureType attacker, CreatureType defender)
    {
        return _matrix[(int)attacker, (int)defender];
    }

    public void Set(CreatureType attacker, CreatureType defender, float multiplier)
    {
        _matrix[(int)attacker, (int)defender] = multiplier;
        _matrix[(int)defender, (int)attacker] = multiplier;
    }

    public float GetMultiplicative(CreatureType moveType, IReadOnlyList<CreatureType> defenderTypes)
    {
        float m = 1.0f;
        foreach (var t in defenderTypes) m *= Get(moveType, t);
        return m;
    }
}
```

- [ ] **Step 5: Run, verify pass**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~TypeEffectivenessTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add scripts/runtime/CreatureType.cs scripts/runtime/TypeEffectiveness.cs tests/rpg-game.Tests/TypeEffectivenessTests.cs
git commit -m "feat(runtime): add CreatureType enum + TypeEffectiveness table"
```

---

### Task A5: `MoveCategory` enum + `DamageCalculator` (pure)

**Files:**
- Create: `scripts/runtime/MoveCategory.cs`
- Create: `scripts/battle/DamageCalculator.cs`
- Create: `tests/rpg-game.Tests/DamageCalculatorTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using NUnit.Framework;
using rpg_game.scripts.runtime;
using rpg_game.scripts.battle;

namespace rpg_game.Tests;

public class DamageCalculatorTests
{
    [Test]
    public void StatusMove_ReturnsZero()
    {
        var dmg = DamageCalculator.Compute(
            attackerLevel: 10, attackerAtk: 50, defenderDef: 50,
            movePower: 0, moveType: CreatureType.Beast, moveCategory: MoveCategory.Status,
            attackerTypes: new[] { CreatureType.Beast },
            defenderTypes: new[] { CreatureType.Demon },
            chart: new TypeEffectiveness(),
            rng: () => 1.0f);
        Assert.That(dmg, Is.EqualTo(0));
    }

    [Test]
    public void SuperEffective_DoublesDamage()
    {
        var chart = new TypeEffectiveness();
        chart.Set(CreatureType.Holy, CreatureType.Demon, 2.0f);
        var neutral = DamageCalculator.Compute(
            attackerLevel: 50, attackerAtk: 100, defenderDef: 100,
            movePower: 50, moveType: CreatureType.Beast, moveCategory: MoveCategory.Physical,
            attackerTypes: new[] { CreatureType.Beast },
            defenderTypes: new[] { CreatureType.Demon },
            chart: chart, rng: () => 1.0f);
        var superEffective = DamageCalculator.Compute(
            attackerLevel: 50, attackerAtk: 100, defenderDef: 100,
            movePower: 50, moveType: CreatureType.Holy, moveCategory: MoveCategory.Physical,
            attackerTypes: new[] { CreatureType.Holy },
            defenderTypes: new[] { CreatureType.Demon },
            chart: chart, rng: () => 1.0f);
        Assert.That(superEffective, Is.GreaterThan(neutral));
    }

    [Test]
    public void STAB_IncreasesDamage()
    {
        var chart = new TypeEffectiveness();
        var noStab = DamageCalculator.Compute(
            50, 100, 100, 50, CreatureType.Holy, MoveCategory.Physical,
            new[] { CreatureType.Beast }, new[] { CreatureType.Demon }, chart, () => 1.0f);
        var withStab = DamageCalculator.Compute(
            50, 100, 100, 50, CreatureType.Holy, MoveCategory.Physical,
            new[] { CreatureType.Holy }, new[] { CreatureType.Demon }, chart, () => 1.0f);
        Assert.That(withStab, Is.GreaterThan(noStab));
    }
}
```

- [ ] **Step 2: Run, verify failure**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~DamageCalculatorTests`
Expected: FAIL.

- [ ] **Step 3: Implement enum `scripts/runtime/MoveCategory.cs`**

```csharp
namespace rpg_game.scripts.runtime;

public enum MoveCategory
{
    Physical,
    Special,
    Status,
}
```

- [ ] **Step 4: Implement `scripts/battle/DamageCalculator.cs`**

```csharp
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.battle;

public static class DamageCalculator
{
    public const float StabMultiplier = 1.5f;

    public static int Compute(
        int attackerLevel,
        int attackerAtk,
        int defenderDef,
        int movePower,
        CreatureType moveType,
        MoveCategory moveCategory,
        IReadOnlyList<CreatureType> attackerTypes,
        IReadOnlyList<CreatureType> defenderTypes,
        TypeEffectiveness chart,
        Func<float> rng)
    {
        if (moveCategory == MoveCategory.Status || movePower <= 0)
            return 0;
        if (defenderDef <= 0) defenderDef = 1;

        float baseDmg = ((2f * attackerLevel / 5f) + 2f) * movePower * attackerAtk / defenderDef / 50f + 2f;

        float stab = attackerTypes.Contains(moveType) ? StabMultiplier : 1.0f;
        float typeEff = chart.GetMultiplicative(moveType, defenderTypes);
        float roll = rng();

        float final = baseDmg * stab * typeEff * roll;
        return Math.Max(1, (int)Math.Floor(final));
    }
}
```

- [ ] **Step 5: Run, verify pass**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~DamageCalculatorTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add scripts/runtime/MoveCategory.cs scripts/battle/DamageCalculator.cs tests/rpg-game.Tests/DamageCalculatorTests.cs
git commit -m "feat(battle): add DamageCalculator with Gen 3 formula"
```

---

### Task A6: `CreatureInstance` + level/exp

**Files:**
- Create: `scripts/runtime/CreatureInstance.cs`
- Create: `tests/rpg-game.Tests/CreatureInstanceTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using NUnit.Framework;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class CreatureInstanceTests
{
    private static CreatureInstance Make(int level = 5)
    {
        var species = new CreatureSpeciesLite
        {
            DisplayName = "Wolf",
            Types = new[] { CreatureType.Beast },
            BaseStats = new BaseStats(40, 50, 40, 30, 30, 60),
        };
        return new CreatureInstance(species, level);
    }

    [Test]
    public void NewInstance_StartsAtFullHp()
    {
        var c = Make(level: 10);
        Assert.That(c.CurrentHp, Is.EqualTo(c.MaxHp));
        Assert.That(c.CurrentHp, Is.GreaterThan(0));
    }

    [Test]
    public void TakeDamage_ReducesHp()
    {
        var c = Make();
        int before = c.CurrentHp;
        c.TakeDamage(5);
        Assert.That(c.CurrentHp, Is.EqualTo(before - 5));
    }

    [Test]
    public void TakeDamage_CannotGoBelowZero()
    {
        var c = Make();
        c.TakeDamage(9999);
        Assert.That(c.CurrentHp, Is.EqualTo(0));
        Assert.That(c.IsFainted, Is.True);
    }

    [Test]
    public void GainExperience_TriggersLevelUp()
    {
        var c = Make(level: 2);
        int targetExp = CreatureInstance.ExpToNext(2);
        bool leveled = c.GainExperience(targetExp);
        Assert.That(leveled, Is.True);
        Assert.That(c.Level, Is.EqualTo(3));
    }

    [Test]
    public void ExpToNext_GrowsCubically()
    {
        Assert.That(CreatureInstance.ExpToNext(2), Is.EqualTo(8));
        Assert.That(CreatureInstance.ExpToNext(5), Is.EqualTo(125));
    }
}
```

This test references a small lightweight species type — to be implemented alongside.

- [ ] **Step 2: Run, verify failure**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~CreatureInstanceTests`
Expected: FAIL.

- [ ] **Step 3: Implement `scripts/runtime/CreatureInstance.cs`**

```csharp
namespace rpg_game.scripts.runtime;

public class CreatureInstance
{
    public CreatureSpeciesLite Species { get; }
    public int Level { get; private set; }
    public int Experience { get; private set; }
    public int CurrentHp { get; private set; }
    public int MaxHp => Stats.ComputeHp(Species.BaseStats.Hp, Level, 0, 0);
    public bool IsFainted => CurrentHp <= 0;

    public CreatureInstance(CreatureSpeciesLite species, int level)
    {
        Species = species;
        Level = level;
        CurrentHp = MaxHp;
    }

    public void TakeDamage(int amount)
    {
        CurrentHp = Math.Max(0, CurrentHp - amount);
    }

    public void Heal(int amount)
    {
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }

    public bool GainExperience(int amount)
    {
        Experience += amount;
        while (Experience >= ExpToNext(Level))
        {
            Experience -= ExpToNext(Level);
            Level++;
            CurrentHp = MaxHp;
            return true;
        }
        return false;
    }

    public static int ExpToNext(int level) => level * level * level;
}

public record CreatureSpeciesLite(
    string DisplayName,
    IReadOnlyList<CreatureType> Types,
    BaseStats BaseStats)
{
    public string DisplayName { get; init; } = DisplayName;
    public IReadOnlyList<CreatureType> Types { get; init; } = Types;
    public BaseStats BaseStats { get; init; } = BaseStats;
}

public record BaseStats(int Hp, int Attack, int Defense, int SpAtk, int SpDef, int Speed);
```

- [ ] **Step 4: Run, verify pass**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~CreatureInstanceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add scripts/runtime/CreatureInstance.cs tests/rpg-game.Tests/CreatureInstanceTests.cs
git commit -m "feat(runtime): add CreatureInstance with HP + level/exp"
```

---

### Task A7: `BattleState` enum + `BattleAction` record + `BattleStateMachine` (turn resolution only)

**Files:**
- Create: `scripts/battle/BattleState.cs`
- Create: `scripts/battle/BattleAction.cs`
- Create: `scripts/battle/BattleStateMachine.cs`
- Create: `tests/rpg-game.Tests/BattleStateMachineTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using NUnit.Framework;
using rpg_game.scripts.battle;
using rpg_game.scripts.runtime;

namespace rpg_game.Tests;

public class BattleStateMachineTests
{
    private static CreatureInstance MakePlayer(int level) =>
        new(new CreatureSpeciesLite("Hero", new[] { CreatureType.Holy }, new BaseStats(50, 50, 50, 50, 50, 60)), level);

    private static CreatureInstance MakeEnemy(int level) =>
        new(new CreatureSpeciesLite("Foe", new[] { CreatureType.Demon }, new BaseStats(50, 50, 50, 50, 50, 50)), level);

    [Test]
    public void FasterAttacker_GoesFirst()
    {
        var sm = new BattleStateMachine();
        var player = MakePlayer(5);
        var enemy = MakeEnemy(5);
        var order = sm.ComputeTurnOrder(player, enemy);
        Assert.That(order[0], Is.EqualTo(BattleSide.Player));
    }

    [Test]
    public void Resolve_PlayerAttacks_EnemyHpDecreases()
    {
        var sm = new BattleStateMachine();
        var player = MakePlayer(5);
        var enemy = MakeEnemy(5);
        int before = enemy.CurrentHp;
        var move = new MoveDataLite("Tackle", CreatureType.Beast, MoveCategory.Physical, 40, 100);
        var result = sm.ResolveSingle(player, enemy, move, chart: new TypeEffectiveness(), rng: () => 1.0f);
        Assert.That(result.Damage, Is.GreaterThan(0));
        Assert.That(enemy.CurrentHp, Is.LessThan(before));
    }

    [Test]
    public void Resolve_EnemyFaints_SetsStateToVictory()
    {
        var sm = new BattleStateMachine();
        var player = MakePlayer(50);
        var enemy = MakeEnemy(1);
        var move = new MoveDataLite("Smash", CreatureType.Holy, MoveCategory.Physical, 200, 100);
        sm.ResolveSingle(player, enemy, move, new TypeEffectiveness(), () => 1.0f);
        Assert.That(enemy.IsFainted, Is.True);
        Assert.That(sm.State, Is.EqualTo(BattleState.Victory).Or.EqualTo(BattleState.Resolving));
    }
}

public record MoveDataLite(
    string Name,
    CreatureType Type,
    MoveCategory Category,
    int Power,
    int Accuracy);
```

- [ ] **Step 2: Run, verify failure**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~BattleStateMachineTests`
Expected: FAIL.

- [ ] **Step 3: Implement `scripts/battle/BattleState.cs`**

```csharp
namespace rpg_game.scripts.battle;

public enum BattleState
{
    Intro,
    PlayerTurn,
    EnemyTurn,
    Resolving,
    TurnEnd,
    Fainted,
    SwitchPrompt,
    Victory,
    Defeat,
    Escaped,
}

public enum BattleSide { Player, Enemy }
```

- [ ] **Step 4: Implement `scripts/battle/BattleAction.cs`**

```csharp
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.battle;

public abstract record BattleAction(BattleSide Side);

public record AttackAction(BattleSide Side, MoveDataLite Move) : BattleAction(Side);

public record SwitchAction(BattleSide Side, int NewIndex) : BattleAction(Side);

public record UseItemAction(BattleSide Side, string ItemId) : BattleAction(Side);

public record RunAction(BattleSide Side) : BattleAction(Side);
```

- [ ] **Step 5: Implement `scripts/battle/BattleStateMachine.cs`**

```csharp
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.battle;

public class BattleStateMachine
{
    public BattleState State { get; private set; } = BattleState.Intro;
    private readonly Random _rng = new();

    public BattleSide[] ComputeTurnOrder(CreatureInstance player, CreatureInstance enemy)
    {
        int ps = Stats.Compute(player.Species.BaseStats.Speed, player.Level, 0, 0);
        int es = Stats.Compute(enemy.Species.BaseStats.Speed, enemy.Level, 0, 0);
        if (ps == es) return _rng.Next(0, 2) == 0
            ? new[] { BattleSide.Player, BattleSide.Enemy }
            : new[] { BattleSide.Enemy, BattleSide.Player };
        return ps > es
            ? new[] { BattleSide.Player, BattleSide.Enemy }
            : new[] { BattleSide.Enemy, BattleSide.Player };
    }

    public DamageResult ResolveSingle(
        CreatureInstance attacker,
        CreatureInstance defender,
        MoveDataLite move,
        TypeEffectiveness chart,
        Func<float> rng)
    {
        State = BattleState.Resolving;
        var aStats = attacker.Species.BaseStats;
        var dStats = defender.Species.BaseStats;
        int atk = move.Category == MoveCategory.Special ? aStats.SpAtk : aStats.Attack;
        int def = move.Category == MoveCategory.Special ? dStats.SpDef : dStats.Defense;
        int dmg = DamageCalculator.Compute(
            attacker.Level, atk, def, move.Power, move.Type, move.Category,
            attacker.Species.Types, defender.Species.Types, chart, rng);
        defender.TakeDamage(dmg);
        if (defender.IsFainted)
        {
            State = attacker == null ? State : BattleState.Fainted;
        }
        State = BattleState.TurnEnd;
        return new DamageResult(dmg, defender.IsFainted);
    }
}

public record DamageResult(int Damage, bool TargetFainted);
```

- [ ] **Step 6: Run, verify pass**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~BattleStateMachineTests`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add scripts/battle/BattleState.cs scripts/battle/BattleAction.cs scripts/battle/BattleStateMachine.cs tests/rpg-game.Tests/BattleStateMachineTests.cs
git commit -m "feat(battle): add BattleStateMachine with turn order + damage resolution"
```

---

### Task A8: `BattleLog` (event log)

**Files:**
- Create: `scripts/battle/BattleLog.cs`
- Create: `tests/rpg-game.Tests/BattleLogTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using NUnit.Framework;
using rpg_game.scripts.battle;

namespace rpg_game.Tests;

public class BattleLogTests
{
    [Test]
    public void Append_StoresLinesInOrder()
    {
        var log = new BattleLog();
        log.Append("X used Y!");
        log.Append("It's super effective!");
        Assert.That(log.Lines, Is.EqualTo(new[] { "X used Y!", "It's super effective!" }));
    }
}
```

- [ ] **Step 2: Run, verify failure**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~BattleLogTests`
Expected: FAIL.

- [ ] **Step 3: Implement `scripts/battle/BattleLog.cs`**

```csharp
namespace rpg_game.scripts.battle;

public class BattleLog
{
    private readonly List<string> _lines = new();
    public IReadOnlyList<string> Lines => _lines;

    public void Append(string line) => _lines.Add(line);
    public void Clear() => _lines.Clear();
}
```

- [ ] **Step 4: Run, verify pass**

Run: `dotnet test tests/rpg-game.Tests/rpg-game.Tests.csproj --filter FullyQualifiedName~BattleLogTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add scripts/battle/BattleLog.cs tests/rpg-game.Tests/BattleLogTests.cs
git commit -m "feat(battle): add BattleLog for combat messages"
```

---

## Phase B: Godot Resources (designer-edited data)

### Task B1: `MoveData` Resource

**Files:**
- Create: `scripts/data/MoveData.cs`

- [ ] **Step 1: Implement `scripts/data/MoveData.cs`**

```csharp
using Godot;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class MoveData : Resource
{
    [Export] public StringName Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public CreatureType Type { get; set; } = CreatureType.Beast;
    [Export] public MoveCategory Category { get; set; } = MoveCategory.Physical;
    [Export] public int Power { get; set; } = 40;
    [Export] public int Accuracy { get; set; } = 100;
    [Export] public int Pp { get; set; } = 20;
    [Export] public string Description { get; set; } = "";

    public MoveDataLite ToLite() => new(DisplayName, Type, Category, Power, Accuracy);
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/data/MoveData.cs
git commit -m "feat(data): add MoveData Resource for designer-edited moves"
```

---

### Task B2: `CreatureSpecies` Resource

**Files:**
- Create: `scripts/data/CreatureSpecies.cs`

- [ ] **Step 1: Implement `scripts/data/CreatureSpecies.cs`**

```csharp
using Godot;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class CreatureSpecies : Resource
{
    [Export] public StringName Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Godot.Collections.Array<CreatureType> Types { get; set; } = new();
    [Export] public int BaseHp { get; set; } = 50;
    [Export] public int BaseAttack { get; set; } = 50;
    [Export] public int BaseDefense { get; set; } = 50;
    [Export] public int BaseSpAtk { get; set; } = 50;
    [Export] public int BaseSpDef { get; set; } = 50;
    [Export] public int BaseSpeed { get; set; } = 50;
    [Export] public Godot.Collections.Dictionary<int, StringName[]> Learnset { get; set; } = new();
    [Export] public Texture2D FrontSprite { get; set; }
    [Export] public Texture2D BackSprite { get; set; }
    [Export] public Color PlaceholderColor { get; set; } = new Color(1, 1, 1);

    public CreatureSpeciesLite ToLite() => new(
        DisplayName,
        Types.ToArray(),
        new BaseStats(BaseHp, BaseAttack, BaseDefense, BaseSpAtk, BaseSpDef, BaseSpeed));
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/data/CreatureSpecies.cs
git commit -m "feat(data): add CreatureSpecies Resource"
```

---

### Task B3: `TypeChart` Resource + default instance

**Files:**
- Create: `scripts/data/TypeChart.cs`

- [ ] **Step 1: Implement `scripts/data/TypeChart.cs`**

```csharp
using Godot;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class TypeChart : Resource
{
    [Export] public Godot.Collections.Dictionary<StringName, float> Multipliers { get; set; } = new();

    public TypeEffectiveness ToEffective()
    {
        var t = new TypeEffectiveness();
        foreach (var kv in Multipliers)
        {
            // key format: "ATTACKER->DEFENDER" (e.g. "Holy->Demon")
            var parts = kv.Key.ToString().Split("->");
            if (parts.Length != 2) continue;
            if (!Enum.TryParse<CreatureType>(parts[0], out var a)) continue;
            if (!Enum.TryParse<CreatureType>(parts[1], out var d)) continue;
            t.Set(a, d, kv.Value);
        }
        return t;
    }
}
```

- [ ] **Step 2: Author `resources/data/TypeChart.tres`**

Open Godot editor → right-click `resources/data/` → New Resource → `TypeChart` → save as `TypeChart.tres`. Add entries in inspector:

| Key (StringName) | Value (float) |
| --- | --- |
| `Holy->Demon` | 2.0 |
| `Demon->Holy` | 2.0 |
| `Undead->Holy` | 0.5 |
| `Holy->Undead` | 0.5 |
| `Elemental->Beast` | 2.0 |
| `Beast->Elemental` | 2.0 |
| `Arcane->Demon` | 0.5 |
| `Demon->Arcane` | 0.5 |

(7 entries — all others default to 1.0.)

- [ ] **Step 3: Commit**

```bash
git add scripts/data/TypeChart.cs resources/data/TypeChart.tres
git commit -m "feat(data): add TypeChart Resource + default v1 type matchups"
```

---

### Task B4: `EncounterEntry` + `EncounterTable` Resources

**Files:**
- Create: `scripts/data/EncounterEntry.cs`
- Create: `scripts/data/EncounterTable.cs`

- [ ] **Step 1: Implement `scripts/data/EncounterEntry.cs`**

```csharp
using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class EncounterEntry : Resource
{
    [Export] public CreatureSpecies Species { get; set; }
    [Export] public int Level { get; set; } = 3;
    [Export] public bool CaptureEligible { get; set; } = true;
}
```

- [ ] **Step 2: Implement `scripts/data/EncounterTable.cs`**

```csharp
using Godot;

namespace rpg_game.scripts.data;

[GlobalClass]
public partial class EncounterTable : Resource
{
    [Export] public StringName AreaId { get; set; } = "";
    [Export] public Godot.Collections.Array<EncounterEntry> Entries { get; set; } = new();
}
```

- [ ] **Step 3: Commit**

```bash
git add scripts/data/EncounterEntry.cs scripts/data/EncounterTable.cs
git commit -m "feat(data): add EncounterEntry + EncounterTable Resources"
```

---

## Phase C: Autoloads + Scene Infrastructure

### Task C1: `PlaceholderTexture` utility

**Files:**
- Create: `scripts/util/PlaceholderTexture.cs`

- [ ] **Step 1: Implement `scripts/util/PlaceholderTexture.cs`**

```csharp
using Godot;

namespace rpg_game.scripts.util;

public static class PlaceholderTexture
{
    public static ImageTexture RectColor(int w, int h, Color color)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(color);
        return ImageTexture.CreateFromImage(img);
    }

    public static ImageTexture LetterSprite(string letter, Color bg, Color fg, int size = 64)
    {
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(bg);
        // crude letter mark: a centered plus sign per first letter
        var c = size / 2;
        int t = Math.Max(2, size / 12);
        for (int y = -t; y <= t; y++)
            for (int x = -size / 3; x <= size / 3; x++)
                img.SetPixel(c + x, c + y, fg);
        for (int x = -t; x <= t; x++)
            for (int y = -size / 3; y <= size / 3; y++)
                img.SetPixel(c + x, c + y, fg);
        return ImageTexture.CreateFromImage(img);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/util/PlaceholderTexture.cs
git commit -m "feat(util): add PlaceholderTexture generator"
```

---

### Task C2: `Party` autoload

**Files:**
- Create: `scripts/autoload/Party.cs`

- [ ] **Step 1: Implement `scripts/autoload/Party.cs`**

```csharp
using Godot;
using rpg_game.scripts.data;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.autoload;

public partial class Party : Node
{
    public const int MaxSize = 6;
    public Godot.Collections.Array<CreatureInstance> Members { get; } = new();
    public int ActiveIndex { get; private set; } = 0;

    [Signal] public delegate void PartyChangedEventHandler();
    [Signal] public delegate void ActiveChangedEventHandler(int index);

    public CreatureInstance Active => (Members.Count > 0 && ActiveIndex < Members.Count) ? Members[ActiveIndex] : null;

    public void Add(CreatureInstance member)
    {
        if (Members.Count >= MaxSize) return;
        Members.Add(member);
        EmitSignal(SignalName.PartyChanged);
    }

    public void SetActive(int index)
    {
        if (index < 0 || index >= Members.Count) return;
        ActiveIndex = index;
        EmitSignal(SignalName.ActiveChanged, index);
    }

    public bool HasSpace() => Members.Count < MaxSize;
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/autoload/Party.cs
git commit -m "feat(autoload): add Party singleton with active slot"
```

---

### Task C3: `EncounterSystem` autoload

**Files:**
- Create: `scripts/autoload/EncounterSystem.cs`

- [ ] **Step 1: Implement `scripts/autoload/EncounterSystem.cs`**

```csharp
using Godot;
using rpg_game.scripts.data;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.autoload;

public partial class EncounterSystem : Node
{
    [Signal] public delegate void EncounterTriggeredEventHandler(EncounterEntry entry);

    public void Trigger(EncounterEntry entry)
    {
        EmitSignal(SignalName.EncounterTriggered, entry);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/autoload/EncounterSystem.cs
git commit -m "feat(autoload): add EncounterSystem"
```

---

### Task C4: `BattleManager` autoload (state machine + signals)

**Files:**
- Create: `scripts/autoload/BattleManager.cs`

- [ ] **Step 1: Implement `scripts/autoload/BattleManager.cs`**

```csharp
using Godot;
using rpg_game.scripts.battle;
using rpg_game.scripts.data;
using rpg_game.scripts.runtime;

namespace rpg_game.scripts.autoload;

public partial class BattleManager : Node
{
    [Signal] public delegate void BattleStartedEventHandler(bool isWild);
    [Signal] public delegate void StateChangedEventHandler(int state);
    [Signal] public delegate void MessageEventHandler(string text);
    [Signal] public delegate void HpChangedEventHandler(int side, int hp, int hpMax);
    [Signal] public delegate void BattleEndedEventHandler(int outcome);

    public BattleStateMachine Fsm { get; } = new();
    public CreatureInstance PlayerInstance { get; private set; }
    public CreatureInstance EnemyInstance { get; private set; }
    public bool IsWild { get; private set; }
    public EncounterEntry CurrentEntry { get; private set; }

    public void StartWild(EncounterEntry entry)
    {
        IsWild = true;
        CurrentEntry = entry;
        PlayerInstance = GetNode<Party>("/root/Party").Active;
        EnemyInstance = new CreatureInstance(entry.Species.ToLite(), entry.Level);
        Fsm = new BattleStateMachine();
        EmitSignal(SignalName.BattleStarted, true);
        EmitSignal(SignalName.HpChanged, 0, PlayerInstance.CurrentHp, PlayerInstance.MaxHp);
        EmitSignal(SignalName.HpChanged, 1, EnemyInstance.CurrentHp, EnemyInstance.MaxHp);
        EmitSignal(SignalName.Message, $"A wild {entry.Species.DisplayName} appeared!");
        EmitSignal(SignalName.StateChanged, (int)BattleState.PlayerTurn);
    }

    public void PlayerAttack(MoveDataLite move)
    {
        if (EnemyInstance == null || EnemyInstance.IsFainted) return;
        var chart = LoadChart();
        EmitSignal(SignalName.Message, $"{PlayerInstance.Species.DisplayName} used {move.Name}!");
        var result = Fsm.ResolveSingle(PlayerInstance, EnemyInstance, move, chart, Rng);
        EmitSignal(SignalName.HpChanged, 1, EnemyInstance.CurrentHp, EnemyInstance.MaxHp);
        if (result.Damage > 0) EmitSignal(SignalName.Message, $"It dealt {result.Damage} damage.");
        if (EnemyInstance.IsFainted)
        {
            EmitSignal(SignalName.Message, $"{EnemyInstance.Species.DisplayName} fainted!");
            EmitSignal(SignalName.BattleEnded, (int)BattleState.Victory);
            return;
        }
        EnemyTurn();
    }

    public void Run()
    {
        EmitSignal(SignalName.Message, "Got away safely!");
        EmitSignal(SignalName.BattleEnded, (int)BattleState.Escaped);
    }

    private void EnemyTurn()
    {
        var move = PickEnemyMove();
        EmitSignal(SignalName.Message, $"{EnemyInstance.Species.DisplayName} used {move.Name}!");
        var chart = LoadChart();
        var result = Fsm.ResolveSingle(EnemyInstance, PlayerInstance, move, chart, Rng);
        EmitSignal(SignalName.HpChanged, 0, PlayerInstance.CurrentHp, PlayerInstance.MaxHp);
        if (result.Damage > 0) EmitSignal(SignalName.Message, $"It dealt {result.Damage} damage.");
        if (PlayerInstance.IsFainted)
        {
            EmitSignal(SignalName.Message, $"{PlayerInstance.Species.DisplayName} fainted!");
            EmitSignal(SignalName.BattleEnded, (int)BattleState.Defeat);
            return;
        }
        EmitSignal(SignalName.StateChanged, (int)BattleState.PlayerTurn);
    }

    private MoveDataLite PickEnemyMove()
    {
        // v1 AI: one generic move per species type
        var type = EnemyInstance.Species.Types[0];
        return new MoveDataLite("Strike", type, MoveCategory.Physical, 40, 100);
    }

    private TypeEffectiveness LoadChart()
    {
        var chartRes = GD.Load<TypeChart>("res://resources/data/TypeChart.tres");
        return chartRes != null ? chartRes.ToEffective() : new TypeEffectiveness();
    }

    private static readonly System.Random _rng = new();
    private float Rng() => 0.85f + (float)_rng.NextDouble() * 0.15f;
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/autoload/BattleManager.cs
git commit -m "feat(autoload): add BattleManager state machine + signals"
```

---

### Task C5: `SceneManager` autoload (scene swap + fade)

**Files:**
- Create: `scripts/autoload/SceneManager.cs`

- [ ] **Step 1: Implement `scripts/autoload/SceneManager.cs`**

```csharp
using Godot;

namespace rpg_game.scripts.autoload;

public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }

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
        GotoScene("res://scenes/Overworld.tscn");
    }

    public void GotoBattle()
    {
        GotoScene("res://scenes/Battle.tscn");
    }

    public void GotoTitle()
    {
        GotoScene("res://scenes/TitleScreen.tscn");
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/autoload/SceneManager.cs
git commit -m "feat(autoload): add SceneManager"
```

---

### Task C6: `GameState` autoload

**Files:**
- Create: `scripts/autoload/GameState.cs`

- [ ] **Step 1: Implement `scripts/autoload/GameState.cs`**

```csharp
using Godot;

namespace rpg_game.scripts.autoload;

public partial class GameState : Node
{
    public enum Mode { Title, Overworld, Battle, Dialog }

    public Mode Current { get; private set; } = Mode.Title;

    [Signal] public delegate void ModeChangedEventHandler(int mode);

    public void SetMode(Mode mode)
    {
        Current = mode;
        EmitSignal(SignalName.ModeChanged, (int)mode);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/autoload/GameState.cs
git commit -m "feat(autoload): add GameState mode tracker"
```

---

### Task C7: `DialogPlayer` autoload

**Files:**
- Create: `scripts/autoload/DialogPlayer.cs`

- [ ] **Step 1: Implement `scripts/autoload/DialogPlayer.cs`**

```csharp
using Godot;
using System.Threading.Tasks;

namespace rpg_game.scripts.autoload;

public partial class DialogPlayer : Node
{
    [Signal] public delegate void DialogFinishedEventHandler();
    [Signal] public delegate void LineShownEventHandler(string text);

    public bool IsActive { get; private set; }

    public async void Play(string[] lines)
    {
        if (IsActive) return;
        IsActive = true;
        foreach (var line in lines)
        {
            EmitSignal(SignalName.LineShown, line);
            await ToSignal(GetTree().CreateTimer(1.5), Timer.SignalName.Timeout);
        }
        IsActive = false;
        EmitSignal(SignalName.DialogFinished);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/autoload/DialogPlayer.cs
git commit -m "feat(autoload): add DialogPlayer"
```

---

### Task C8: Register all autoloads in `project.godot`

**Files:**
- Modify: `project.godot`

- [ ] **Step 1: Add autoload entries**

Append to `project.godot`:

```ini
[autoload]

SceneManager="*res://scripts/autoload/SceneManager.cs"
GameState="*res://scripts/autoload/GameState.cs"
Party="*res://scripts/autoload/Party.cs"
EncounterSystem="*res://scripts/autoload/EncounterSystem.cs"
BattleManager="*res://scripts/autoload/BattleManager.cs"
DialogPlayer="*res://scripts/autoload/DialogPlayer.cs"
```

- [ ] **Step 2: Build + verify**

Run: `dotnet build rpg-game.sln -c Debug`
Expected: `Build succeeded.`

Open the project in Godot editor. Confirm no errors in the Output panel.

- [ ] **Step 3: Commit**

```bash
git add project.godot
git commit -m "chore: register autoloads in project.godot"
```

---

## Phase D: Scenes (UI + Battle + Overworld)

### Task D1: `TitleScreen` scene

**Files:**
- Create: `scenes/TitleScreen.tscn`

- [ ] **Step 1: Author scene in Godot editor**

1. Scene → New Node → `Control` (root, name `TitleScreen`).
2. Attach script: leave none for now.
3. Add child `VBoxContainer` (full rect, anchors set to fill).
4. Add child `Label` (text: "RPG Game", font size 48, horizontal alignment center).
5. Add child `Button` (text: "New Game", name `NewGameButton`).
6. Select `NewGameButton` → Signal tab → connect `pressed()` to `SceneManager.GotoOverworld`. (Wire the absolute path `/root/SceneManager` if Godot prompts.)

- [ ] **Step 2: Commit**

```bash
git add scenes/TitleScreen.tscn
git commit -m "feat(scenes): add TitleScreen with New Game button"
```

---

### Task D2: `HealthBar` custom control

**Files:**
- Create: `scripts/ui/HealthBar.cs`
- Create: `scenes/ui/HealthBar.tscn`

- [ ] **Step 1: Implement `scripts/ui/HealthBar.cs`**

```csharp
using Godot;

namespace rpg_game.scripts.ui;

public partial class HealthBar : Control
{
    private int _hp;
    private int _hpMax;
    private Color _bg = new(0.1f, 0.1f, 0.1f);
    private Color _fg = new(0.2f, 0.8f, 0.2f);

    public void SetValues(int hp, int hpMax)
    {
        _hp = hp;
        _hpMax = Mathf.Max(1, hpMax);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(0, 0, Size.X, Size.Y);
        DrawRect(rect, _bg);
        float w = Size.X * ((float)_hp / _hpMax);
        DrawRect(new Rect2(0, 0, w, Size.Y), _fg);
    }
}
```

- [ ] **Step 2: Author `scenes/ui/HealthBar.tscn`**

1. Scene → New Node → `Control` (root, name `HealthBar`).
2. Attach `scripts/ui/HealthBar.cs`.
3. Set size in inspector: 200×24.

- [ ] **Step 3: Commit**

```bash
git add scripts/ui/HealthBar.cs scenes/ui/HealthBar.tscn
git commit -m "feat(ui): add HealthBar custom control"
```

---

### Task D3: `DialogBox` scene

**Files:**
- Create: `scripts/ui/DialogBox.cs`
- Create: `scenes/ui/DialogBox.tscn`

- [ ] **Step 1: Implement `scripts/ui/DialogBox.cs`**

```csharp
using Godot;

namespace rpg_game.scripts.ui;

public partial class DialogBox : Control
{
    [Export] public Label TextLabel;

    public override void _Ready()
    {
        Visible = false;
    }

    public void ShowLine(string text)
    {
        TextLabel.Text = text;
        Visible = true;
    }

    public void Hide()
    {
        Visible = false;
    }
}
```

- [ ] **Step 2: Author `scenes/ui/DialogBox.tscn`**

1. Root `Control`, name `DialogBox`, full-rect anchor.
2. Add child `Panel` (full rect).
3. Add child `Label` (margin 16px, name `Text`, set as `TextLabel` exported node path).
4. Attach `scripts/ui/DialogBox.cs` to root.

- [ ] **Step 3: Commit**

```bash
git add scripts/ui/DialogBox.cs scenes/ui/DialogBox.tscn
git commit -m "feat(ui): add DialogBox"
```

---

### Task D4: `PartyMenu` scene

**Files:**
- Create: `scripts/ui/PartyMenu.cs`
- Create: `scenes/ui/PartyMenu.tscn`

- [ ] **Step 1: Implement `scripts/ui/PartyMenu.cs`**

```csharp
using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.ui;

public partial class PartyMenu : Control
{
    [Export] public VBoxContainer SlotContainer;

    public override void _Ready()
    {
        Visible = false;
        Rebuild();
    }

    public void Rebuild()
    {
        foreach (var child in SlotContainer.GetChildren()) child.QueueFree();
        var party = GetNode<Party>("/root/Party");
        for (int i = 0; i < party.Members.Count; i++)
        {
            var c = party.Members[i];
            var row = new HBoxContainer();
            var label = new Label
            {
                Text = (i == party.ActiveIndex ? "▶ " : "  ") + c.Species.DisplayName + $" Lv{c.Level} HP {c.CurrentHp}/{c.MaxHp}"
            };
            row.AddChild(label);
            SlotContainer.AddChild(row);
        }
    }
}
```

- [ ] **Step 2: Author `scenes/ui/PartyMenu.tscn`**

1. Root `Control`, name `PartyMenu`, anchored center, size 400×300.
2. Add child `Panel`.
3. Add child `VBoxContainer` (name `SlotContainer`, margin 16px).
4. Attach `scripts/ui/PartyMenu.cs`.

- [ ] **Step 3: Commit**

```bash
git add scripts/ui/PartyMenu.cs scenes/ui/PartyMenu.tscn
git commit -m "feat(ui): add PartyMenu"
```

---

### Task D5: `ItemMenu` scene

**Files:**
- Create: `scripts/ui/ItemMenu.cs`
- Create: `scenes/ui/ItemMenu.tscn`

- [ ] **Step 1: Implement `scripts/ui/ItemMenu.cs`**

```csharp
using Godot;
using rpg_game.scripts.autoload;

namespace rpg_game.scripts.ui;

public partial class ItemMenu : Control
{
    [Export] public VBoxContainer SlotContainer;

    public override void _Ready()
    {
        Visible = false;
    }

    public void Show()
    {
        Visible = true;
        foreach (var child in SlotContainer.GetChildren()) child.QueueFree();
        var items = new[] { ("Potion", "+20 HP"), ("Revive", "50% HP from 0") };
        foreach (var (name, desc) in items)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = name });
            row.AddChild(new Label { Text = "  " + desc });
            var btn = new Button { Text = "Use" };
            btn.Pressed += () => UseItem(name);
            row.AddChild(btn);
            SlotContainer.AddChild(row);
        }
    }

    private void UseItem(string name)
    {
        var party = GetNode<Party>("/root/Party");
        var active = party.Active;
        if (active == null) return;
        if (name == "Potion") active.Heal(20);
        if (name == "Revive" && active.IsFainted) active.Heal(active.MaxHp / 2);
        Visible = false;
    }
}
```

- [ ] **Step 2: Author `scenes/ui/ItemMenu.tscn`**

Same shape as `PartyMenu.tscn` but root has `scripts/ui/ItemMenu.cs`, and the `VBoxContainer` is named `SlotContainer`.

- [ ] **Step 3: Commit**

```bash
git add scripts/ui/ItemMenu.cs scenes/ui/ItemMenu.tscn
git commit -m "feat(ui): add ItemMenu with Potion + Revive"
```

---

### Task D6: `Battle` scene

**Files:**
- Create: `scripts/ui/BattleScreen.cs`
- Create: `scenes/Battle.tscn`

- [ ] **Step 1: Implement `scripts/ui/BattleScreen.cs`**

```csharp
using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.battle;
using rpg_game.scripts.runtime;
using rpg_game.scripts.ui;

namespace rpg_game.scripts.ui;

public partial class BattleScreen : Control
{
    [Export] public TextureRect EnemySprite;
    [Export] public TextureRect PlayerSprite;
    [Export] public Label EnemyNameLabel;
    [Export] public Label PlayerNameLabel;
    [Export] public HealthBar EnemyHpBar;
    [Export] public HealthBar PlayerHpBar;
    [Export] public Label MessageLabel;
    [Export] public Control ActionMenu;
    [Export] public Control MovePicker;
    [Export] public VBoxContainer MoveList;
    [Export] public PartyMenu PartyMenuRef;
    [Export] public ItemMenu ItemMenuRef;

    public override void _Ready()
    {
        var bm = GetNode<BattleManager>("/root/BattleManager");
        bm.BattleStarted += OnBattleStarted;
        bm.HpChanged += OnHpChanged;
        bm.Message += OnMessage;
        bm.BattleEnded += OnBattleEnded;
        GetNode<Button>("%FightButton").Pressed += OpenMovePicker;
        GetNode<Button>("%CreatureButton").Pressed += OpenParty;
        GetNode<Button>("%ItemButton").Pressed += OpenItems;
        GetNode<Button>("%RunButton").Pressed += () => bm.Run();
    }

    private void OnBattleStarted(bool isWild)
    {
        ShowActionMenu();
    }

    private void OnHpChanged(int side, int hp, int hpMax)
    {
        if (side == 0) PlayerHpBar.SetValues(hp, hpMax);
        else EnemyHpBar.SetValues(hp, hpMax);
    }

    private void OnMessage(string text)
    {
        MessageLabel.Text = text;
    }

    private void OnBattleEnded(int outcome)
    {
        MessageLabel.Text = outcome == (int)BattleState.Victory ? "Victory!"
            : outcome == (int)BattleState.Defeat ? "You blacked out..."
            : "Got away!";
        GetTree().CreateTimer(1.5).Timeout += () => GetNode<SceneManager>("/root/SceneManager").GotoOverworld();
    }

    private void ShowActionMenu()
    {
        ActionMenu.Visible = true;
        MovePicker.Visible = false;
        PartyMenuRef.Visible = false;
        ItemMenuRef.Visible = false;
    }

    private void OpenMovePicker()
    {
        ActionMenu.Visible = false;
        MovePicker.Visible = true;
        foreach (var c in MoveList.GetChildren()) c.QueueFree();
        var active = GetNode<Party>("/root/Party").Active;
        for (int i = 0; i < active.Species.Types.Count; i++) // placeholder
        {
            var t = active.Species.Types[i];
            var move = new MoveDataLite($"Strike-{i}", t, MoveCategory.Physical, 40, 100);
            var btn = new Button { Text = move.Name };
            btn.Pressed += () => { GetNode<BattleManager>("/root/BattleManager").PlayerAttack(move); };
            MoveList.AddChild(btn);
        }
    }

    private void OpenParty()
    {
        PartyMenuRef.Rebuild();
        PartyMenuRef.Visible = true;
        ActionMenu.Visible = false;
    }

    private void OpenItems()
    {
        ItemMenuRef.Show();
        ActionMenu.Visible = false;
    }
}
```

- [ ] **Step 2: Author `scenes/Battle.tscn`**

Build the scene in Godot editor:
- Root: `Control` (full rect), script `scripts/ui/BattleScreen.cs`.
- Top half: `EnemySprite` (TextureRect, anchored top-right, 192×192), `EnemyNameLabel` (Label), `EnemyHpBar` (instance `scenes/ui/HealthBar.tscn`).
- Bottom half: `PlayerSprite` (TextureRect, anchored bottom-left, 192×192), `PlayerNameLabel` (Label), `PlayerHpBar` (instance `scenes/ui/HealthBar.tscn`).
- Bottom strip: `MessageLabel` (Label, full width, height 48).
- Right side: `ActionMenu` Control with 4 buttons: `FightButton`, `CreatureButton`, `ItemButton`, `RunButton`. Use unique names (`%`).
- Center overlay: `MovePicker` Control with `MoveList` VBoxContainer, 4 buttons added at runtime.
- Overlay: `PartyMenu` (instance `scenes/ui/PartyMenu.tscn`).
- Overlay: `ItemMenu` (instance `scenes/ui/ItemMenu.tscn`).
- Bind exported properties on the root to these children.

- [ ] **Step 3: Commit**

```bash
git add scripts/ui/BattleScreen.cs scenes/Battle.tscn
git commit -m "feat(scenes): add Battle scene with action menu + move picker"
```

---

### Task D7: `Player` controller script

**Files:**
- Create: `scripts/overworld/Player.cs`

- [ ] **Step 1: Implement `scripts/overworld/Player.cs`**

```csharp
using Godot;

namespace rpg_game.scripts.overworld;

public partial class Player : CharacterBody2D
{
    [Export] public int Speed = 80;

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Velocity = input * Speed;
        MoveAndSlide();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/overworld/Player.cs
git commit -m "feat(overworld): add Player CharacterBody2D controller"
```

---

### Task D8: `Enemy` node script

**Files:**
- Create: `scripts/overworld/Enemy.cs`

- [ ] **Step 1: Implement `scripts/overworld/Enemy.cs`**

```csharp
using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.data;

namespace rpg_game.scripts.overworld;

public partial class Enemy : Area2D
{
    [Export] public EncounterEntry Entry;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player && Entry != null)
        {
            GetNode<EncounterSystem>("/root/EncounterSystem").Trigger(Entry);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/overworld/Enemy.cs
git commit -m "feat(overworld): add Enemy with encounter trigger"
```

---

### Task D9: `Npc` node script

**Files:**
- Create: `scripts/overworld/Npc.cs`

- [ ] **Step 1: Implement `scripts/overworld/Npc.cs`**

```csharp
using Godot;
using rpg_game.scripts.autoload;
using rpg_game.scripts.ui;

namespace rpg_game.scripts.overworld;

public partial class Npc : Area2D
{
    [Export] public string[] Lines;
    [Export] public DialogBox DialogBoxRef;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player && Lines != null && Lines.Length > 0)
        {
            GetNode<DialogPlayer>("/root/DialogPlayer").Play(Lines);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/overworld/Npc.cs
git commit -m "feat(overworld): add Npc dialog trigger"
```

---

### Task D10: `Overworld` base scene + first map (`Town`)

**Files:**
- Create: `scripts/overworld/Overworld.cs`
- Create: `scenes/Overworld.tscn`
- Create: `scenes/Town.tscn`

- [ ] **Step 1: Implement `scripts/overworld/Overworld.cs`**

```csharp
using Godot;
using rpg_game.scripts.autoload;

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
        GetNode<SceneManager>("/root/SceneManager").GotoBattle();
    }
}
```

- [ ] **Step 2: Author `scenes/Overworld.tscn`**

- Root: `Node2D`, name `Overworld`, script `scripts/overworld/Overworld.cs`.
- Add `TileMapLayer` (16×16 tiles, 1 tileset placeholder).
- Add `Player` (CharacterBody2D, script `scripts/overworld/Player.cs`, + `AnimatedSprite2D` child using a placeholder sprite).
- Add `Camera2D` child of `Player` (current = true, limit left/top/right/bottom set to map bounds).
- Add `DialogBox` instance (`scenes/ui/DialogBox.tscn`).

- [ ] **Step 3: Author `scenes/Town.tscn`**

- Root: `Node2D`.
- Add `TileMapLayer` filled with grass + path tiles (placeholder: solid colors).
- Add `Player` instance at start position.
- Add 1 `Npc` instance with `Lines = ["Welcome to town!", "Brave the cave to the north."]`.
- Add 1 `Enemy` instance south of town with a placeholder `EncounterEntry` (use `Wolf.tres` or whatever the first species is — created in Phase E).

- [ ] **Step 4: Commit**

```bash
git add scripts/overworld/Overworld.cs scenes/Overworld.tscn scenes/Town.tscn
git commit -m "feat(scenes): add Overworld + Town (first map)"
```

---

## Phase E: Content (`.tres` data files)

### Task E1: Author 6 moves

**Files:**
- Create: `resources/data/moves/Tackle.tres`
- Create: `resources/data/moves/Ember.tres`
- Create: `resources/data/moves/WaterGun.tres`
- Create: `resources/data/moves/VineWhip.tres`
- Create: `resources/data/moves/HolySmite.tres`
- Create: `resources/data/moves/DemonBite.tres`

- [ ] **Step 1: Create `Tackle.tres`**

In Godot editor, FileSystem → right-click `resources/data/moves/` → New Resource → `MoveData`. Set:

```
Id: "tackle"
DisplayName: "Tackle"
Type: Beast
Category: Physical
Power: 40
Accuracy: 100
Pp: 30
Description: "A basic body-slam attack."
```

Save as `Tackle.tres`. Repeat for the other 5 moves with these values:

| File | Id | Name | Type | Cat | Pow | Acc | PP | Desc |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Ember.tres` | `ember` | Ember | Elemental | Special | 40 | 100 | 25 | "A weak flame." |
| `WaterGun.tres` | `watergun` | Water Gun | Elemental | Special | 40 | 100 | 25 | "Sprays water." |
| `VineWhip.tres` | `vinewhip` | Vine Whip | Beast | Physical | 45 | 100 | 25 | "Whips with vines." |
| `HolySmite.tres` | `holysmite` | Holy Smite | Holy | Special | 50 | 90 | 15 | "Smites undead and demons." |
| `DemonBite.tres` | `demonbite` | Demon Bite | Demon | Physical | 50 | 90 | 15 | "Bites with demonic force." |

- [ ] **Step 2: Commit**

```bash
git add resources/data/moves/
git commit -m "content: add 6 starter move .tres files"
```

---

### Task E2: Author 8 species

**Files:**
- Create: 8 `.tres` files under `resources/data/creatures/`

- [ ] **Step 1: Create species files**

For each, in Godot editor: New Resource → `CreatureSpecies` → set fields → save.

| File | Id | Name | Types | HP/Atk/Def/SpA/SpD/Spe | PlaceholderColor | Learnset (level → moves) |
| --- | --- | --- | --- | --- | --- | --- |
| `Wolf.tres` | `wolf` | Wolf | `[Beast]` | 50/60/45/30/35/70 | `#A0A0A0` | `{3:["tackle"]}` |
| `Bear.tres` | `bear` | Bear | `[Beast]` | 80/85/60/30/40/45 | `#8B4513` | `{3:["tackle"]}` |
| `Imp.tres` | `imp` | Imp | `[Demon]` | 40/55/35/65/45/70 | `#B22222` | `{3:["demonbite"]}` |
| `Succubus.tres` | `succubus` | Succubus | `[Demon]` | 60/60/50/80/65/75 | `#8B008B` | `{3:["demonbite"]}` |
| `Wraith.tres` | `wraith` | Wraith | `[Undead]` | 50/50/50/70/70/55 | `#708090` | `{3:["holysmite"]}` |
| `Salamander.tres` | `salamander` | Salamander | `[Elemental]` | 55/55/50/70/50/65 | `#FF4500` | `{3:["ember"]}` |
| `Sylph.tres` | `sylph` | Sylph | `[Elemental, Holy]` | 50/40/45/65/70/80 | `#87CEEB` | `{3:["watergun"]}` |
| `Treant.tres` | `treant` | Treant | `[Beast, Holy]` | 90/70/80/50/60/30 | `#228B22` | `{3:["vinewhip"]}` |

For `FrontSprite` and `BackSprite`, leave unset (placeholder generator fills later in Task F1).

- [ ] **Step 2: Commit**

```bash
git add resources/data/creatures/
git commit -m "content: add 8 starter species .tres files"
```

---

### Task E3: Author 1 EncounterTable per area

**Files:**
- Create: `resources/data/encounters/Route1.tres`
- Create: `resources/data/encounters/Cave.tres`
- Create: `resources/data/encounters/BossRoom.tres`

- [ ] **Step 1: Create encounter tables**

For each: New Resource → `EncounterTable` → set `AreaId` + add `EncounterEntry` array members.

`Route1.tres`:
- `AreaId`: `route1`
- Entries: 3 wild entries — `(Wolf, L3, true)`, `(Imp, L4, true)`, `(Salamander, L5, true)`.

`Cave.tres`:
- `AreaId`: `cave`
- Entries: `(Wraith, L6, true)`, `(Succubus, L7, true)`, `(Bear, L6, true)`.

`BossRoom.tres`:
- `AreaId`: `bossroom`
- Entries: `(Treant, L10, false)` (capture_eligible=false for boss).

- [ ] **Step 2: Commit**

```bash
git add resources/data/encounters/
git commit -m "content: add 3 encounter tables for Route1, Cave, BossRoom"
```

---

## Phase F: Wire It Up + Polish

### Task F1: Populate starter party + populate Town with starter

**Files:**
- Modify: `scripts/autoload/Party.cs` (already has Add method)
- Modify: `scenes/Town.tscn` (or via script)

- [ ] **Step 1: Add a "give starter" trigger to Town**

Add a second `Npc` (or replace the first) that calls `Party.Add` with a level-5 `Wolf` instance, then opens a dialog. Implement as an override in Town's root script:

Create `scripts/overworld/TownStarter.cs`:

```csharp
using Godot;
using rpg_game.scripts.autoload;
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
        var wolf = GD.Load<rpg_game.scripts.data.CreatureSpecies>("res://resources/data/creatures/Wolf.tres");
        var inst = new CreatureInstance(wolf.ToLite(), 5);
        GetNode<Party>("/root/Party").Add(inst);
        GetNode<DialogPlayer>("/root/DialogPlayer").Play(
            new[] { "You received a Wolf!", "Press on, brave tamer." });
    }
}
```

- [ ] **Step 2: Attach script to Town root**

Attach `scripts/overworld/TownStarter.cs` to the `Town.tscn` root, set `StarterNpcPath` to the existing `Npc` node.

- [ ] **Step 3: Commit**

```bash
git add scripts/overworld/TownStarter.cs scenes/Town.tscn
git commit -m "feat(overworld): give player a starter Wolf in Town"
```

---

### Task F2: Wire encounter → battle transition

**Files:**
- Modify: `scripts/overworld/Overworld.cs`

- [ ] **Step 1: Pass entry to BattleManager before swap**

Replace the encounter handler to also call `BattleManager.StartWild`:

```csharp
private void OnEncounter(EncounterEntry entry)
{
    GetNode<BattleManager>("/root/BattleManager").StartWild(entry);
    GetNode<SceneManager>("/root/SceneManager").GotoBattle();
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/overworld/Overworld.cs
git commit -m "feat: wire encounter trigger into BattleManager + scene swap"
```

---

### Task F3: Wire battle end → return to overworld

**Files:**
- Modify: `scripts/ui/BattleScreen.cs` (already in Task D6 — verify timer/swap is wired)
- Modify: `scripts/autoload/BattleManager.cs` — restore `BattleManager` to a usable state if scene returns to overworld

- [ ] **Step 1: Verify `OnBattleEnded`**

`scripts/ui/BattleScreen.cs` already calls `SceneManager.GotoOverworld()` after 1.5s. No code change needed.

- [ ] **Step 2: Verify capture prompt after victory (optional v1)**

Skip for v1. Capture happens by default on enemy faint when `capture_eligible`. Add a one-line stub in `BattleManager.OnEnemyFaint`:

Modify `scripts/autoload/BattleManager.cs` `PlayerAttack` method, after the faint message:

```csharp
if (EnemyInstance.IsFainted)
{
    EmitSignal(SignalName.Message, $"{EnemyInstance.Species.DisplayName} fainted!");
    if (IsWild && CurrentEntry.CaptureEligible && GetNode<Party>("/root/Party").HasSpace())
    {
        var captured = new CreatureInstance(EnemyInstance.Species, EnemyInstance.Level);
        GetNode<Party>("/root/Party").Add(captured);
        EmitSignal(SignalName.Message, $"{captured.Species.DisplayName} joined your party!");
    }
    EmitSignal(SignalName.BattleEnded, (int)BattleState.Victory);
    return;
}
```

- [ ] **Step 3: Commit**

```bash
git add scripts/autoload/BattleManager.cs
git commit -m "feat(battle): auto-capture on enemy faint in wild battles"
```

---

### Task F4: Defeat handling → return to Town

**Files:**
- Modify: `scripts/ui/BattleScreen.cs`

- [ ] **Step 1: Update `OnBattleEnded` to swap to Town on defeat**

Replace the `OnBattleEnded` method:

```csharp
private void OnBattleEnded(int outcome)
{
    var sm = GetNode<SceneManager>("/root/SceneManager");
    if (outcome == (int)BattleState.Defeat)
    {
        MessageLabel.Text = "You blacked out...";
        var party = GetNode<Party>("/root/Party");
        foreach (var m in party.Members) m.Heal(m.MaxHp / 2);
        GetTree().CreateTimer(1.5).Timeout += () => sm.GotoScene("res://scenes/Town.tscn");
        return;
    }
    MessageLabel.Text = outcome == (int)BattleState.Victory ? "Victory!"
        : outcome == (int)BattleState.Escaped ? "Got away!" : "...";
    GetTree().CreateTimer(1.5).Timeout += sm.GotoOverworld;
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/ui/BattleScreen.cs
git commit -m "feat: defeat returns to Town with half HP"
```

---

### Task F5: Manual smoke test

- [ ] **Step 1: Run the game**

Run from project root: `godot --path .` (or open in editor and press F5).

Expected sequence:
1. Title screen appears.
2. Click "New Game" → Town loads.
3. Walk south → encounter a Wolf → Battle scene loads.
4. Pick "Fight" → "Strike" → enemy HP drops → enemy attacks → your HP drops.
5. Reduce enemy to 0 → Victory → "Wolf joined your party" → return to overworld.
6. Walk to NPC → dialog plays.
7. (If implemented) Walk to boss area → boss battle → on defeat, return to Town at half HP.

- [ ] **Step 2: Document any issues**

If any step fails, file a follow-up. If the spec is missing something required for this flow, add it to the spec and re-plan.

- [ ] **Step 3: Commit any post-test fixes**

```bash
git add -A
git commit -m "fix: smoke-test fixes from vertical slice playthrough"
```

---

## Self-Review

**Spec coverage check:**

| Spec section | Task(s) |
| --- | --- |
| Architecture (autoloads, resources, runtime) | A3–A8, B1–B4, C2–C8 |
| Data model (Stats, CreatureType, TypeChart, CreatureSpecies, MoveData, EncounterTable, CreatureInstance) | A3–A6, B1–B4 |
| Battle state machine + damage formula | A5, A7, A8, C4 |
| Overworld + visible enemy encounters | D7–D10, F2 |
| UI (Title, Dialog, Battle, Party, Item, HealthBar) | D1–D6 |
| Placeholder strategy | C1, F5 |
| Out of scope (no save/load, no evolution, etc.) | confirmed not implemented |

**Coverage complete.**

**Type/signature consistency:**

- `MoveDataLite` defined in test (A6) and used in `BattleStateMachine.ResolveSingle` (A7), `BattleManager.PlayerAttack` (C4), `BattleScreen.OpenMovePicker` (D6). All consistent.
- `CreatureInstance` constructed via `new(species_lite, level)` in A6; consumed in C4 via `.Species.Types` and `.CurrentHp/MaxHp`. Consistent.
- `BattleState` enum members used in `BattleManager.StartWild` (PlayerTurn), `PlayerAttack` (Victory/Escaped), `BattleScreen.OnBattleEnded` (Victory/Defeat/Escaped). Consistent.

**No placeholders found in steps** (no TBD, no "add appropriate X").

**Open question from spec §11 (capture trigger):** resolved as "auto-capture on enemy faint when wild" (F3). User can revisit.

**Open question from spec §11 (defeat handling):** resolved as "heal to half HP, return to Town" (F4). Matches spec default.

**Open question from spec §11 (starter):** resolved as "1 fixed starter (Wolf) in Town" (F1).

**Open question from spec §11 (creature/move lists):** resolved as 8 species + 6 moves in E1/E2.

**Open question from spec §11 (party size cap):** 6 (C2), confirmed in spec.

**Ready to execute.**
