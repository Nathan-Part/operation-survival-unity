# Spec Import (JSON → ScriptableObjects) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an Editor menu command that reads `unity-spec/*.json` and generates/updates ScriptableObjects (weapons, enemies, characters, power-ups, items, config singletons) while never touching art asset references assigned by hand.

**Architecture:** Three assemblies — `Game.Data` (runtime: enums, ScriptableObject classes, pure C# formula classes), `Game.Data.Editor` (editor-only: JSON POCOs, one importer class per domain, orchestrator menu command), `Game.Data.Tests` (EditMode tests, run against dedicated JSON fixtures, never against the real `unity-spec/*.json`). Each importer is idempotent (no-op on unchanged input) and never writes `modelPrefab`/`icon`/`image` fields.

**Tech Stack:** Unity 6000.5.2f1, C#, `com.unity.nuget.newtonsoft-json` for JSON parsing, Unity Test Framework (NUnit, EditMode) already present as `com.unity.test-framework`. Verification in this environment goes through the connected Unity MCP bridge (instance `FPS@42e6fd67fcf34cce`): after writing/editing `.cs` files, call `mcp__UnityMCP__refresh_unity` then `mcp__UnityMCP__read_console` (expect no errors) to confirm compilation, and `mcp__UnityMCP__run_tests` + `mcp__UnityMCP__get_test_job` (poll until done) to run EditMode tests. These Unity MCP tools are deferred — load their schemas with `ToolSearch` (`select:mcp__UnityMCP__refresh_unity,mcp__UnityMCP__read_console,mcp__UnityMCP__run_tests,mcp__UnityMCP__get_test_job`) before first use.

## Global Constraints

- Source of truth for values: `unity-spec/*.json` at the project root (same repo as this Unity project — confirmed, not a sibling project). Never hand-edit generated `.asset` files' gameplay fields; re-run the importer instead.
- Art asset references (`modelPrefab`, `icon`, `image`, `animationClip`) are assigned by hand in the Inspector and must NEVER be written by any importer — this is the core invariant of the whole feature.
- Formulas (wave composition, XP, economy, upgrade effects) are written by hand in `Game.Data.Rules` — importers only produce constants/config, never derived logic.
- Code identifiers in English; JSON content values (names, descriptions) stay in French exactly as extracted.
- `ScriptableObject` classes must never declare a public field literally named `name` — `UnityEngine.Object` already defines `name`; use `displayName` instead everywhere a JSON `"name"` field is mapped.
- Idempotence: running the importer twice with no JSON changes must not mark any asset as modified (verified per-importer via a "second run reports Unchanged" test).
- No auto-import on file save (no `AssetPostprocessor`) — only the explicit `Tools/Game/Import Spec from JSON` menu command, invoked after this plan's tasks are executed.
- Test fixtures live under `Assets/_Game/Tests/Editor/Fixtures/` and are trimmed (2-3 entries), never the real `unity-spec/*.json` files.
- Every importer writes into a folder passed as a parameter (not hardcoded inline) so tests can target an isolated fixture output folder distinct from the real `Assets/_Game/Data/` tree, and clean up after themselves in `[TearDown]`.

---

## Task 1: Project scaffolding — package, assemblies, enums, placeholder library

**Files:**
- Modify: `Packages/manifest.json`
- Create: `Assets/_Game/Scripts/Data/Game.Data.asmdef`
- Create: `Assets/_Game/Scripts/Data/Enums.cs`
- Create: `Assets/_Game/Scripts/Data/PlaceholderLibrary.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/Game.Data.Editor.asmdef`
- Create: `Assets/_Game/Tests/Editor/Game.Data.Tests.asmdef`
- Create: `Assets/_Game/Tests/Editor/PlaceholderLibraryTests.cs`

**Interfaces:**
- Produces: `Game.Data.Rarity`, `Game.Data.WeaponCategory`, `Game.Data.ArmorType`, `Game.Data.PvpPolicy`, `Game.Data.BoostSlot`, `Game.Data.BoostStat` enums. `Game.Data.PlaceholderLibrary` (ScriptableObject) with `GetWeaponPlaceholder(WeaponCategory)`, `GetEnemyPlaceholder()`, `GetCharacterPlaceholder()`, `GetGenericIcon()`.

- [ ] **Step 1: Add the Newtonsoft.Json package dependency**

Edit `Packages/manifest.json`, add this line right after the opening `"dependencies": {`:

```json
    "com.unity.nuget.newtonsoft-json": "3.2.1",
```

- [ ] **Step 2: Trigger package resolution and confirm no errors**

Call `mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__read_console` filtered for errors.
Expected: no errors; package `com.unity.nuget.newtonsoft-json` resolved.

- [ ] **Step 3: Create the runtime assembly definition**

Create `Assets/_Game/Scripts/Data/Game.Data.asmdef`:

```json
{
    "name": "Game.Data",
    "rootNamespace": "Game.Data",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Create the editor assembly definition**

Create `Assets/_Game/Scripts/Editor/Import/Game.Data.Editor.asmdef`:

```json
{
    "name": "Game.Data.Editor",
    "rootNamespace": "Game.Data.Editor.Import",
    "references": [
        "Game.Data",
        "Newtonsoft.Json"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5: Create the test assembly definition**

Create `Assets/_Game/Tests/Editor/Game.Data.Tests.asmdef`:

```json
{
    "name": "Game.Data.Tests",
    "rootNamespace": "Game.Data.Tests",
    "references": [
        "Game.Data",
        "Game.Data.Editor",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Newtonsoft.Json"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 6: Write the shared enums**

Create `Assets/_Game/Scripts/Data/Enums.cs`:

```csharp
namespace Game.Data
{
    public enum Rarity { Common, Rare, Epic, Legendary }
    public enum WeaponCategory { Assault, Smg, Sniper, Shotgun, Pistol, Lmg, Melee }
    public enum ArmorType { None, Torso, Full }
    public enum PvpPolicy { Allowed, Rebalance, Exclude }
    public enum BoostSlot { Vest, Boots, Medal }
    public enum BoostStat { Health, Regen, Speed }
}
```

- [ ] **Step 7: Write the failing test for PlaceholderLibrary**

Create `Assets/_Game/Tests/Editor/PlaceholderLibraryTests.cs`:

```csharp
using Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Data.Tests
{
    public class PlaceholderLibraryTests
    {
        [Test]
        public void GetWeaponPlaceholder_ReturnsMatchingCategoryPrefab()
        {
            var lib = ScriptableObject.CreateInstance<PlaceholderLibrary>();
            var assaultPrefab = new GameObject("AssaultPlaceholder");
            var sniperPrefab = new GameObject("SniperPlaceholder");
            lib.weaponPlaceholders = new[]
            {
                new WeaponCategoryPlaceholder { category = WeaponCategory.Assault, prefab = assaultPrefab },
                new WeaponCategoryPlaceholder { category = WeaponCategory.Sniper, prefab = sniperPrefab }
            };

            Assert.AreEqual(sniperPrefab, lib.GetWeaponPlaceholder(WeaponCategory.Sniper));
            Assert.IsNull(lib.GetWeaponPlaceholder(WeaponCategory.Lmg));

            Object.DestroyImmediate(assaultPrefab);
            Object.DestroyImmediate(sniperPrefab);
        }
    }
}
```

- [ ] **Step 8: Run the test to verify it fails**

Call `mcp__UnityMCP__run_tests` (EditMode, filter `Game.Data.Tests.PlaceholderLibraryTests`), then poll `mcp__UnityMCP__get_test_job` until complete.
Expected: compilation error (`PlaceholderLibrary` does not exist) or test run failure — `PlaceholderLibrary` isn't written yet.

- [ ] **Step 9: Implement PlaceholderLibrary**

Create `Assets/_Game/Scripts/Data/PlaceholderLibrary.cs`:

```csharp
using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class WeaponCategoryPlaceholder
    {
        public WeaponCategory category;
        public GameObject prefab;
    }

    [CreateAssetMenu(menuName = "Game/Config/Placeholder Library", fileName = "PlaceholderLibrary")]
    public class PlaceholderLibrary : ScriptableObject
    {
        public WeaponCategoryPlaceholder[] weaponPlaceholders = Array.Empty<WeaponCategoryPlaceholder>();
        public GameObject enemyPlaceholder;
        public GameObject characterPlaceholder;
        public Sprite genericIcon;

        public GameObject GetWeaponPlaceholder(WeaponCategory category)
        {
            foreach (var entry in weaponPlaceholders)
                if (entry.category == category)
                    return entry.prefab;
            return null;
        }

        public GameObject GetEnemyPlaceholder() => enemyPlaceholder;
        public GameObject GetCharacterPlaceholder() => characterPlaceholder;
        public Sprite GetGenericIcon() => genericIcon;
    }
}
```

- [ ] **Step 10: Run the test to verify it passes**

Call `mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` (same filter), poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, 1 test run, 0 failures.

- [ ] **Step 11: Commit**

```bash
git add Packages/manifest.json Assets/_Game/Scripts/Data/Game.Data.asmdef Assets/_Game/Scripts/Data/Game.Data.asmdef.meta Assets/_Game/Scripts/Data/Enums.cs Assets/_Game/Scripts/Data/Enums.cs.meta Assets/_Game/Scripts/Data/PlaceholderLibrary.cs Assets/_Game/Scripts/Data/PlaceholderLibrary.cs.meta Assets/_Game/Scripts/Editor/Import/Game.Data.Editor.asmdef Assets/_Game/Scripts/Editor/Import/Game.Data.Editor.asmdef.meta Assets/_Game/Tests/Editor/Game.Data.Tests.asmdef Assets/_Game/Tests/Editor/Game.Data.Tests.asmdef.meta Assets/_Game/Tests/Editor/PlaceholderLibraryTests.cs Assets/_Game/Tests/Editor/PlaceholderLibraryTests.cs.meta
git commit -m "feat: scaffold Game.Data assemblies, enums, and PlaceholderLibrary"
```

Note: Unity generates a `.meta` file next to every new asset on the next `refresh_unity` / editor focus. If a `.meta` file doesn't exist yet when you run `git add`, run `git add -A -- Assets/_Game` instead to pick up whatever `.meta` files Unity has generated so far.

---

## Task 2: Gameplay formulas (WaveRules, EconomyRules, UpgradeFormulas, StatFormulas)

**Files:**
- Create: `Assets/_Game/Scripts/Data/Rules/WaveRules.cs`
- Create: `Assets/_Game/Scripts/Data/Rules/EconomyRules.cs`
- Create: `Assets/_Game/Scripts/Data/Rules/UpgradeFormulas.cs`
- Create: `Assets/_Game/Scripts/Data/Rules/StatFormulas.cs`
- Test: `Assets/_Game/Tests/Editor/WaveRulesTests.cs`
- Test: `Assets/_Game/Tests/Editor/EconomyRulesTests.cs`
- Test: `Assets/_Game/Tests/Editor/UpgradeFormulasTests.cs`
- Test: `Assets/_Game/Tests/Editor/StatFormulasTests.cs`

**Interfaces:**
- Consumes: nothing from Task 1 besides the assembly setup.
- Produces: `Game.Data.Rules.WaveRules.{IsBossWave(int), EnemiesForWave(int), EnemyHpForWave(int,float), WaveScoreMult(int), WaveMoneyBonus(int,float)}`; `Game.Data.Rules.EconomyRules.{ComboMultiplier(int), MoneyForKill(int,int,float,bool,int), XpForLevel(int)}`; `Game.Data.Rules.UpgradeFormulas.{Cost(int,int), WeaponDamageMult(float,int), WeaponCooldown(float,int), WeaponRecoil(float,int), WeaponMag(int,int)}`; `Game.Data.Rules.StatFormulas.{MaxHealth(float,int,float), RegenRate(float,int,float), SpeedMult(float,int,float)}`. These are consumed by later gameplay code, not by the importer tasks in this plan.

### WaveRules

Values transposed from `unity-spec/waves.json` → `formulas` (`w`=wave number, 1-based).

- [ ] **Step 1: Write the failing tests**

Create `Assets/_Game/Tests/Editor/WaveRulesTests.cs`:

```csharp
using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class WaveRulesTests
    {
        [Test]
        public void IsBossWave_TrueOnMultiplesOfFive()
        {
            Assert.IsTrue(WaveRules.IsBossWave(5));
            Assert.IsTrue(WaveRules.IsBossWave(10));
            Assert.IsFalse(WaveRules.IsBossWave(4));
        }

        [Test]
        public void EnemiesForWave_CapsAtSevenExceptBossWaves()
        {
            Assert.AreEqual(3, WaveRules.EnemiesForWave(1));
            Assert.AreEqual(7, WaveRules.EnemiesForWave(5));
            Assert.AreEqual(7, WaveRules.EnemiesForWave(6));
            Assert.AreEqual(7, WaveRules.EnemiesForWave(9));
            Assert.AreEqual(3, WaveRules.EnemiesForWave(10));
        }

        [Test]
        public void EnemyHpForWave_MatchesReferenceValues()
        {
            Assert.AreEqual(100, WaveRules.EnemyHpForWave(1, 1.0f));
            Assert.AreEqual(193, WaveRules.EnemyHpForWave(5, 1.0f));
            Assert.AreEqual(135, WaveRules.EnemyHpForWave(1, 1.35f));
        }

        [Test]
        public void WaveScoreMult_IncreasesEveryFiveWaves()
        {
            Assert.AreEqual(1f, WaveRules.WaveScoreMult(4));
            Assert.AreEqual(1.5f, WaveRules.WaveScoreMult(5));
            Assert.AreEqual(1.5f, WaveRules.WaveScoreMult(9));
            Assert.AreEqual(2f, WaveRules.WaveScoreMult(10));
        }

        [Test]
        public void WaveMoneyBonus_RoundsToNearestInt()
        {
            Assert.AreEqual(25, WaveRules.WaveMoneyBonus(1, 1.0f));
            Assert.AreEqual(23, WaveRules.WaveMoneyBonus(1, 0.9f));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.WaveRulesTests`, poll `mcp__UnityMCP__get_test_job`.
Expected: compile error — `Game.Data.Rules.WaveRules` doesn't exist.

- [ ] **Step 3: Implement WaveRules**

Create `Assets/_Game/Scripts/Data/Rules/WaveRules.cs`:

```csharp
using System;

namespace Game.Data.Rules
{
    public static class WaveRules
    {
        public static bool IsBossWave(int wave) => wave % 5 == 0;

        public static int EnemiesForWave(int wave) => IsBossWave(wave) ? 3 : Math.Min(2 + wave, 7);

        public static int EnemyHpForWave(int wave, float difficultyHpMult) =>
            (int)Math.Round(100 * Math.Pow(1.18, wave - 1) * difficultyHpMult, MidpointRounding.AwayFromZero);

        public static float WaveScoreMult(int wave) => 1f + (wave / 5) * 0.5f;

        public static int WaveMoneyBonus(int wave, float difficultyMoneyMult) =>
            (int)Math.Round(25 * wave * difficultyMoneyMult, MidpointRounding.AwayFromZero);
    }
}
```

- [ ] **Step 4: Run to verify pass**

Same filter as Step 2. Expected: PASS, 5 tests, 0 failures.

### EconomyRules

- [ ] **Step 5: Write the failing tests**

Create `Assets/_Game/Tests/Editor/EconomyRulesTests.cs`:

```csharp
using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class EconomyRulesTests
    {
        [Test]
        public void ComboMultiplier_CapsAtFive()
        {
            Assert.AreEqual(1, EconomyRules.ComboMultiplier(0));
            Assert.AreEqual(2, EconomyRules.ComboMultiplier(3));
            Assert.AreEqual(5, EconomyRules.ComboMultiplier(30));
        }

        [Test]
        public void MoneyForKill_AppliesComboDifficultyAndVip()
        {
            Assert.AreEqual(10, EconomyRules.MoneyForKill(10, 1, 1.0f, false, 2));
            Assert.AreEqual(20, EconomyRules.MoneyForKill(10, 1, 1.0f, true, 2));
            Assert.AreEqual(9, EconomyRules.MoneyForKill(10, 1, 0.9f, false, 2));
        }

        [Test]
        public void XpForLevel_MatchesTriangularFormula()
        {
            Assert.AreEqual(100, EconomyRules.XpForLevel(1));
            Assert.AreEqual(300, EconomyRules.XpForLevel(2));
            Assert.AreEqual(600, EconomyRules.XpForLevel(3));
        }
    }
}
```

- [ ] **Step 6: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.EconomyRulesTests`. Expected: compile error.

- [ ] **Step 7: Implement EconomyRules**

Create `Assets/_Game/Scripts/Data/Rules/EconomyRules.cs`:

```csharp
using System;

namespace Game.Data.Rules
{
    public static class EconomyRules
    {
        public const int ComboCapMultiplier = 5;

        public static int ComboMultiplier(int consecutiveKills) =>
            Math.Min(ComboCapMultiplier, 1 + consecutiveKills / 3);

        public static int MoneyForKill(int perKill, int comboMultiplier, float difficultyMoneyMult, bool isVip, int vipMult)
        {
            float vipFactor = isVip ? vipMult : 1;
            return (int)Math.Round(perKill * comboMultiplier * difficultyMoneyMult * vipFactor, MidpointRounding.AwayFromZero);
        }

        public static long XpForLevel(int level) => 100L * level * (level + 1) / 2;
    }
}
```

- [ ] **Step 8: Run to verify pass**

Expected: PASS, 3 tests, 0 failures.

### UpgradeFormulas

- [ ] **Step 9: Write the failing tests**

Create `Assets/_Game/Tests/Editor/UpgradeFormulasTests.cs`:

```csharp
using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class UpgradeFormulasTests
    {
        [Test]
        public void Cost_ScalesWithLevel()
        {
            Assert.AreEqual(300, UpgradeFormulas.Cost(300, 0));
            Assert.AreEqual(900, UpgradeFormulas.Cost(300, 2));
        }

        [Test]
        public void WeaponDamageMult_AppliesFifteenPercentPerLevel()
        {
            Assert.AreEqual(1.0f, UpgradeFormulas.WeaponDamageMult(1.0f, 0), 0.0001f);
            Assert.AreEqual(1.45f, UpgradeFormulas.WeaponDamageMult(1.0f, 3), 0.0001f);
        }

        [Test]
        public void WeaponCooldown_ReducesEightPercentPerLevel()
        {
            Assert.AreEqual(0.12f, UpgradeFormulas.WeaponCooldown(0.12f, 0), 0.0001f);
            Assert.AreEqual(0.12f * 0.6f, UpgradeFormulas.WeaponCooldown(0.12f, 5), 0.0001f);
        }

        [Test]
        public void WeaponMag_RoundsToNearestInt()
        {
            Assert.AreEqual(12, UpgradeFormulas.WeaponMag(12, 0));
            Assert.AreEqual(14, UpgradeFormulas.WeaponMag(12, 1));
        }
    }
}
```

- [ ] **Step 10: Run to verify failure**

Expected: compile error.

- [ ] **Step 11: Implement UpgradeFormulas**

Create `Assets/_Game/Scripts/Data/Rules/UpgradeFormulas.cs`:

```csharp
using System;

namespace Game.Data.Rules
{
    public static class UpgradeFormulas
    {
        public static int Cost(int baseCost, int level) => baseCost * (level + 1);

        public static float WeaponDamageMult(float baseDmgMult, int level) => baseDmgMult * (1 + 0.15f * level);

        public static float WeaponCooldown(float baseCooldown, int level) => baseCooldown * (1 - 0.08f * level);

        public static float WeaponRecoil(float baseRecoil, int level) => baseRecoil * (1 - 0.12f * level);

        public static int WeaponMag(int baseMag, int level) =>
            (int)Math.Round(baseMag * (1 + 0.2f * level), MidpointRounding.AwayFromZero);
    }
}
```

- [ ] **Step 12: Run to verify pass**

Expected: PASS, 4 tests, 0 failures.

### StatFormulas

- [ ] **Step 13: Write the failing tests**

Create `Assets/_Game/Tests/Editor/StatFormulasTests.cs`:

```csharp
using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class StatFormulasTests
    {
        [Test]
        public void MaxHealth_AddsUpgradeAndBonus()
        {
            Assert.AreEqual(100f, StatFormulas.MaxHealth(100f, 0, 0f), 0.0001f);
            Assert.AreEqual(160f, StatFormulas.MaxHealth(100f, 2, 20f), 0.0001f);
        }

        [Test]
        public void RegenRate_AddsUpgradeAndBonus()
        {
            Assert.AreEqual(10f, StatFormulas.RegenRate(10f, 0, 0f), 0.0001f);
            Assert.AreEqual(19f, StatFormulas.RegenRate(10f, 2, 3f), 0.0001f);
        }

        [Test]
        public void SpeedMult_AppliesPercentBonusToBase()
        {
            Assert.AreEqual(1.0f, StatFormulas.SpeedMult(1.0f, 0, 0f), 0.0001f);
            Assert.AreEqual(1.18f, StatFormulas.SpeedMult(1.0f, 2, 0.06f), 0.0001f);
        }
    }
}
```

- [ ] **Step 14: Run to verify failure**

Expected: compile error.

- [ ] **Step 15: Implement StatFormulas**

Create `Assets/_Game/Scripts/Data/Rules/StatFormulas.cs`:

```csharp
namespace Game.Data.Rules
{
    public static class StatFormulas
    {
        public static float MaxHealth(float baseHealth, int healthUpgradeLevel, float bonusHealth) =>
            baseHealth + 20f * healthUpgradeLevel + bonusHealth;

        public static float RegenRate(float baseRegen, int regenUpgradeLevel, float bonusRegen) =>
            baseRegen + 3f * regenUpgradeLevel + bonusRegen;

        public static float SpeedMult(float baseSpeed, int speedUpgradeLevel, float bonusSpeed) =>
            baseSpeed * (1 + 0.06f * speedUpgradeLevel + bonusSpeed);
    }
}
```

- [ ] **Step 16: Run to verify pass**

Expected: PASS, 3 tests, 0 failures.

- [ ] **Step 17: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/Rules Assets/_Game/Tests/Editor/WaveRulesTests.cs Assets/_Game/Tests/Editor/EconomyRulesTests.cs Assets/_Game/Tests/Editor/UpgradeFormulasTests.cs Assets/_Game/Tests/Editor/StatFormulasTests.cs
git commit -m "feat: add hand-written gameplay formulas (wave, economy, upgrade, stat)"
```

---

## Task 3: Weapons — WeaponData, import infrastructure, WeaponImporter, UpgradeConfig

**Files:**
- Create: `Assets/_Game/Scripts/Data/WeaponData.cs`
- Create: `Assets/_Game/Scripts/Data/Config/UpgradeConfig.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/Paths.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/ImportReport.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/JsonFile.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/AssetImportUtil.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/EnumParsing.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/JsonModels/WeaponsJson.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/WeaponImporter.cs`
- Create: `Assets/_Game/Tests/Editor/Fixtures/weapons.test.json`
- Test: `Assets/_Game/Tests/Editor/WeaponImporterTests.cs`

**Interfaces:**
- Consumes: `Game.Data.WeaponCategory`, `Game.Data.Rarity` (Task 1).
- Produces: `Game.Data.WeaponData` (fields: `id, displayName, category, rarity, price, desc, semiAuto, melee, mag, cooldown, reload, dmgMult, recoil, range, pellets, modelPrefab, icon`, methods `ResolveModel(PlaceholderLibrary)`, `ResolveIcon(PlaceholderLibrary)`). `Game.Data.UpgradeConfig` (fields `weaponUpgrades: WeaponUpgradeSpec[]`, `playerUpgrades: PlayerUpgradeSpec[]`). `Game.Data.Editor.Import.Paths` (folder/singleton path constants, extended by every later task). `Game.Data.Editor.Import.ImportReport`, `JsonFile.Read<T>(string path)`, `AssetImportUtil.{FindOrCreate<T>(string folder, string id, out bool wasCreated), FindById<T>(string folder, string id), FindOrCreateSingleton<T>(string path), AssetPath(string folder, string id), ReportOrphans<T>(string folder, IEnumerable<string> importedIds, ImportReport report)}`, `EnumParsing.{ParseRarity, ParseWeaponCategory, ParseArmor, ParsePvpPolicy, ParseBoostSlot, ParseBoostStat}`. `Game.Data.Editor.Import.WeaponImporter.Import(string jsonPath, string weaponsFolder, string upgradeConfigPath, ImportReport report) : WeaponImportResult` where `WeaponImportResult { List<WeaponData> Weapons; UpgradeConfig UpgradeConfig; }`. `ReportOrphans` is how every catalog importer (this one included) fills `ImportReport.Orphaned` — every importer from this task onward calls it once, after building its id list, before returning.

- [ ] **Step 1: Write the failing test for WeaponImporter (creation + field mapping)**

Create `Assets/_Game/Tests/Editor/Fixtures/weapons.test.json`:

```json
{
  "_doc": "Fixture — 3 armes, un sous-ensemble représentatif de unity-spec/weapons.json.",
  "upgrades": {
    "damage":   { "name": "Dégâts",   "max": 5, "baseCost": 300, "effect": "dmgMult × (1 + 0.15 × l)" },
    "fireRate": { "name": "Cadence",  "max": 5, "baseCost": 300, "effect": "cooldown × (1 − 0.08 × l)" },
    "recoil":   { "name": "Recul",    "max": 5, "baseCost": 200, "effect": "recoil × (1 − 0.12 × l)" },
    "mag":      { "name": "Chargeur", "max": 5, "baseCost": 250, "effect": "mag = round(mag × (1 + 0.2 × l))" }
  },
  "weapons": [
    { "id": "default", "name": "Viper-AR", "category": "assault", "rarity": "commun", "price": 0,
      "desc": "Fusil d'assaut polyvalent.",
      "stats": { "mag": 12, "cooldown": 0.12, "reload": 1.2, "dmgMult": 1.0, "recoil": 1.0, "range": 60 },
      "assets": { "model": "artefact/AR-15/", "icon": null } },
    { "id": "jackal", "name": "Jackal-45", "category": "pistol", "rarity": "rare", "price": 600, "semiAuto": true,
      "desc": "Gros calibre semi-auto.",
      "stats": { "mag": 12, "cooldown": 0.18, "reload": 0.85, "dmgMult": 1.3, "recoil": 0.8, "range": 35 },
      "assets": { "model": null, "icon": null } },
    { "id": "shotgun", "name": "Devastator 870", "category": "shotgun", "rarity": "epique", "price": 1100,
      "desc": "Pompe dévastatrice à courte portée.",
      "stats": { "mag": 6, "cooldown": 0.7, "reload": 2.0, "dmgMult": 0.55, "recoil": 2.6, "pellets": 8, "range": 14 },
      "assets": { "model": null, "icon": null } }
  ]
}
```

Create `Assets/_Game/Tests/Editor/WeaponImporterTests.cs`:

```csharp
using System.IO;
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Data.Tests
{
    public class WeaponImporterTests
    {
        const string FixturePath = "Assets/_Game/Tests/Editor/Fixtures/weapons.test.json";
        const string OutputFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Weapons";
        const string UpgradeConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/UpgradeConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_CreatesWeaponDataWithMappedFields()
        {
            var report = new ImportReport();

            var result = WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, report);

            Assert.AreEqual(3, result.Weapons.Count);
            var jackal = result.Weapons.Find(w => w.id == "jackal");
            Assert.IsNotNull(jackal);
            Assert.AreEqual("Jackal-45", jackal.displayName);
            Assert.AreEqual(WeaponCategory.Pistol, jackal.category);
            Assert.AreEqual(Rarity.Rare, jackal.rarity);
            Assert.IsTrue(jackal.semiAuto);
            Assert.AreEqual(12, jackal.mag);
            Assert.AreEqual(1.3f, jackal.dmgMult, 0.0001f);
            Assert.AreEqual(3, report.Created.Count);

            Assert.AreEqual(4, result.UpgradeConfig.weaponUpgrades.Length);
            var damageSpec = System.Array.Find(result.UpgradeConfig.weaponUpgrades, s => s.statKey == "damage");
            Assert.AreEqual(300, damageSpec.baseCost);
            Assert.AreEqual(5, damageSpec.maxLevel);
        }

        [Test]
        public void Import_NeverOverwritesHandAssignedAssetRefs()
        {
            var report = new ImportReport();
            var first = WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, report);
            var jackal = first.Weapons.Find(w => w.id == "jackal");
            var fakePrefab = new GameObject("HandAssignedJackalModel");
            jackal.modelPrefab = fakePrefab;
            EditorUtility.SetDirty(jackal);
            AssetDatabase.SaveAssets();

            var second = WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, new ImportReport());

            var jackalAfter = second.Weapons.Find(w => w.id == "jackal");
            Assert.AreEqual(fakePrefab, jackalAfter.modelPrefab);

            Object.DestroyImmediate(fakePrefab);
        }

        [Test]
        public void Import_SecondRunWithNoChangesReportsNothingUpdated()
        {
            WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, new ImportReport());

            var secondReport = new ImportReport();
            WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, secondReport);

            Assert.AreEqual(0, secondReport.Created.Count);
            Assert.AreEqual(0, secondReport.Updated.Count);
            Assert.AreEqual(3, secondReport.Unchanged.Count);
        }

        [Test]
        public void Import_ReportsOrphanWhenAssetHasNoMatchingJsonId()
        {
            WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, new ImportReport());
            AssetImportUtil.EnsureFolder(OutputFolder);
            var strayAsset = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(strayAsset, $"{OutputFolder}/stray_weapon_no_longer_in_json.asset");

            var report = new ImportReport();
            WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, report);

            Assert.AreEqual(1, report.Orphaned.Count);
            StringAssert.Contains("stray_weapon_no_longer_in_json", report.Orphaned[0]);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.WeaponImporterTests`. Expected: compile errors (none of the referenced types exist yet).

- [ ] **Step 3: Create shared import infrastructure — Paths**

Create `Assets/_Game/Scripts/Editor/Import/Paths.cs`:

```csharp
namespace Game.Data.Editor.Import
{
    public static class Paths
    {
        public const string DataRoot = "Assets/_Game/Data";
        public const string Weapons = DataRoot + "/Weapons";
        public const string Enemies = DataRoot + "/Enemies";
        public const string Characters = DataRoot + "/Characters";
        public const string PowerUps = DataRoot + "/PowerUps";
        public const string Accessories = DataRoot + "/Items/Accessories";
        public const string Boosts = DataRoot + "/Items/Boosts";
        public const string Consumables = DataRoot + "/Items/Consumables";
        public const string Dances = DataRoot + "/Items/Dances";
        public const string ConfigRoot = DataRoot + "/Config";
        public const string MovementConfig = ConfigRoot + "/MovementConfig.asset";
        public const string EnemyCombatConfig = ConfigRoot + "/EnemyCombatConfig.asset";
        public const string WaveConfig = ConfigRoot + "/WaveConfig.asset";
        public const string EconomyConfig = ConfigRoot + "/EconomyConfig.asset";
        public const string UpgradeConfig = ConfigRoot + "/UpgradeConfig.asset";
        public const string PowerUpConfig = ConfigRoot + "/PowerUpConfig.asset";
        public const string GameDatabase = DataRoot + "/GameDatabase.asset";
    }
}
```

- [ ] **Step 4: Create ImportReport**

Create `Assets/_Game/Scripts/Editor/Import/ImportReport.cs`:

```csharp
using System.Collections.Generic;

namespace Game.Data.Editor.Import
{
    public class ImportReport
    {
        public readonly List<string> Created = new();
        public readonly List<string> Updated = new();
        public readonly List<string> Unchanged = new();
        public readonly List<string> Orphaned = new();
        public readonly List<string> Warnings = new();

        public void Merge(ImportReport other)
        {
            Created.AddRange(other.Created);
            Updated.AddRange(other.Updated);
            Unchanged.AddRange(other.Unchanged);
            Orphaned.AddRange(other.Orphaned);
            Warnings.AddRange(other.Warnings);
        }

        public string Summary() =>
            $"Created: {Created.Count}, Updated: {Updated.Count}, Unchanged: {Unchanged.Count}, " +
            $"Orphaned: {Orphaned.Count}, Warnings: {Warnings.Count}" +
            (Warnings.Count > 0 ? "\n\nWarnings:\n" + string.Join("\n", Warnings) : "") +
            (Orphaned.Count > 0 ? "\n\nOrphaned (not deleted, review manually):\n" + string.Join("\n", Orphaned) : "");
    }
}
```

- [ ] **Step 5: Create JsonFile helper**

Create `Assets/_Game/Scripts/Editor/Import/JsonFile.cs`:

```csharp
using System.IO;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import
{
    public static class JsonFile
    {
        public static T Read<T>(string path) => JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
    }
}
```

- [ ] **Step 6: Create AssetImportUtil**

Create `Assets/_Game/Scripts/Editor/Import/AssetImportUtil.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Data.Editor.Import
{
    public static class AssetImportUtil
    {
        public static string AssetPath(string folder, string id) => $"{folder}/{SanitizeFileName(id)}.asset";

        public static string SanitizeFileName(string id) => id.Replace(":", "_").Replace("/", "_");

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static T FindById<T>(string folder, string id) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(AssetPath(folder, id));

        public static T FindOrCreate<T>(string folder, string id, out bool wasCreated) where T : ScriptableObject
        {
            EnsureFolder(folder);
            var path = AssetPath(folder, id);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                wasCreated = false;
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            wasCreated = true;
            return asset;
        }

        public static T FindOrCreateSingleton<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(folder);
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void ReportOrphans<T>(string folder, IEnumerable<string> importedIds, ImportReport report) where T : UnityEngine.Object
        {
            var importedFileNames = new HashSet<string>(importedIds.Select(SanitizeFileName));
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (!importedFileNames.Contains(fileName))
                    report.Orphaned.Add(path);
            }
        }
    }
}
```

- [ ] **Step 7: Create EnumParsing**

Create `Assets/_Game/Scripts/Editor/Import/EnumParsing.cs`:

```csharp
using System;

namespace Game.Data.Editor.Import
{
    public static class EnumParsing
    {
        public static Rarity ParseRarity(string value) => value switch
        {
            "commun" => Rarity.Common,
            "rare" => Rarity.Rare,
            "epique" => Rarity.Epic,
            "legendaire" => Rarity.Legendary,
            _ => throw new ArgumentException($"Unknown rarity '{value}'")
        };

        public static WeaponCategory ParseWeaponCategory(string value) => value switch
        {
            "assault" => WeaponCategory.Assault,
            "smg" => WeaponCategory.Smg,
            "sniper" => WeaponCategory.Sniper,
            "shotgun" => WeaponCategory.Shotgun,
            "pistol" => WeaponCategory.Pistol,
            "lmg" => WeaponCategory.Lmg,
            "melee" => WeaponCategory.Melee,
            _ => throw new ArgumentException($"Unknown weapon category '{value}'")
        };

        public static ArmorType ParseArmor(string value) => value switch
        {
            null => ArmorType.None,
            "torso" => ArmorType.Torso,
            "full" => ArmorType.Full,
            _ => throw new ArgumentException($"Unknown armor type '{value}'")
        };

        public static PvpPolicy ParsePvpPolicy(string value) => value switch
        {
            "allowed" => PvpPolicy.Allowed,
            "rebalance" => PvpPolicy.Rebalance,
            "exclude" => PvpPolicy.Exclude,
            _ => throw new ArgumentException($"Unknown pvp policy '{value}'")
        };

        public static BoostSlot ParseBoostSlot(string value) => value switch
        {
            "vest" => BoostSlot.Vest,
            "boots" => BoostSlot.Boots,
            "medal" => BoostSlot.Medal,
            _ => throw new ArgumentException($"Unknown boost slot '{value}'")
        };

        public static BoostStat ParseBoostStat(string value) => value switch
        {
            "health" => BoostStat.Health,
            "regen" => BoostStat.Regen,
            "speed" => BoostStat.Speed,
            _ => throw new ArgumentException($"Unknown boost stat '{value}'")
        };
    }
}
```

- [ ] **Step 8: Create WeaponData**

Create `Assets/_Game/Scripts/Data/WeaponData.cs`:

```csharp
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string id;
        public string displayName;
        public WeaponCategory category;
        public Rarity rarity;
        public int price;
        public string desc;
        public bool semiAuto;
        public bool melee;
        public int mag;
        public float cooldown;
        public float reload;
        public float dmgMult;
        public float recoil;
        public float range;
        public int pellets;

        public GameObject modelPrefab;
        public Sprite icon;

        public GameObject ResolveModel(PlaceholderLibrary placeholders) =>
            modelPrefab != null ? modelPrefab : placeholders.GetWeaponPlaceholder(category);

        public Sprite ResolveIcon(PlaceholderLibrary placeholders) =>
            icon != null ? icon : placeholders.GetGenericIcon();
    }
}
```

- [ ] **Step 9: Create UpgradeConfig**

Create `Assets/_Game/Scripts/Data/Config/UpgradeConfig.cs`:

```csharp
using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class WeaponUpgradeSpec
    {
        public string statKey;
        public string displayName;
        public int maxLevel;
        public int baseCost;
        public string effectDesc;
    }

    [Serializable]
    public class PlayerUpgradeSpec
    {
        public string statKey;
        public string displayName;
        public int maxLevel;
        public int baseCost;
        public float perLevel;
    }

    [CreateAssetMenu(menuName = "Game/Config/Upgrade Config", fileName = "UpgradeConfig")]
    public class UpgradeConfig : ScriptableObject
    {
        public WeaponUpgradeSpec[] weaponUpgrades = Array.Empty<WeaponUpgradeSpec>();
        public PlayerUpgradeSpec[] playerUpgrades = Array.Empty<PlayerUpgradeSpec>();
    }
}
```

- [ ] **Step 10: Create the WeaponsJson POCO model**

Create `Assets/_Game/Scripts/Editor/Import/JsonModels/WeaponsJson.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class WeaponsJsonRoot
    {
        [JsonProperty("upgrades")] public WeaponUpgradeSpecsJson Upgrades;
        [JsonProperty("weapons")] public List<WeaponJsonEntry> Weapons;
    }

    public class WeaponUpgradeSpecsJson
    {
        [JsonProperty("damage")] public UpgradeSpecJson Damage;
        [JsonProperty("fireRate")] public UpgradeSpecJson FireRate;
        [JsonProperty("recoil")] public UpgradeSpecJson Recoil;
        [JsonProperty("mag")] public UpgradeSpecJson Mag;
    }

    public class UpgradeSpecJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("max")] public int Max;
        [JsonProperty("baseCost")] public int BaseCost;
        [JsonProperty("effect")] public string Effect;
    }

    public class WeaponJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("category")] public string Category;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("price")] public int Price;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("semiAuto")] public bool SemiAuto;
        [JsonProperty("melee")] public bool Melee;
        [JsonProperty("stats")] public WeaponStatsJson Stats;
    }

    public class WeaponStatsJson
    {
        [JsonProperty("mag")] public int Mag;
        [JsonProperty("cooldown")] public float Cooldown;
        [JsonProperty("reload")] public float Reload;
        [JsonProperty("dmgMult")] public float DmgMult;
        [JsonProperty("recoil")] public float Recoil;
        [JsonProperty("range")] public float Range;
        [JsonProperty("pellets")] public int Pellets;
    }
}
```

- [ ] **Step 11: Create WeaponImporter**

Create `Assets/_Game/Scripts/Editor/Import/WeaponImporter.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class WeaponImportResult
    {
        public List<WeaponData> Weapons;
        public UpgradeConfig UpgradeConfig;
    }

    public static class WeaponImporter
    {
        public static WeaponImportResult Import(string jsonPath, string weaponsFolder, string upgradeConfigPath, ImportReport report)
        {
            var root = JsonFile.Read<WeaponsJsonRoot>(jsonPath);
            var weapons = new List<WeaponData>();

            foreach (var entry in root.Weapons)
            {
                var weapon = AssetImportUtil.FindOrCreate<WeaponData>(weaponsFolder, entry.Id, out var wasCreated);
                var changed = wasCreated;

                Set(ref weapon.id, entry.Id, ref changed);
                Set(ref weapon.displayName, entry.Name, ref changed);
                Set(ref weapon.category, EnumParsing.ParseWeaponCategory(entry.Category), ref changed);
                Set(ref weapon.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);
                Set(ref weapon.price, entry.Price, ref changed);
                Set(ref weapon.desc, entry.Desc, ref changed);
                Set(ref weapon.semiAuto, entry.SemiAuto, ref changed);
                Set(ref weapon.melee, entry.Melee, ref changed);
                Set(ref weapon.mag, entry.Stats.Mag, ref changed);
                Set(ref weapon.cooldown, entry.Stats.Cooldown, ref changed);
                Set(ref weapon.reload, entry.Stats.Reload, ref changed);
                Set(ref weapon.dmgMult, entry.Stats.DmgMult, ref changed);
                Set(ref weapon.recoil, entry.Stats.Recoil, ref changed);
                Set(ref weapon.range, entry.Stats.Range, ref changed);
                Set(ref weapon.pellets, entry.Stats.Pellets, ref changed);

                if (wasCreated) report.Created.Add(weapon.id);
                else if (changed) report.Updated.Add(weapon.id);
                else report.Unchanged.Add(weapon.id);

                if (changed) EditorUtility.SetDirty(weapon);
                weapons.Add(weapon);
            }

            AssetImportUtil.ReportOrphans<WeaponData>(weaponsFolder, weapons.Select(w => w.id), report);

            var upgradeConfig = AssetImportUtil.FindOrCreateSingleton<UpgradeConfig>(upgradeConfigPath);
            upgradeConfig.weaponUpgrades = new[]
            {
                ToSpec("damage", root.Upgrades.Damage),
                ToSpec("fireRate", root.Upgrades.FireRate),
                ToSpec("recoil", root.Upgrades.Recoil),
                ToSpec("mag", root.Upgrades.Mag)
            };
            EditorUtility.SetDirty(upgradeConfig);

            AssetDatabase.SaveAssets();

            return new WeaponImportResult { Weapons = weapons, UpgradeConfig = upgradeConfig };
        }

        static WeaponUpgradeSpec ToSpec(string key, UpgradeSpecJson json) => new()
        {
            statKey = key,
            displayName = json.Name,
            maxLevel = json.Max,
            baseCost = json.BaseCost,
            effectDesc = json.Effect
        };

        static void Set<T>(ref T current, T next, ref bool changed)
        {
            if (!EqualityComparer<T>.Default.Equals(current, next))
            {
                current = next;
                changed = true;
            }
        }
    }
}
```

- [ ] **Step 12: Run to verify pass**

`mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.WeaponImporterTests`, poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, 4 tests, 0 failures.

- [ ] **Step 13: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/WeaponData.cs Assets/_Game/Scripts/Data/Config Assets/_Game/Scripts/Editor/Import Assets/_Game/Tests/Editor/Fixtures/weapons.test.json Assets/_Game/Tests/Editor/WeaponImporterTests.cs
git commit -m "feat: import weapons.json into WeaponData + UpgradeConfig"
```

---

## Task 4: Enemies — EnemyData, EnemyCombatConfig, EnemyImporter (resolves weapon refs)

**Files:**
- Create: `Assets/_Game/Scripts/Data/EnemyData.cs`
- Create: `Assets/_Game/Scripts/Data/Config/EnemyCombatConfig.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/JsonModels/EnemiesJson.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/EnemyImporter.cs`
- Create: `Assets/_Game/Tests/Editor/Fixtures/enemies.test.json`
- Modify: `Assets/_Game/Tests/Editor/Fixtures/weapons.test.json` (add a `sniper` entry, referenced by the `tank` enemy)
- Modify: `Assets/_Game/Tests/Editor/WeaponImporterTests.cs` (update weapon count assertion from 3 to 4)
- Test: `Assets/_Game/Tests/Editor/EnemyImporterTests.cs`

**Interfaces:**
- Consumes: `Game.Data.ArmorType` (Task 1); `Game.Data.WeaponData`, `AssetImportUtil.{FindOrCreate,FindOrCreateSingleton,FindById,EnsureFolder}`, `ImportReport`, `JsonFile.Read<T>`, `EnumParsing.ParseArmor` (Task 3). Looks up `WeaponData` by id via `AssetImportUtil.FindById<WeaponData>(weaponsFolder, weaponId)` — requires weapons already imported into that folder.
- Produces: `Game.Data.EnemyData` (fields: `id, displayName, hpMult, speedMult, scale, melee, damageMult, burst, weapon (WeaponData), radarColor, glow, armor, opacity, hasShootOverride, shootCdOverride, shootRangeOverride, behavior, modelPrefab`, method `ResolveModel(PlaceholderLibrary)`). `Game.Data.EnemyCombatConfig`. `EnemyImporter.Import(string jsonPath, string enemiesFolder, string weaponsFolder, string combatConfigPath, ImportReport report) : EnemyImportResult` where `EnemyImportResult { List<EnemyData> Enemies; EnemyCombatConfig CombatConfig; }`.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/Editor/Fixtures/enemies.test.json` (includes the `ghost` entry with missing multipliers, to exercise the default-and-warn path):

```json
{
  "_doc": "Fixture — 3 types d'ennemis représentatifs de unity-spec/enemies.json.",
  "baseStats": {
    "hp": 100, "walkSpeed": 1.8, "runSpeed": 4.0, "radius": 0.45, "turnSpeed": 8,
    "shootRange": 9, "fireCooldown": 1.3, "accuracy": 0.55, "damage": 10, "burstGap": 0.12,
    "detectRadius": 12, "loseRadius": 18, "loseTime": 3.0, "meleeRange": 2.2, "meleeDamage": 14, "meleeCooldown": 0.9
  },
  "hitZones": {
    "head":  { "aboveY": 1.5, "damage": 60 },
    "torso": { "damage": 25 },
    "legs":  { "belowY": 0.6, "damage": 15 }
  },
  "types": {
    "shooter": { "name": "Shooter", "hpMult": 1.0, "speedMult": 1.0, "scale": 1.0, "melee": false, "damageMult": 1.0, "burst": 3, "weapon": "default", "radarColor": "#ffb84d", "glow": null,
                 "behavior": "Tireur standard en rafales de 3.", "assets": { "model": null } },
    "tank": { "name": "Tank", "hpMult": 2.6, "speedMult": 0.55, "scale": 1.34, "melee": false, "damageMult": 1.8, "burst": 2, "weapon": "sniper", "radarColor": "#c07bff", "glow": "0x3a1250", "armor": "torso",
              "behavior": "Lourd et lent, armure de torse.", "assets": { "model": null } },
    "ghost": { "name": "Ghost", "_doc": "Voir main.js pour stats exactes (#107)", "glow": "0x55ddff", "opacity": 0.5,
               "behavior": "Semi-transparent, difficile à voir.", "assets": { "model": null } }
  },
  "vip": { "moneyMult": 2, "minWave": 3, "excluded": ["boss"] },
  "specialConstants": { "bossKillBonus": 300 }
}
```

`tank`'s `weapon` field references `sniper`, which isn't in `weapons.test.json` yet (Task 3's fixture only has `default`, `jackal`, `shotgun`). Reuse that same fixture file rather than creating a second one — modify `Assets/_Game/Tests/Editor/Fixtures/weapons.test.json` to add a fourth entry so `EnemyImporterTests` can resolve the ref:

```json
    { "id": "sniper", "name": "Eclipse SR", "category": "sniper", "rarity": "epique", "price": 1500,
      "desc": "Fusil de précision.",
      "stats": { "mag": 5, "cooldown": 0.85, "reload": 1.9, "dmgMult": 3.0, "recoil": 2.2, "range": 160 },
      "assets": { "model": null, "icon": null } }
```

(Insert it into the `weapons` array of `Assets/_Game/Tests/Editor/Fixtures/weapons.test.json`, after the `shotgun` entry. In `WeaponImporterTests.cs`, update both `Import_CreatesWeaponDataWithMappedFields`'s `Assert.AreEqual(3, result.Weapons.Count)` and `Import_SecondRunWithNoChangesReportsNothingUpdated`'s `Assert.AreEqual(3, secondReport.Unchanged.Count)` from `3` to `4`.)

Create `Assets/_Game/Tests/Editor/EnemyImporterTests.cs`:

```csharp
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class EnemyImporterTests
    {
        const string WeaponsFixture = "Assets/_Game/Tests/Editor/Fixtures/weapons.test.json";
        const string EnemiesFixture = "Assets/_Game/Tests/Editor/Fixtures/enemies.test.json";
        const string WeaponsFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Weapons";
        const string EnemiesFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Enemies";
        const string UpgradeConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/UpgradeConfig.asset";
        const string CombatConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/EnemyCombatConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_CreatesEnemyDataAndResolvesWeaponRef()
        {
            WeaponImporter.Import(WeaponsFixture, WeaponsFolder, UpgradeConfigPath, new ImportReport());
            var report = new ImportReport();

            var result = EnemyImporter.Import(EnemiesFixture, EnemiesFolder, WeaponsFolder, CombatConfigPath, report);

            Assert.AreEqual(3, result.Enemies.Count);
            var tank = result.Enemies.Find(e => e.id == "tank");
            Assert.IsNotNull(tank.weapon);
            Assert.AreEqual("sniper", tank.weapon.id);
            Assert.AreEqual(ArmorType.Torso, tank.armor);
            Assert.AreEqual(2.6f, tank.hpMult, 0.0001f);

            Assert.AreEqual(60, result.CombatConfig.hitZones.headDamage);
            Assert.AreEqual(1.5f, result.CombatConfig.hitZones.headAboveY, 0.0001f);
        }

        [Test]
        public void Import_DefaultsMissingMultipliersAndWarns()
        {
            WeaponImporter.Import(WeaponsFixture, WeaponsFolder, UpgradeConfigPath, new ImportReport());
            var report = new ImportReport();

            var result = EnemyImporter.Import(EnemiesFixture, EnemiesFolder, WeaponsFolder, CombatConfigPath, report);

            var ghost = result.Enemies.Find(e => e.id == "ghost");
            Assert.AreEqual(1.0f, ghost.hpMult, 0.0001f);
            Assert.AreEqual(1.0f, ghost.speedMult, 0.0001f);
            Assert.IsNull(ghost.weapon);
            Assert.IsTrue(report.Warnings.Exists(w => w.Contains("ghost")));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.EnemyImporterTests`. Expected: compile error.

- [ ] **Step 3: Create EnemyData**

Create `Assets/_Game/Scripts/Data/EnemyData.cs`:

```csharp
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        public string id;
        public string displayName;
        public float hpMult;
        public float speedMult;
        public float scale;
        public bool melee;
        public float damageMult;
        public int burst;
        public WeaponData weapon;
        public string radarColor;
        public string glow;
        public ArmorType armor;
        public float opacity = 1f;
        public bool hasShootOverride;
        public float shootCdOverride;
        public float shootRangeOverride;
        public string behavior;

        public GameObject modelPrefab;

        public GameObject ResolveModel(PlaceholderLibrary placeholders) =>
            modelPrefab != null ? modelPrefab : placeholders.GetEnemyPlaceholder();
    }
}
```

- [ ] **Step 4: Create EnemyCombatConfig**

Create `Assets/_Game/Scripts/Data/Config/EnemyCombatConfig.cs`:

```csharp
using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class EnemyBaseStats
    {
        public float hp; public float walkSpeed; public float runSpeed; public float radius;
        public float turnSpeed; public float shootRange; public float fireCooldown; public float accuracy;
        public float damage; public float burstGap; public float detectRadius; public float loseRadius;
        public float loseTime; public float meleeRange; public float meleeDamage; public float meleeCooldown;
    }

    [Serializable]
    public class HitZoneSettings
    {
        public float headAboveY;
        public int headDamage;
        public int torsoDamage;
        public float legsBelowY;
        public int legsDamage;
    }

    [CreateAssetMenu(menuName = "Game/Config/Enemy Combat Config", fileName = "EnemyCombatConfig")]
    public class EnemyCombatConfig : ScriptableObject
    {
        public EnemyBaseStats baseStats;
        public HitZoneSettings hitZones;
        public int vipMoneyMult;
        public int vipMinWave;
        public string[] vipExcludedTypes = Array.Empty<string>();
        public int bossKillBonus;
    }
}
```

- [ ] **Step 5: Create the EnemiesJson POCO model**

Create `Assets/_Game/Scripts/Editor/Import/JsonModels/EnemiesJson.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class EnemiesJsonRoot
    {
        [JsonProperty("baseStats")] public BaseStatsJson BaseStats;
        [JsonProperty("hitZones")] public HitZonesJson HitZones;
        [JsonProperty("types")] public Dictionary<string, EnemyTypeJson> Types;
        [JsonProperty("vip")] public VipJson Vip;
        [JsonProperty("specialConstants")] public SpecialConstantsJson SpecialConstants;
    }

    public class BaseStatsJson
    {
        [JsonProperty("hp")] public float Hp;
        [JsonProperty("walkSpeed")] public float WalkSpeed;
        [JsonProperty("runSpeed")] public float RunSpeed;
        [JsonProperty("radius")] public float Radius;
        [JsonProperty("turnSpeed")] public float TurnSpeed;
        [JsonProperty("shootRange")] public float ShootRange;
        [JsonProperty("fireCooldown")] public float FireCooldown;
        [JsonProperty("accuracy")] public float Accuracy;
        [JsonProperty("damage")] public float Damage;
        [JsonProperty("burstGap")] public float BurstGap;
        [JsonProperty("detectRadius")] public float DetectRadius;
        [JsonProperty("loseRadius")] public float LoseRadius;
        [JsonProperty("loseTime")] public float LoseTime;
        [JsonProperty("meleeRange")] public float MeleeRange;
        [JsonProperty("meleeDamage")] public float MeleeDamage;
        [JsonProperty("meleeCooldown")] public float MeleeCooldown;
    }

    public class HitZonesJson
    {
        [JsonProperty("head")] public HitZoneJson Head;
        [JsonProperty("torso")] public HitZoneJson Torso;
        [JsonProperty("legs")] public HitZoneJson Legs;
    }

    public class HitZoneJson
    {
        [JsonProperty("aboveY")] public float? AboveY;
        [JsonProperty("belowY")] public float? BelowY;
        [JsonProperty("damage")] public int Damage;
    }

    public class EnemyTypeJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("hpMult")] public float? HpMult;
        [JsonProperty("speedMult")] public float? SpeedMult;
        [JsonProperty("scale")] public float? Scale;
        [JsonProperty("melee")] public bool Melee;
        [JsonProperty("damageMult")] public float? DamageMult;
        [JsonProperty("burst")] public int? Burst;
        [JsonProperty("weapon")] public string Weapon;
        [JsonProperty("radarColor")] public string RadarColor;
        [JsonProperty("glow")] public string Glow;
        [JsonProperty("armor")] public string Armor;
        [JsonProperty("shootCd")] public float? ShootCd;
        [JsonProperty("shootRange")] public float? ShootRange;
        [JsonProperty("opacity")] public float? Opacity;
        [JsonProperty("behavior")] public string Behavior;
    }

    public class VipJson
    {
        [JsonProperty("moneyMult")] public int MoneyMult;
        [JsonProperty("minWave")] public int MinWave;
        [JsonProperty("excluded")] public List<string> Excluded;
    }

    public class SpecialConstantsJson
    {
        [JsonProperty("bossKillBonus")] public int BossKillBonus;
    }
}
```

- [ ] **Step 6: Create EnemyImporter**

Create `Assets/_Game/Scripts/Editor/Import/EnemyImporter.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class EnemyImportResult
    {
        public List<EnemyData> Enemies;
        public EnemyCombatConfig CombatConfig;
    }

    public static class EnemyImporter
    {
        public static EnemyImportResult Import(string jsonPath, string enemiesFolder, string weaponsFolder, string combatConfigPath, ImportReport report)
        {
            var root = JsonFile.Read<EnemiesJsonRoot>(jsonPath);
            var enemies = new List<EnemyData>();

            foreach (var kvp in root.Types)
            {
                var id = kvp.Key;
                var entry = kvp.Value;
                var enemy = AssetImportUtil.FindOrCreate<EnemyData>(enemiesFolder, id, out var wasCreated);
                var changed = wasCreated;

                Set(ref enemy.id, id, ref changed);
                Set(ref enemy.displayName, entry.Name, ref changed);

                var hpMult = entry.HpMult ?? WarnDefault(report, id, nameof(entry.HpMult), 1f);
                Set(ref enemy.hpMult, hpMult, ref changed);

                var speedMult = entry.SpeedMult ?? WarnDefault(report, id, nameof(entry.SpeedMult), 1f);
                Set(ref enemy.speedMult, speedMult, ref changed);

                var scale = entry.Scale ?? WarnDefault(report, id, nameof(entry.Scale), 1f);
                Set(ref enemy.scale, scale, ref changed);

                Set(ref enemy.melee, entry.Melee, ref changed);

                var damageMult = entry.DamageMult ?? WarnDefault(report, id, nameof(entry.DamageMult), 1f);
                Set(ref enemy.damageMult, damageMult, ref changed);

                var burst = entry.Burst ?? (int)WarnDefault(report, id, nameof(entry.Burst), 1f);
                Set(ref enemy.burst, burst, ref changed);

                var weapon = entry.Weapon != null ? AssetImportUtil.FindById<WeaponData>(weaponsFolder, entry.Weapon) : null;
                Set(ref enemy.weapon, weapon, ref changed);

                Set(ref enemy.radarColor, entry.RadarColor, ref changed);
                Set(ref enemy.glow, entry.Glow, ref changed);
                Set(ref enemy.armor, EnumParsing.ParseArmor(entry.Armor), ref changed);
                Set(ref enemy.opacity, entry.Opacity ?? 1f, ref changed);
                Set(ref enemy.hasShootOverride, entry.ShootCd.HasValue || entry.ShootRange.HasValue, ref changed);
                Set(ref enemy.shootCdOverride, entry.ShootCd ?? 0f, ref changed);
                Set(ref enemy.shootRangeOverride, entry.ShootRange ?? 0f, ref changed);
                Set(ref enemy.behavior, entry.Behavior, ref changed);

                if (wasCreated) report.Created.Add(enemy.id);
                else if (changed) report.Updated.Add(enemy.id);
                else report.Unchanged.Add(enemy.id);

                if (changed) EditorUtility.SetDirty(enemy);
                enemies.Add(enemy);
            }

            AssetImportUtil.ReportOrphans<EnemyData>(enemiesFolder, enemies.Select(e => e.id), report);

            var combatConfig = AssetImportUtil.FindOrCreateSingleton<EnemyCombatConfig>(combatConfigPath);
            combatConfig.baseStats = new EnemyBaseStats
            {
                hp = root.BaseStats.Hp, walkSpeed = root.BaseStats.WalkSpeed, runSpeed = root.BaseStats.RunSpeed,
                radius = root.BaseStats.Radius, turnSpeed = root.BaseStats.TurnSpeed, shootRange = root.BaseStats.ShootRange,
                fireCooldown = root.BaseStats.FireCooldown, accuracy = root.BaseStats.Accuracy, damage = root.BaseStats.Damage,
                burstGap = root.BaseStats.BurstGap, detectRadius = root.BaseStats.DetectRadius, loseRadius = root.BaseStats.LoseRadius,
                loseTime = root.BaseStats.LoseTime, meleeRange = root.BaseStats.MeleeRange, meleeDamage = root.BaseStats.MeleeDamage,
                meleeCooldown = root.BaseStats.MeleeCooldown
            };
            combatConfig.hitZones = new HitZoneSettings
            {
                headAboveY = root.HitZones.Head.AboveY ?? 0f,
                headDamage = root.HitZones.Head.Damage,
                torsoDamage = root.HitZones.Torso.Damage,
                legsBelowY = root.HitZones.Legs.BelowY ?? 0f,
                legsDamage = root.HitZones.Legs.Damage
            };
            combatConfig.vipMoneyMult = root.Vip.MoneyMult;
            combatConfig.vipMinWave = root.Vip.MinWave;
            combatConfig.vipExcludedTypes = root.Vip.Excluded.ToArray();
            combatConfig.bossKillBonus = root.SpecialConstants.BossKillBonus;
            EditorUtility.SetDirty(combatConfig);

            AssetDatabase.SaveAssets();

            return new EnemyImportResult { Enemies = enemies, CombatConfig = combatConfig };
        }

        static float WarnDefault(ImportReport report, string id, string field, float defaultValue)
        {
            report.Warnings.Add($"{id}: missing {field}, defaulted to {defaultValue}");
            return defaultValue;
        }

        static void Set<T>(ref T current, T next, ref bool changed)
        {
            if (!EqualityComparer<T>.Default.Equals(current, next))
            {
                current = next;
                changed = true;
            }
        }
    }
}
```

- [ ] **Step 7: Run to verify pass**

`mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.EnemyImporterTests` and `Game.Data.Tests.WeaponImporterTests` (the fixture edit in Step 1 touches the weapon count assertion), poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, all tests green.

- [ ] **Step 8: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/EnemyData.cs Assets/_Game/Scripts/Data/Config/EnemyCombatConfig.cs Assets/_Game/Scripts/Editor/Import/JsonModels/EnemiesJson.cs Assets/_Game/Scripts/Editor/Import/EnemyImporter.cs Assets/_Game/Tests/Editor/Fixtures/enemies.test.json Assets/_Game/Tests/Editor/Fixtures/weapons.test.json Assets/_Game/Tests/Editor/EnemyImporterTests.cs Assets/_Game/Tests/Editor/WeaponImporterTests.cs
git commit -m "feat: import enemies.json into EnemyData + EnemyCombatConfig, resolve weapon refs"
```

---

## Task 5: Characters — CharacterData, MovementConfig, CharacterImporter (adds player upgrades)

**Files:**
- Create: `Assets/_Game/Scripts/Data/CharacterData.cs`
- Create: `Assets/_Game/Scripts/Data/Config/MovementConfig.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/JsonModels/PlayerJson.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/CharacterImporter.cs`
- Create: `Assets/_Game/Tests/Editor/Fixtures/player.test.json`
- Test: `Assets/_Game/Tests/Editor/CharacterImporterTests.cs`

**Interfaces:**
- Consumes: `Game.Data.Rarity`, `AssetImportUtil`, `ImportReport`, `JsonFile`, `EnumParsing.ParseRarity`, `Game.Data.UpgradeConfig`/`PlayerUpgradeSpec` (Task 3).
- Produces: `Game.Data.CharacterData` (fields: `id, displayName, price, rarity, desc, baseHealth, baseRegen, baseSpeed, skills: CharacterSkill[], modelPrefab, icon`). `Game.Data.MovementConfig`. `CharacterImporter.Import(string jsonPath, string charactersFolder, string movementConfigPath, string upgradeConfigPath, ImportReport report) : CharacterImportResult` where `CharacterImportResult { List<CharacterData> Characters; MovementConfig MovementConfig; UpgradeConfig UpgradeConfig; }`. This importer loads the *same* `UpgradeConfig` singleton Task 3 wrote to and only replaces `playerUpgrades`, leaving `weaponUpgrades` untouched.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/Editor/Fixtures/player.test.json`:

```json
{
  "characters": [
    { "id": "perso:blue-soldier", "name": "Blue-Soldier", "price": 0, "rarity": "commun",
      "base": { "health": 100, "regen": 10, "speed": 1.00 },
      "desc": "Soldat d'élite polyvalent.",
      "skills": [
        { "icon": "target", "name": "Tireur confirmé", "desc": "Entraînement au tir sans faille." },
        { "icon": "shield", "name": "Discipline", "desc": "Garde son sang-froid sous le feu." }
      ],
      "assets": { "model": "perso/ (GLB actuel)", "icon": null } },
    { "id": "perso:robot", "name": "Cyborg", "price": 3000, "rarity": "epique",
      "base": { "health": 175, "regen": 17, "speed": 1.15 },
      "desc": "Un être augmenté.",
      "skills": [
        { "icon": "heart", "name": "Système optimisé", "desc": "Régénération de santé améliorée." }
      ],
      "assets": { "model": "perso/ (GLB actuel)", "icon": null } }
  ],
  "playerUpgrades": {
    "health": { "name": "Vie", "max": 5, "baseCost": 300, "perLevel": 20 },
    "regen":  { "name": "Régén", "max": 5, "baseCost": 250, "perLevel": 3 },
    "speed":  { "name": "Vitesse", "max": 5, "baseCost": 350, "perLevel": 0.06 }
  },
  "movement": {
    "walkSpeed": 2.4, "runSpeed": 5.4, "jumpSpeed": 6, "gravity": 18, "turnLerp": 12,
    "stepUpMax": 0.55, "stepDownSnap": 0.5,
    "dash": { "distance": 2.6, "speed": 16, "cooldown": 1.4 },
    "stamina": { "max": 100, "drainPerSec": 25, "regenPerSec": 15, "minToRun": 20 },
    "fallDamage": { "safeHeight": 3.0, "damagePerMeter": 5, "maxDamage": 40 }
  },
  "secondWind": { "invulnSeconds": 2, "usesPerRun": 1 }
}
```

Create `Assets/_Game/Tests/Editor/CharacterImporterTests.cs`:

```csharp
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class CharacterImporterTests
    {
        const string WeaponsFixture = "Assets/_Game/Tests/Editor/Fixtures/weapons.test.json";
        const string PlayerFixture = "Assets/_Game/Tests/Editor/Fixtures/player.test.json";
        const string WeaponsFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Weapons";
        const string CharactersFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Characters";
        const string UpgradeConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/UpgradeConfig.asset";
        const string MovementConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/MovementConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_CreatesCharacterDataAndMovementConfig()
        {
            var report = new ImportReport();

            var result = CharacterImporter.Import(PlayerFixture, CharactersFolder, MovementConfigPath, UpgradeConfigPath, report);

            Assert.AreEqual(2, result.Characters.Count);
            var cyborg = result.Characters.Find(c => c.id == "perso:robot");
            Assert.AreEqual("Cyborg", cyborg.displayName);
            Assert.AreEqual(Rarity.Epic, cyborg.rarity);
            Assert.AreEqual(175f, cyborg.baseHealth, 0.0001f);
            Assert.AreEqual(1, cyborg.skills.Length);
            Assert.AreEqual("Système optimisé", cyborg.skills[0].name);

            Assert.AreEqual(2.4f, result.MovementConfig.walkSpeed, 0.0001f);
            Assert.AreEqual(2.6f, result.MovementConfig.dash.distance, 0.0001f);
            Assert.AreEqual(100f, result.MovementConfig.stamina.max, 0.0001f);
            Assert.AreEqual(2f, result.MovementConfig.secondWindInvulnSeconds, 0.0001f);
        }

        [Test]
        public void Import_PreservesWeaponUpgradesAlreadyWrittenByWeaponImporter()
        {
            WeaponImporter.Import(WeaponsFixture, WeaponsFolder, UpgradeConfigPath, new ImportReport());

            var result = CharacterImporter.Import(PlayerFixture, CharactersFolder, MovementConfigPath, UpgradeConfigPath, new ImportReport());

            Assert.AreEqual(4, result.UpgradeConfig.weaponUpgrades.Length);
            Assert.AreEqual(3, result.UpgradeConfig.playerUpgrades.Length);
            var speedSpec = System.Array.Find(result.UpgradeConfig.playerUpgrades, s => s.statKey == "speed");
            Assert.AreEqual(0.06f, speedSpec.perLevel, 0.0001f);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.CharacterImporterTests`. Expected: compile error.

- [ ] **Step 3: Create CharacterData**

Create `Assets/_Game/Scripts/Data/CharacterData.cs`:

```csharp
using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class CharacterSkill
    {
        public string icon;
        public string name;
        public string desc;
    }

    [CreateAssetMenu(menuName = "Game/Character Data", fileName = "CharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string id;
        public string displayName;
        public int price;
        public Rarity rarity;
        public string desc;
        public float baseHealth;
        public float baseRegen;
        public float baseSpeed;
        public CharacterSkill[] skills = Array.Empty<CharacterSkill>();

        public GameObject modelPrefab;
        public Sprite icon;

        public GameObject ResolveModel(PlaceholderLibrary placeholders) =>
            modelPrefab != null ? modelPrefab : placeholders.GetCharacterPlaceholder();

        public Sprite ResolveIcon(PlaceholderLibrary placeholders) =>
            icon != null ? icon : placeholders.GetGenericIcon();
    }
}
```

- [ ] **Step 4: Create MovementConfig**

Create `Assets/_Game/Scripts/Data/Config/MovementConfig.cs`:

```csharp
using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class DashSettings { public float distance; public float speed; public float cooldown; }

    [Serializable]
    public class StaminaSettings { public float max; public float drainPerSec; public float regenPerSec; public float minToRun; }

    [Serializable]
    public class FallDamageSettings { public float safeHeight; public float damagePerMeter; public float maxDamage; }

    [CreateAssetMenu(menuName = "Game/Config/Movement Config", fileName = "MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        public float walkSpeed;
        public float runSpeed;
        public float jumpSpeed;
        public float gravity;
        public float turnLerp;
        public float stepUpMax;
        public float stepDownSnap;
        public DashSettings dash;
        public StaminaSettings stamina;
        public FallDamageSettings fallDamage;
        public float secondWindInvulnSeconds;
        public int secondWindUsesPerRun;
    }
}
```

- [ ] **Step 5: Create the PlayerJson POCO model**

Create `Assets/_Game/Scripts/Editor/Import/JsonModels/PlayerJson.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class PlayerJsonRoot
    {
        [JsonProperty("characters")] public List<CharacterJsonEntry> Characters;
        [JsonProperty("playerUpgrades")] public PlayerUpgradeSpecsJson PlayerUpgrades;
        [JsonProperty("movement")] public MovementJson Movement;
        [JsonProperty("secondWind")] public SecondWindJson SecondWind;
    }

    public class CharacterJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("price")] public int Price;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("base")] public CharacterBaseJson Base;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("skills")] public List<SkillJson> Skills;
    }

    public class CharacterBaseJson
    {
        [JsonProperty("health")] public float Health;
        [JsonProperty("regen")] public float Regen;
        [JsonProperty("speed")] public float Speed;
    }

    public class SkillJson
    {
        [JsonProperty("icon")] public string Icon;
        [JsonProperty("name")] public string Name;
        [JsonProperty("desc")] public string Desc;
    }

    public class PlayerUpgradeSpecsJson
    {
        [JsonProperty("health")] public PlayerUpgradeSpecJson Health;
        [JsonProperty("regen")] public PlayerUpgradeSpecJson Regen;
        [JsonProperty("speed")] public PlayerUpgradeSpecJson Speed;
    }

    public class PlayerUpgradeSpecJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("max")] public int Max;
        [JsonProperty("baseCost")] public int BaseCost;
        [JsonProperty("perLevel")] public float PerLevel;
    }

    public class MovementJson
    {
        [JsonProperty("walkSpeed")] public float WalkSpeed;
        [JsonProperty("runSpeed")] public float RunSpeed;
        [JsonProperty("jumpSpeed")] public float JumpSpeed;
        [JsonProperty("gravity")] public float Gravity;
        [JsonProperty("turnLerp")] public float TurnLerp;
        [JsonProperty("stepUpMax")] public float StepUpMax;
        [JsonProperty("stepDownSnap")] public float StepDownSnap;
        [JsonProperty("dash")] public DashJson Dash;
        [JsonProperty("stamina")] public StaminaJson Stamina;
        [JsonProperty("fallDamage")] public FallDamageJson FallDamage;
    }

    public class DashJson
    {
        [JsonProperty("distance")] public float Distance;
        [JsonProperty("speed")] public float Speed;
        [JsonProperty("cooldown")] public float Cooldown;
    }

    public class StaminaJson
    {
        [JsonProperty("max")] public float Max;
        [JsonProperty("drainPerSec")] public float DrainPerSec;
        [JsonProperty("regenPerSec")] public float RegenPerSec;
        [JsonProperty("minToRun")] public float MinToRun;
    }

    public class FallDamageJson
    {
        [JsonProperty("safeHeight")] public float SafeHeight;
        [JsonProperty("damagePerMeter")] public float DamagePerMeter;
        [JsonProperty("maxDamage")] public float MaxDamage;
    }

    public class SecondWindJson
    {
        [JsonProperty("invulnSeconds")] public float InvulnSeconds;
        [JsonProperty("usesPerRun")] public int UsesPerRun;
    }
}
```

- [ ] **Step 6: Create CharacterImporter**

Create `Assets/_Game/Scripts/Editor/Import/CharacterImporter.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class CharacterImportResult
    {
        public List<CharacterData> Characters;
        public MovementConfig MovementConfig;
        public UpgradeConfig UpgradeConfig;
    }

    public static class CharacterImporter
    {
        public static CharacterImportResult Import(string jsonPath, string charactersFolder, string movementConfigPath, string upgradeConfigPath, ImportReport report)
        {
            var root = JsonFile.Read<PlayerJsonRoot>(jsonPath);
            var characters = new List<CharacterData>();

            foreach (var entry in root.Characters)
            {
                var character = AssetImportUtil.FindOrCreate<CharacterData>(charactersFolder, entry.Id, out var wasCreated);
                var changed = wasCreated;

                Set(ref character.id, entry.Id, ref changed);
                Set(ref character.displayName, entry.Name, ref changed);
                Set(ref character.price, entry.Price, ref changed);
                Set(ref character.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);
                Set(ref character.desc, entry.Desc, ref changed);
                Set(ref character.baseHealth, entry.Base.Health, ref changed);
                Set(ref character.baseRegen, entry.Base.Regen, ref changed);
                Set(ref character.baseSpeed, entry.Base.Speed, ref changed);

                var skills = entry.Skills.Select(s => new CharacterSkill { icon = s.Icon, name = s.Name, desc = s.Desc }).ToArray();
                if (!SkillsEqual(character.skills, skills))
                {
                    character.skills = skills;
                    changed = true;
                }

                if (wasCreated) report.Created.Add(character.id);
                else if (changed) report.Updated.Add(character.id);
                else report.Unchanged.Add(character.id);

                if (changed) EditorUtility.SetDirty(character);
                characters.Add(character);
            }

            AssetImportUtil.ReportOrphans<CharacterData>(charactersFolder, characters.Select(c => c.id), report);

            var movementConfig = AssetImportUtil.FindOrCreateSingleton<MovementConfig>(movementConfigPath);
            movementConfig.walkSpeed = root.Movement.WalkSpeed;
            movementConfig.runSpeed = root.Movement.RunSpeed;
            movementConfig.jumpSpeed = root.Movement.JumpSpeed;
            movementConfig.gravity = root.Movement.Gravity;
            movementConfig.turnLerp = root.Movement.TurnLerp;
            movementConfig.stepUpMax = root.Movement.StepUpMax;
            movementConfig.stepDownSnap = root.Movement.StepDownSnap;
            movementConfig.dash = new DashSettings { distance = root.Movement.Dash.Distance, speed = root.Movement.Dash.Speed, cooldown = root.Movement.Dash.Cooldown };
            movementConfig.stamina = new StaminaSettings { max = root.Movement.Stamina.Max, drainPerSec = root.Movement.Stamina.DrainPerSec, regenPerSec = root.Movement.Stamina.RegenPerSec, minToRun = root.Movement.Stamina.MinToRun };
            movementConfig.fallDamage = new FallDamageSettings { safeHeight = root.Movement.FallDamage.SafeHeight, damagePerMeter = root.Movement.FallDamage.DamagePerMeter, maxDamage = root.Movement.FallDamage.MaxDamage };
            movementConfig.secondWindInvulnSeconds = root.SecondWind.InvulnSeconds;
            movementConfig.secondWindUsesPerRun = root.SecondWind.UsesPerRun;
            EditorUtility.SetDirty(movementConfig);

            var upgradeConfig = AssetImportUtil.FindOrCreateSingleton<UpgradeConfig>(upgradeConfigPath);
            upgradeConfig.playerUpgrades = new[]
            {
                ToSpec("health", root.PlayerUpgrades.Health),
                ToSpec("regen", root.PlayerUpgrades.Regen),
                ToSpec("speed", root.PlayerUpgrades.Speed)
            };
            EditorUtility.SetDirty(upgradeConfig);

            AssetDatabase.SaveAssets();

            return new CharacterImportResult { Characters = characters, MovementConfig = movementConfig, UpgradeConfig = upgradeConfig };
        }

        static PlayerUpgradeSpec ToSpec(string key, PlayerUpgradeSpecJson json) => new()
        {
            statKey = key,
            displayName = json.Name,
            maxLevel = json.Max,
            baseCost = json.BaseCost,
            perLevel = json.PerLevel
        };

        static bool SkillsEqual(CharacterSkill[] a, CharacterSkill[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i].icon != b[i].icon || a[i].name != b[i].name || a[i].desc != b[i].desc)
                    return false;
            return true;
        }

        static void Set<T>(ref T current, T next, ref bool changed)
        {
            if (!EqualityComparer<T>.Default.Equals(current, next))
            {
                current = next;
                changed = true;
            }
        }
    }
}
```

- [ ] **Step 7: Run to verify pass**

`mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.CharacterImporterTests`, poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, 2 tests, 0 failures.

- [ ] **Step 8: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/CharacterData.cs Assets/_Game/Scripts/Data/Config/MovementConfig.cs Assets/_Game/Scripts/Editor/Import/JsonModels/PlayerJson.cs Assets/_Game/Scripts/Editor/Import/CharacterImporter.cs Assets/_Game/Tests/Editor/Fixtures/player.test.json Assets/_Game/Tests/Editor/CharacterImporterTests.cs
git commit -m "feat: import player.json into CharacterData + MovementConfig, add player upgrades"
```

---

## Task 6: Economy — EconomyConfig, EconomyConfigImporter

**Files:**
- Create: `Assets/_Game/Scripts/Data/Config/EconomyConfig.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/JsonModels/EconomyJson.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/EconomyConfigImporter.cs`
- Create: `Assets/_Game/Tests/Editor/Fixtures/economy.test.json`
- Test: `Assets/_Game/Tests/Editor/EconomyConfigImporterTests.cs`

**Interfaces:**
- Consumes: `Game.Data.Rarity`, `AssetImportUtil.FindOrCreateSingleton`, `ImportReport`, `JsonFile.Read<T>`, `EnumParsing.ParseRarity` (Tasks 1, 3).
- Produces: `Game.Data.EconomyConfig`. `EconomyConfigImporter.Import(string jsonPath, string configPath, ImportReport report) : EconomyConfig`.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/Editor/Fixtures/economy.test.json`:

```json
{
  "money": { "perKill": 10, "vipMult": 2, "bossKillBonus": 300 },
  "xp": { "maxLevel": 50 },
  "difficulties": {
    "facile":    { "name": "Facile",    "enemyHp": 0.7, "enemyDmg": 0.7, "money": 1.2 },
    "normal":    { "name": "Normal",    "enemyHp": 1.0, "enemyDmg": 1.0, "money": 1.0 },
    "difficile": { "name": "Difficile", "enemyHp": 1.4, "enemyDmg": 1.5, "money": 0.9 }
  },
  "rarities": {
    "commun":     { "name": "Commun",     "color": "#9aa3b2", "tier": 0 },
    "rare":       { "name": "Rare",       "color": "#4f9dff", "tier": 1 },
    "epique":     { "name": "Épique",     "color": "#b066ff", "tier": 2 },
    "legendaire": { "name": "Légendaire", "color": "#ffb02e", "tier": 3 }
  },
  "leaderboard": { "maxEntries": 5 },
  "loadout": { "thresholds": { "mine": 600, "dog": 900, "carepackage": 1000, "turret": 1200, "airstrike": 1800 } }
}
```

Create `Assets/_Game/Tests/Editor/EconomyConfigImporterTests.cs`:

```csharp
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class EconomyConfigImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/economy.test.json";
        const string ConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/EconomyConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_MapsMoneyXpDifficultiesRaritiesAndLoadout()
        {
            var config = EconomyConfigImporter.Import(Fixture, ConfigPath, new ImportReport());

            Assert.AreEqual(10, config.perKillMoney);
            Assert.AreEqual(2, config.vipMoneyMult);
            Assert.AreEqual(300, config.bossKillBonus);
            Assert.AreEqual(50, config.maxLevel);
            Assert.AreEqual(5, config.leaderboardMaxEntries);

            Assert.AreEqual(3, config.difficulties.Length);
            var hard = System.Array.Find(config.difficulties, d => d.id == "difficile");
            Assert.AreEqual(1.4f, hard.enemyHpMult, 0.0001f);

            Assert.AreEqual(4, config.rarities.Length);
            var epic = System.Array.Find(config.rarities, r => r.rarity == Rarity.Epic);
            Assert.AreEqual(2, epic.tier);

            Assert.AreEqual(1800, config.loadoutThresholds.airstrike);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.EconomyConfigImporterTests`. Expected: compile error.

- [ ] **Step 3: Create EconomyConfig**

Create `Assets/_Game/Scripts/Data/Config/EconomyConfig.cs`:

```csharp
using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class DifficultySettings
    {
        public string id;
        public string displayName;
        public float enemyHpMult;
        public float enemyDmgMult;
        public float moneyMult;
    }

    [Serializable]
    public class RaritySettings
    {
        public Rarity rarity;
        public string displayName;
        public string colorHex;
        public int tier;
    }

    [Serializable]
    public class LoadoutThresholds
    {
        public int mine;
        public int dog;
        public int carepackage;
        public int turret;
        public int airstrike;
    }

    [CreateAssetMenu(menuName = "Game/Config/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        public int perKillMoney;
        public int vipMoneyMult;
        public int bossKillBonus;
        public int maxLevel;
        public int leaderboardMaxEntries;
        public DifficultySettings[] difficulties = Array.Empty<DifficultySettings>();
        public RaritySettings[] rarities = Array.Empty<RaritySettings>();
        public LoadoutThresholds loadoutThresholds;
    }
}
```

- [ ] **Step 4: Create the EconomyJson POCO model**

Create `Assets/_Game/Scripts/Editor/Import/JsonModels/EconomyJson.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class EconomyJsonRoot
    {
        [JsonProperty("money")] public MoneyJson Money;
        [JsonProperty("xp")] public XpJson Xp;
        [JsonProperty("difficulties")] public Dictionary<string, DifficultyJson> Difficulties;
        [JsonProperty("rarities")] public Dictionary<string, RarityJson> Rarities;
        [JsonProperty("leaderboard")] public LeaderboardJson Leaderboard;
        [JsonProperty("loadout")] public LoadoutJson Loadout;
    }

    public class MoneyJson
    {
        [JsonProperty("perKill")] public int PerKill;
        [JsonProperty("vipMult")] public int VipMult;
        [JsonProperty("bossKillBonus")] public int BossKillBonus;
    }

    public class XpJson
    {
        [JsonProperty("maxLevel")] public int MaxLevel;
    }

    public class DifficultyJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("enemyHp")] public float EnemyHp;
        [JsonProperty("enemyDmg")] public float EnemyDmg;
        [JsonProperty("money")] public float Money;
    }

    public class RarityJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("color")] public string Color;
        [JsonProperty("tier")] public int Tier;
    }

    public class LeaderboardJson
    {
        [JsonProperty("maxEntries")] public int MaxEntries;
    }

    public class LoadoutJson
    {
        [JsonProperty("thresholds")] public ThresholdsJson Thresholds;
    }

    public class ThresholdsJson
    {
        [JsonProperty("mine")] public int Mine;
        [JsonProperty("dog")] public int Dog;
        [JsonProperty("carepackage")] public int Carepackage;
        [JsonProperty("turret")] public int Turret;
        [JsonProperty("airstrike")] public int Airstrike;
    }
}
```

- [ ] **Step 5: Create EconomyConfigImporter**

Create `Assets/_Game/Scripts/Editor/Import/EconomyConfigImporter.cs`:

```csharp
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public static class EconomyConfigImporter
    {
        public static EconomyConfig Import(string jsonPath, string configPath, ImportReport report)
        {
            var root = JsonFile.Read<EconomyJsonRoot>(jsonPath);
            var config = AssetImportUtil.FindOrCreateSingleton<EconomyConfig>(configPath);

            config.perKillMoney = root.Money.PerKill;
            config.vipMoneyMult = root.Money.VipMult;
            config.bossKillBonus = root.Money.BossKillBonus;
            config.maxLevel = root.Xp.MaxLevel;
            config.leaderboardMaxEntries = root.Leaderboard.MaxEntries;

            config.difficulties = root.Difficulties.Select(kvp => new DifficultySettings
            {
                id = kvp.Key,
                displayName = kvp.Value.Name,
                enemyHpMult = kvp.Value.EnemyHp,
                enemyDmgMult = kvp.Value.EnemyDmg,
                moneyMult = kvp.Value.Money
            }).ToArray();

            config.rarities = root.Rarities.Select(kvp => new RaritySettings
            {
                rarity = EnumParsing.ParseRarity(kvp.Key),
                displayName = kvp.Value.Name,
                colorHex = kvp.Value.Color,
                tier = kvp.Value.Tier
            }).ToArray();

            config.loadoutThresholds = new LoadoutThresholds
            {
                mine = root.Loadout.Thresholds.Mine,
                dog = root.Loadout.Thresholds.Dog,
                carepackage = root.Loadout.Thresholds.Carepackage,
                turret = root.Loadout.Thresholds.Turret,
                airstrike = root.Loadout.Thresholds.Airstrike
            };

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            report.Updated.Add("EconomyConfig");

            return config;
        }
    }
}
```

- [ ] **Step 6: Run to verify pass**

`mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.EconomyConfigImporterTests`, poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, 1 test, 0 failures.

- [ ] **Step 7: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/Config/EconomyConfig.cs Assets/_Game/Scripts/Editor/Import/JsonModels/EconomyJson.cs Assets/_Game/Scripts/Editor/Import/EconomyConfigImporter.cs Assets/_Game/Tests/Editor/Fixtures/economy.test.json Assets/_Game/Tests/Editor/EconomyConfigImporterTests.cs
git commit -m "feat: import economy.json into EconomyConfig"
```

---

## Task 7: Items — AccessoryData, BoostData, ConsumableData, DanceData, ItemsImporter

**Files:**
- Create: `Assets/_Game/Scripts/Data/AccessoryData.cs`
- Create: `Assets/_Game/Scripts/Data/BoostData.cs`
- Create: `Assets/_Game/Scripts/Data/ConsumableData.cs`
- Create: `Assets/_Game/Scripts/Data/DanceData.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/JsonModels/ItemsJson.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/ItemsImporter.cs`
- Create: `Assets/_Game/Tests/Editor/Fixtures/items.test.json`
- Test: `Assets/_Game/Tests/Editor/ItemsImporterTests.cs`

**Interfaces:**
- Consumes: `Game.Data.Rarity`, `Game.Data.BoostSlot`, `Game.Data.BoostStat`, `AssetImportUtil`, `ImportReport`, `JsonFile.Read<T>`, `EnumParsing.{ParseRarity, ParseBoostSlot, ParseBoostStat}` (Tasks 1, 3).
- Produces: `Game.Data.AccessoryData` (fields: `id, displayName, accessoryType (AccessoryType enum: Helmet/Costume), price, rarity, desc, bonuses: AccessoryBonus[], image, modelPrefab`). `Game.Data.BoostData` (fields: `id, slot, displayName, stat, bonus, price, rarity, icon`). `Game.Data.ConsumableData` (fields: `key, displayName, price, iconName, icon`). `Game.Data.DanceData` (fields: `id, displayName, clipName, price, rarity, animationClip`). `ItemsImporter.Import(string jsonPath, string accessoriesFolder, string boostsFolder, string consumablesFolder, string dancesFolder, ImportReport report) : ItemsImportResult` where `ItemsImportResult { List<AccessoryData> Accessories; List<BoostData> Boosts; List<ConsumableData> Consumables; List<DanceData> Dances; }`.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/Editor/Fixtures/items.test.json`:

```json
{
  "accessories": {
    "helmets": [
      { "id": "helm:none", "name": "Aucun", "price": 0, "rarity": "commun", "desc": "Visage à découvert.", "bonuses": [],
        "assets": { "image": null, "model": null } },
      { "id": "helm:insect", "name": "Insectoïde", "price": 400, "rarity": "rare",
        "desc": "Masque de chasse aux reflets chitineux.",
        "bonuses": [
          { "icon": "eye", "name": "Vision de traque", "desc": "Repère ta cible dans le chaos." }
        ],
        "assets": { "image": "artefact/accessory/mask/insect-viber/insect-viber.png", "model": null } }
    ],
    "costumes": [
      { "id": "cost:none", "name": "Aucun", "price": 0, "rarity": "commun", "desc": "Tenue standard.", "bonuses": [],
        "assets": { "image": null, "model": null } }
    ]
  },
  "boosts": {
    "items": [
      { "id": "boost:vest1", "slot": "vest", "name": "Gilet léger", "stat": "health", "bonus": 25, "price": 400, "rarity": "rare" },
      { "id": "boost:boots1", "slot": "boots", "name": "Bottes", "stat": "speed", "bonus": 0.08, "price": 400, "rarity": "rare" }
    ]
  },
  "consumables": {
    "items": [
      { "key": "frag", "name": "Grenade frag", "price": 50, "assets": { "icon": "grenade" } },
      { "key": "medkit", "name": "Trousse de soin", "price": 80, "assets": { "icon": "heart" } }
    ]
  },
  "dances": {
    "items": [
      { "id": "dance:all_night", "name": "All Night", "clip": "All_Night_Dance", "price": 0, "rarity": "commun" },
      { "id": "dance:skill1", "name": "Skill I", "clip": "Skill_01", "price": 500, "rarity": "rare" }
    ]
  }
}
```

Create `Assets/_Game/Tests/Editor/ItemsImporterTests.cs`:

```csharp
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class ItemsImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/items.test.json";
        const string Root = "Assets/_Game/Tests/Editor/Fixtures/Generated";
        const string AccessoriesFolder = Root + "/Accessories";
        const string BoostsFolder = Root + "/Boosts";
        const string ConsumablesFolder = Root + "/Consumables";
        const string DancesFolder = Root + "/Dances";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Root);
        }

        [Test]
        public void Import_CreatesAllFourItemCatalogs()
        {
            var result = ItemsImporter.Import(Fixture, AccessoriesFolder, BoostsFolder, ConsumablesFolder, DancesFolder, new ImportReport());

            Assert.AreEqual(3, result.Accessories.Count);
            var insectHelm = result.Accessories.Find(a => a.id == "helm:insect");
            Assert.AreEqual(AccessoryType.Helmet, insectHelm.accessoryType);
            Assert.AreEqual(1, insectHelm.bonuses.Length);
            var noneCostume = result.Accessories.Find(a => a.id == "cost:none");
            Assert.AreEqual(AccessoryType.Costume, noneCostume.accessoryType);

            Assert.AreEqual(2, result.Boosts.Count);
            var vest1 = result.Boosts.Find(b => b.id == "boost:vest1");
            Assert.AreEqual(BoostSlot.Vest, vest1.slot);
            Assert.AreEqual(BoostStat.Health, vest1.stat);
            Assert.AreEqual(25f, vest1.bonus, 0.0001f);

            Assert.AreEqual(2, result.Consumables.Count);
            var frag = result.Consumables.Find(c => c.key == "frag");
            Assert.AreEqual("grenade", frag.iconName);

            Assert.AreEqual(2, result.Dances.Count);
            var skill1 = result.Dances.Find(d => d.id == "dance:skill1");
            Assert.AreEqual("Skill_01", skill1.clipName);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.ItemsImporterTests`. Expected: compile error.

- [ ] **Step 3: Create AccessoryData**

Create `Assets/_Game/Scripts/Data/AccessoryData.cs`:

```csharp
using System;
using UnityEngine;

namespace Game.Data
{
    public enum AccessoryType { Helmet, Costume }

    [Serializable]
    public class AccessoryBonus
    {
        public string icon;
        public string name;
        public string desc;
    }

    [CreateAssetMenu(menuName = "Game/Accessory Data", fileName = "AccessoryData")]
    public class AccessoryData : ScriptableObject
    {
        public string id;
        public string displayName;
        public AccessoryType accessoryType;
        public int price;
        public Rarity rarity;
        public string desc;
        public AccessoryBonus[] bonuses = Array.Empty<AccessoryBonus>();

        public Sprite image;
        public GameObject modelPrefab;
    }
}
```

- [ ] **Step 4: Create BoostData**

Create `Assets/_Game/Scripts/Data/BoostData.cs`:

```csharp
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Boost Data", fileName = "BoostData")]
    public class BoostData : ScriptableObject
    {
        public string id;
        public BoostSlot slot;
        public string displayName;
        public BoostStat stat;
        public float bonus;
        public int price;
        public Rarity rarity;

        public Sprite icon;
    }
}
```

- [ ] **Step 5: Create ConsumableData**

Create `Assets/_Game/Scripts/Data/ConsumableData.cs`:

```csharp
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Consumable Data", fileName = "ConsumableData")]
    public class ConsumableData : ScriptableObject
    {
        public string key;
        public string displayName;
        public int price;
        public string iconName;

        public Sprite icon;
    }
}
```

- [ ] **Step 6: Create DanceData**

Create `Assets/_Game/Scripts/Data/DanceData.cs`:

```csharp
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Dance Data", fileName = "DanceData")]
    public class DanceData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string clipName;
        public int price;
        public Rarity rarity;

        public AnimationClip animationClip;
    }
}
```

- [ ] **Step 7: Create the ItemsJson POCO model**

Create `Assets/_Game/Scripts/Editor/Import/JsonModels/ItemsJson.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class ItemsJsonRoot
    {
        [JsonProperty("accessories")] public AccessoriesJson Accessories;
        [JsonProperty("boosts")] public BoostsJson Boosts;
        [JsonProperty("consumables")] public ConsumablesJson Consumables;
        [JsonProperty("dances")] public DancesJson Dances;
    }

    public class AccessoriesJson
    {
        [JsonProperty("helmets")] public List<AccessoryJsonEntry> Helmets;
        [JsonProperty("costumes")] public List<AccessoryJsonEntry> Costumes;
    }

    public class AccessoryJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("price")] public int Price;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("bonuses")] public List<BonusJson> Bonuses;
    }

    public class BonusJson
    {
        [JsonProperty("icon")] public string Icon;
        [JsonProperty("name")] public string Name;
        [JsonProperty("desc")] public string Desc;
    }

    public class BoostsJson
    {
        [JsonProperty("items")] public List<BoostJsonEntry> Items;
    }

    public class BoostJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("slot")] public string Slot;
        [JsonProperty("name")] public string Name;
        [JsonProperty("stat")] public string Stat;
        [JsonProperty("bonus")] public float Bonus;
        [JsonProperty("price")] public int Price;
        [JsonProperty("rarity")] public string Rarity;
    }

    public class ConsumablesJson
    {
        [JsonProperty("items")] public List<ConsumableJsonEntry> Items;
    }

    public class ConsumableJsonEntry
    {
        [JsonProperty("key")] public string Key;
        [JsonProperty("name")] public string Name;
        [JsonProperty("price")] public int Price;
        [JsonProperty("assets")] public ConsumableAssetsJson Assets;
    }

    public class ConsumableAssetsJson
    {
        [JsonProperty("icon")] public string Icon;
    }

    public class DancesJson
    {
        [JsonProperty("items")] public List<DanceJsonEntry> Items;
    }

    public class DanceJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("clip")] public string Clip;
        [JsonProperty("price")] public int Price;
        [JsonProperty("rarity")] public string Rarity;
    }
}
```

- [ ] **Step 8: Create ItemsImporter**

Create `Assets/_Game/Scripts/Editor/Import/ItemsImporter.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class ItemsImportResult
    {
        public List<AccessoryData> Accessories;
        public List<BoostData> Boosts;
        public List<ConsumableData> Consumables;
        public List<DanceData> Dances;
    }

    public static class ItemsImporter
    {
        public static ItemsImportResult Import(string jsonPath, string accessoriesFolder, string boostsFolder, string consumablesFolder, string dancesFolder, ImportReport report)
        {
            var root = JsonFile.Read<ItemsJsonRoot>(jsonPath);

            var accessories = new List<AccessoryData>();
            accessories.AddRange(root.Accessories.Helmets.Select(e => ImportAccessory(e, AccessoryType.Helmet, accessoriesFolder, report)));
            accessories.AddRange(root.Accessories.Costumes.Select(e => ImportAccessory(e, AccessoryType.Costume, accessoriesFolder, report)));

            var boosts = root.Boosts.Items.Select(e => ImportBoost(e, boostsFolder, report)).ToList();
            var consumables = root.Consumables.Items.Select(e => ImportConsumable(e, consumablesFolder, report)).ToList();
            var dances = root.Dances.Items.Select(e => ImportDance(e, dancesFolder, report)).ToList();

            AssetImportUtil.ReportOrphans<AccessoryData>(accessoriesFolder, accessories.Select(a => a.id), report);
            AssetImportUtil.ReportOrphans<BoostData>(boostsFolder, boosts.Select(b => b.id), report);
            AssetImportUtil.ReportOrphans<ConsumableData>(consumablesFolder, consumables.Select(c => c.key), report);
            AssetImportUtil.ReportOrphans<DanceData>(dancesFolder, dances.Select(d => d.id), report);

            AssetDatabase.SaveAssets();

            return new ItemsImportResult { Accessories = accessories, Boosts = boosts, Consumables = consumables, Dances = dances };
        }

        static AccessoryData ImportAccessory(AccessoryJsonEntry entry, AccessoryType type, string folder, ImportReport report)
        {
            var accessory = AssetImportUtil.FindOrCreate<AccessoryData>(folder, entry.Id, out var wasCreated);
            var changed = wasCreated;

            Set(ref accessory.id, entry.Id, ref changed);
            Set(ref accessory.displayName, entry.Name, ref changed);
            Set(ref accessory.accessoryType, type, ref changed);
            Set(ref accessory.price, entry.Price, ref changed);
            Set(ref accessory.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);
            Set(ref accessory.desc, entry.Desc, ref changed);

            var bonuses = entry.Bonuses.Select(b => new AccessoryBonus { icon = b.Icon, name = b.Name, desc = b.Desc }).ToArray();
            if (!BonusesEqual(accessory.bonuses, bonuses))
            {
                accessory.bonuses = bonuses;
                changed = true;
            }

            Report(report, accessory.id, wasCreated, changed);
            if (changed) EditorUtility.SetDirty(accessory);
            return accessory;
        }

        static BoostData ImportBoost(BoostJsonEntry entry, string folder, ImportReport report)
        {
            var boost = AssetImportUtil.FindOrCreate<BoostData>(folder, entry.Id, out var wasCreated);
            var changed = wasCreated;

            Set(ref boost.id, entry.Id, ref changed);
            Set(ref boost.slot, EnumParsing.ParseBoostSlot(entry.Slot), ref changed);
            Set(ref boost.displayName, entry.Name, ref changed);
            Set(ref boost.stat, EnumParsing.ParseBoostStat(entry.Stat), ref changed);
            Set(ref boost.bonus, entry.Bonus, ref changed);
            Set(ref boost.price, entry.Price, ref changed);
            Set(ref boost.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);

            Report(report, boost.id, wasCreated, changed);
            if (changed) EditorUtility.SetDirty(boost);
            return boost;
        }

        static ConsumableData ImportConsumable(ConsumableJsonEntry entry, string folder, ImportReport report)
        {
            var consumable = AssetImportUtil.FindOrCreate<ConsumableData>(folder, entry.Key, out var wasCreated);
            var changed = wasCreated;

            Set(ref consumable.key, entry.Key, ref changed);
            Set(ref consumable.displayName, entry.Name, ref changed);
            Set(ref consumable.price, entry.Price, ref changed);
            Set(ref consumable.iconName, entry.Assets.Icon, ref changed);

            Report(report, consumable.key, wasCreated, changed);
            if (changed) EditorUtility.SetDirty(consumable);
            return consumable;
        }

        static DanceData ImportDance(DanceJsonEntry entry, string folder, ImportReport report)
        {
            var dance = AssetImportUtil.FindOrCreate<DanceData>(folder, entry.Id, out var wasCreated);
            var changed = wasCreated;

            Set(ref dance.id, entry.Id, ref changed);
            Set(ref dance.displayName, entry.Name, ref changed);
            Set(ref dance.clipName, entry.Clip, ref changed);
            Set(ref dance.price, entry.Price, ref changed);
            Set(ref dance.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);

            Report(report, dance.id, wasCreated, changed);
            if (changed) EditorUtility.SetDirty(dance);
            return dance;
        }

        static bool BonusesEqual(AccessoryBonus[] a, AccessoryBonus[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i].icon != b[i].icon || a[i].name != b[i].name || a[i].desc != b[i].desc)
                    return false;
            return true;
        }

        static void Report(ImportReport report, string id, bool wasCreated, bool changed)
        {
            if (wasCreated) report.Created.Add(id);
            else if (changed) report.Updated.Add(id);
            else report.Unchanged.Add(id);
        }

        static void Set<T>(ref T current, T next, ref bool changed)
        {
            if (!EqualityComparer<T>.Default.Equals(current, next))
            {
                current = next;
                changed = true;
            }
        }
    }
}
```

- [ ] **Step 9: Run to verify pass**

`mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.ItemsImporterTests`, poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, 1 test, 0 failures.

- [ ] **Step 10: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/AccessoryData.cs Assets/_Game/Scripts/Data/BoostData.cs Assets/_Game/Scripts/Data/ConsumableData.cs Assets/_Game/Scripts/Data/DanceData.cs Assets/_Game/Scripts/Editor/Import/JsonModels/ItemsJson.cs Assets/_Game/Scripts/Editor/Import/ItemsImporter.cs Assets/_Game/Tests/Editor/Fixtures/items.test.json Assets/_Game/Tests/Editor/ItemsImporterTests.cs
git commit -m "feat: import items.json into Accessory/Boost/Consumable/Dance data"
```

---

## Task 8: Power-ups — PowerUpData, PowerUpConfig, PowerUpImporter

**Files:**
- Create: `Assets/_Game/Scripts/Data/PowerUpData.cs`
- Create: `Assets/_Game/Scripts/Data/Config/PowerUpConfig.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/JsonModels/PowerUpsJson.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/PowerUpImporter.cs`
- Create: `Assets/_Game/Tests/Editor/Fixtures/powerups.test.json`
- Test: `Assets/_Game/Tests/Editor/PowerUpImporterTests.cs`

**Interfaces:**
- Consumes: `Game.Data.PvpPolicy`, `AssetImportUtil`, `ImportReport`, `JsonFile.Read<T>`, `EnumParsing.ParsePvpPolicy` (Tasks 1, 3).
- Produces: `Game.Data.PowerUpData` (fields: `id, displayName, colorHex, duration, isInstant, effectDesc, pvpPolicy`). `Game.Data.PowerUpConfig`. `PowerUpImporter.Import(string jsonPath, string powerUpsFolder, string configPath, ImportReport report) : PowerUpImportResult` where `PowerUpImportResult { List<PowerUpData> PowerUps; PowerUpConfig Config; }`.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/Editor/Fixtures/powerups.test.json`:

```json
{
  "dropChance": 0.12,
  "dropChanceLootMaster": 0.24,
  "pickupRadius": 1.6,
  "defaultDuration": 8,
  "powerups": {
    "dmgx2":        { "name": "×2 Dégâts",     "color": "#ff8a2a", "duration": 8, "effect": "Dégâts × 2", "pvp": "rebalance" },
    "shield_orb":   { "name": "Bouclier Max",  "color": "#4488ff", "instant": true, "effect": "Bouclier rechargé au max", "pvp": "allowed" },
    "slowmo":       { "name": "Slow-mo",       "color": "#9a6aff", "duration": 8, "effect": "Ennemis ralentis à 40%", "pvp": "exclude" }
  },
  "instantConstants": { "instantHealAmount": 40, "novaRadius": 8, "novaDamage": 80 }
}
```

Create `Assets/_Game/Tests/Editor/PowerUpImporterTests.cs`:

```csharp
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class PowerUpImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/powerups.test.json";
        const string PowerUpsFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/PowerUps";
        const string ConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/PowerUpConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_CreatesPowerUpDataAndConfig()
        {
            var result = PowerUpImporter.Import(Fixture, PowerUpsFolder, ConfigPath, new ImportReport());

            Assert.AreEqual(3, result.PowerUps.Count);
            var shield = result.PowerUps.Find(p => p.id == "shield_orb");
            Assert.IsTrue(shield.isInstant);
            Assert.AreEqual(PvpPolicy.Allowed, shield.pvpPolicy);
            var dmgx2 = result.PowerUps.Find(p => p.id == "dmgx2");
            Assert.AreEqual(8f, dmgx2.duration, 0.0001f);
            Assert.AreEqual(PvpPolicy.Rebalance, dmgx2.pvpPolicy);

            Assert.AreEqual(0.12f, result.Config.dropChance, 0.0001f);
            Assert.AreEqual(0.24f, result.Config.dropChanceLootMaster, 0.0001f);
            Assert.AreEqual(80, result.Config.novaDamage);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.PowerUpImporterTests`. Expected: compile error.

- [ ] **Step 3: Create PowerUpData**

Create `Assets/_Game/Scripts/Data/PowerUpData.cs`:

```csharp
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/PowerUp Data", fileName = "PowerUpData")]
    public class PowerUpData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string colorHex;
        public float duration;
        public bool isInstant;
        public string effectDesc;
        public PvpPolicy pvpPolicy;
    }
}
```

- [ ] **Step 4: Create PowerUpConfig**

Create `Assets/_Game/Scripts/Data/Config/PowerUpConfig.cs`:

```csharp
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Config/PowerUp Config", fileName = "PowerUpConfig")]
    public class PowerUpConfig : ScriptableObject
    {
        public float dropChance;
        public float dropChanceLootMaster;
        public float pickupRadius;
        public float defaultDuration;
        public int instantHealAmount;
        public float novaRadius;
        public int novaDamage;
    }
}
```

- [ ] **Step 5: Create the PowerUpsJson POCO model**

Create `Assets/_Game/Scripts/Editor/Import/JsonModels/PowerUpsJson.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class PowerUpsJsonRoot
    {
        [JsonProperty("dropChance")] public float DropChance;
        [JsonProperty("dropChanceLootMaster")] public float DropChanceLootMaster;
        [JsonProperty("pickupRadius")] public float PickupRadius;
        [JsonProperty("defaultDuration")] public float DefaultDuration;
        [JsonProperty("powerups")] public Dictionary<string, PowerUpJsonEntry> PowerUps;
        [JsonProperty("instantConstants")] public InstantConstantsJson InstantConstants;
    }

    public class PowerUpJsonEntry
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("color")] public string Color;
        [JsonProperty("duration")] public float? Duration;
        [JsonProperty("instant")] public bool? Instant;
        [JsonProperty("effect")] public string Effect;
        [JsonProperty("pvp")] public string Pvp;
    }

    public class InstantConstantsJson
    {
        [JsonProperty("instantHealAmount")] public int InstantHealAmount;
        [JsonProperty("novaRadius")] public float NovaRadius;
        [JsonProperty("novaDamage")] public int NovaDamage;
    }
}
```

- [ ] **Step 6: Create PowerUpImporter**

Create `Assets/_Game/Scripts/Editor/Import/PowerUpImporter.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class PowerUpImportResult
    {
        public List<PowerUpData> PowerUps;
        public PowerUpConfig Config;
    }

    public static class PowerUpImporter
    {
        public static PowerUpImportResult Import(string jsonPath, string powerUpsFolder, string configPath, ImportReport report)
        {
            var root = JsonFile.Read<PowerUpsJsonRoot>(jsonPath);
            var powerUps = new List<PowerUpData>();

            foreach (var kvp in root.PowerUps)
            {
                var id = kvp.Key;
                var entry = kvp.Value;
                var powerUp = AssetImportUtil.FindOrCreate<PowerUpData>(powerUpsFolder, id, out var wasCreated);
                var changed = wasCreated;

                Set(ref powerUp.id, id, ref changed);
                Set(ref powerUp.displayName, entry.Name, ref changed);
                Set(ref powerUp.colorHex, entry.Color, ref changed);
                Set(ref powerUp.duration, entry.Duration ?? 0f, ref changed);
                Set(ref powerUp.isInstant, entry.Instant ?? false, ref changed);
                Set(ref powerUp.effectDesc, entry.Effect, ref changed);
                Set(ref powerUp.pvpPolicy, EnumParsing.ParsePvpPolicy(entry.Pvp), ref changed);

                if (wasCreated) report.Created.Add(powerUp.id);
                else if (changed) report.Updated.Add(powerUp.id);
                else report.Unchanged.Add(powerUp.id);

                if (changed) EditorUtility.SetDirty(powerUp);
                powerUps.Add(powerUp);
            }

            AssetImportUtil.ReportOrphans<PowerUpData>(powerUpsFolder, powerUps.Select(p => p.id), report);

            var config = AssetImportUtil.FindOrCreateSingleton<PowerUpConfig>(configPath);
            config.dropChance = root.DropChance;
            config.dropChanceLootMaster = root.DropChanceLootMaster;
            config.pickupRadius = root.PickupRadius;
            config.defaultDuration = root.DefaultDuration;
            config.instantHealAmount = root.InstantConstants.InstantHealAmount;
            config.novaRadius = root.InstantConstants.NovaRadius;
            config.novaDamage = root.InstantConstants.NovaDamage;
            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();

            return new PowerUpImportResult { PowerUps = powerUps, Config = config };
        }

        static void Set<T>(ref T current, T next, ref bool changed)
        {
            if (!EqualityComparer<T>.Default.Equals(current, next))
            {
                current = next;
                changed = true;
            }
        }
    }
}
```

- [ ] **Step 7: Run to verify pass**

`mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.PowerUpImporterTests`, poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, 1 test, 0 failures.

- [ ] **Step 8: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/PowerUpData.cs Assets/_Game/Scripts/Data/Config/PowerUpConfig.cs Assets/_Game/Scripts/Editor/Import/JsonModels/PowerUpsJson.cs Assets/_Game/Scripts/Editor/Import/PowerUpImporter.cs Assets/_Game/Tests/Editor/Fixtures/powerups.test.json Assets/_Game/Tests/Editor/PowerUpImporterTests.cs
git commit -m "feat: import powerups.json into PowerUpData + PowerUpConfig"
```

---

## Task 9: Waves — WaveConfig, WaveConfigImporter

**Files:**
- Create: `Assets/_Game/Scripts/Data/Config/WaveConfig.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/JsonModels/WavesJson.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/WaveConfigImporter.cs`
- Create: `Assets/_Game/Tests/Editor/Fixtures/waves.test.json`
- Test: `Assets/_Game/Tests/Editor/WaveConfigImporterTests.cs`

**Interfaces:**
- Consumes: `AssetImportUtil.FindOrCreateSingleton`, `ImportReport`, `JsonFile.Read<T>` (Task 3).
- Produces: `Game.Data.WaveConfig` (fields: `maxEnemiesPerWave, intermissionSeconds, bossEvery`). `WaveConfigImporter.Import(string jsonPath, string configPath, ImportReport report) : WaveConfig`.

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/Editor/Fixtures/waves.test.json`:

```json
{
  "constants": { "maxEnemiesPerWave": 7, "intermissionSeconds": 2.5, "bossEvery": 5 }
}
```

Create `Assets/_Game/Tests/Editor/WaveConfigImporterTests.cs`:

```csharp
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class WaveConfigImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/waves.test.json";
        const string ConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/WaveConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_MapsWaveConstants()
        {
            var config = WaveConfigImporter.Import(Fixture, ConfigPath, new ImportReport());

            Assert.AreEqual(7, config.maxEnemiesPerWave);
            Assert.AreEqual(2.5f, config.intermissionSeconds, 0.0001f);
            Assert.AreEqual(5, config.bossEvery);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.WaveConfigImporterTests`. Expected: compile error.

- [ ] **Step 3: Create WaveConfig**

Create `Assets/_Game/Scripts/Data/Config/WaveConfig.cs`:

```csharp
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Config/Wave Config", fileName = "WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        public int maxEnemiesPerWave;
        public float intermissionSeconds;
        public int bossEvery;
    }
}
```

- [ ] **Step 4: Create the WavesJson POCO model**

Create `Assets/_Game/Scripts/Editor/Import/JsonModels/WavesJson.cs`:

```csharp
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class WavesJsonRoot
    {
        [JsonProperty("constants")] public WaveConstantsJson Constants;
    }

    public class WaveConstantsJson
    {
        [JsonProperty("maxEnemiesPerWave")] public int MaxEnemiesPerWave;
        [JsonProperty("intermissionSeconds")] public float IntermissionSeconds;
        [JsonProperty("bossEvery")] public int BossEvery;
    }
}
```

- [ ] **Step 5: Create WaveConfigImporter**

Create `Assets/_Game/Scripts/Editor/Import/WaveConfigImporter.cs`:

```csharp
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public static class WaveConfigImporter
    {
        public static WaveConfig Import(string jsonPath, string configPath, ImportReport report)
        {
            var root = JsonFile.Read<WavesJsonRoot>(jsonPath);
            var config = AssetImportUtil.FindOrCreateSingleton<WaveConfig>(configPath);

            config.maxEnemiesPerWave = root.Constants.MaxEnemiesPerWave;
            config.intermissionSeconds = root.Constants.IntermissionSeconds;
            config.bossEvery = root.Constants.BossEvery;
            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();
            report.Updated.Add("WaveConfig");

            return config;
        }
    }
}
```

- [ ] **Step 6: Run to verify pass**

`mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.WaveConfigImporterTests`, poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, 1 test, 0 failures.

- [ ] **Step 7: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/Config/WaveConfig.cs Assets/_Game/Scripts/Editor/Import/JsonModels/WavesJson.cs Assets/_Game/Scripts/Editor/Import/WaveConfigImporter.cs Assets/_Game/Tests/Editor/Fixtures/waves.test.json Assets/_Game/Tests/Editor/WaveConfigImporterTests.cs
git commit -m "feat: import waves.json into WaveConfig"
```

---

## Task 10: GameDatabase + SpecImporter orchestrator (menu command)

**Files:**
- Create: `Assets/_Game/Scripts/Data/GameDatabase.cs`
- Create: `Assets/_Game/Scripts/Editor/Import/SpecImporter.cs`
- Test: `Assets/_Game/Tests/Editor/SpecImporterTests.cs`

**Interfaces:**
- Consumes: every importer's `Import(...)` entry point and result type from Tasks 3–9; `Paths` (Task 3).
- Produces: `Game.Data.GameDatabase` (fields: `weapons, enemies, characters, powerUps, accessories, boosts, consumables, dances` arrays + `movementConfig, enemyCombatConfig, waveConfig, economyConfig, upgradeConfig, powerUpConfig, placeholders` singleton refs). `SpecImporter.RunImport(string specRoot, string dataRoot, ImportReport report) : GameDatabase` (the testable core) and the menu-bound `SpecImporter.ImportFromMenu()` (calls `RunImport` with the real `unity-spec/` and `Assets/_Game/Data/` paths, shows a confirmation dialog before running and a result dialog after).

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/Editor/SpecImporterTests.cs` — runs the whole pipeline against all the fixtures created in Tasks 3–9, into one shared generated output tree, and checks `GameDatabase` wiring end to end:

```csharp
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class SpecImporterTests
    {
        const string SpecRoot = "Assets/_Game/Tests/Editor/Fixtures";
        const string DataRoot = "Assets/_Game/Tests/Editor/Fixtures/GeneratedDatabase";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(DataRoot);
        }

        [Test]
        public void RunImport_BuildsGameDatabaseFromAllDomains()
        {
            var report = new ImportReport();

            var db = SpecImporter.RunImport(SpecRoot, DataRoot, report);

            Assert.AreEqual(4, db.weapons.Length);
            Assert.AreEqual(3, db.enemies.Length);
            Assert.AreEqual(2, db.characters.Length);
            Assert.AreEqual(3, db.powerUps.Length);
            Assert.AreEqual(3, db.accessories.Length);
            Assert.AreEqual(2, db.boosts.Length);
            Assert.AreEqual(2, db.consumables.Length);
            Assert.AreEqual(2, db.dances.Length);

            Assert.IsNotNull(db.movementConfig);
            Assert.IsNotNull(db.enemyCombatConfig);
            Assert.IsNotNull(db.waveConfig);
            Assert.IsNotNull(db.economyConfig);
            Assert.IsNotNull(db.upgradeConfig);
            Assert.IsNotNull(db.powerUpConfig);
            Assert.AreEqual(4, db.upgradeConfig.weaponUpgrades.Length);
            Assert.AreEqual(3, db.upgradeConfig.playerUpgrades.Length);

            var tank = System.Array.Find(db.enemies, e => e.id == "tank");
            Assert.IsNotNull(tank.weapon);
        }

        [Test]
        public void RunImport_SecondRunIsIdempotent()
        {
            SpecImporter.RunImport(SpecRoot, DataRoot, new ImportReport());

            var secondReport = new ImportReport();
            SpecImporter.RunImport(SpecRoot, DataRoot, secondReport);

            Assert.AreEqual(0, secondReport.Created.Count);
        }
    }
}
```

This test expects fixture file names matching the convention already established: `weapons.test.json`, `enemies.test.json`, `player.test.json`, `economy.test.json`, `items.test.json`, `powerups.test.json`, `waves.test.json`, all directly under `Assets/_Game/Tests/Editor/Fixtures/` (already created in Tasks 3–9).

- [ ] **Step 2: Run to verify failure**

`mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.SpecImporterTests`. Expected: compile error — `SpecImporter` doesn't exist.

- [ ] **Step 3: Create GameDatabase**

Create `Assets/_Game/Scripts/Data/GameDatabase.cs`:

```csharp
using System;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public WeaponData[] weapons = Array.Empty<WeaponData>();
        public EnemyData[] enemies = Array.Empty<EnemyData>();
        public CharacterData[] characters = Array.Empty<CharacterData>();
        public PowerUpData[] powerUps = Array.Empty<PowerUpData>();
        public AccessoryData[] accessories = Array.Empty<AccessoryData>();
        public BoostData[] boosts = Array.Empty<BoostData>();
        public ConsumableData[] consumables = Array.Empty<ConsumableData>();
        public DanceData[] dances = Array.Empty<DanceData>();

        public MovementConfig movementConfig;
        public EnemyCombatConfig enemyCombatConfig;
        public WaveConfig waveConfig;
        public EconomyConfig economyConfig;
        public UpgradeConfig upgradeConfig;
        public PowerUpConfig powerUpConfig;
        public PlaceholderLibrary placeholders;
    }
}
```

- [ ] **Step 4: Create SpecImporter**

`RunImport` must work identically whether it's fed the real `unity-spec/` file names (`weapons.json`, ...) or the test fixture names (`weapons.test.json`, ...), so the JSON file names are parameterized through a small `SpecFileNames` class rather than hardcoded — that keeps the test and production code paths byte-for-byte identical.

Create `Assets/_Game/Scripts/Editor/Import/SpecImporter.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace Game.Data.Editor.Import
{
    public class SpecFileNames
    {
        public string Weapons = "weapons.json";
        public string Enemies = "enemies.json";
        public string Player = "player.json";
        public string Economy = "economy.json";
        public string Items = "items.json";
        public string PowerUps = "powerups.json";
        public string Waves = "waves.json";
    }

    public static class SpecImporter
    {
        [MenuItem("Tools/Game/Import Spec from JSON")]
        public static void ImportFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Import Spec from JSON",
                    "This will create/update ScriptableObjects under Assets/_Game/Data from unity-spec/*.json. " +
                    "Art asset references (models, icons) are never touched. Continue?",
                    "Import", "Cancel"))
            {
                return;
            }

            var specRoot = System.IO.Path.Combine(Application.dataPath, "..", "unity-spec");
            var report = new ImportReport();
            RunImport(specRoot, Paths.DataRoot, new SpecFileNames(), report);

            EditorUtility.DisplayDialog("Import Spec from JSON — done", report.Summary(), "OK");
        }

        public static GameDatabase RunImport(string specRoot, string dataRoot, SpecFileNames fileNames, ImportReport report)
        {
            var weaponsFolder = $"{dataRoot}/Weapons";
            var enemiesFolder = $"{dataRoot}/Enemies";
            var charactersFolder = $"{dataRoot}/Characters";
            var powerUpsFolder = $"{dataRoot}/PowerUps";
            var accessoriesFolder = $"{dataRoot}/Items/Accessories";
            var boostsFolder = $"{dataRoot}/Items/Boosts";
            var consumablesFolder = $"{dataRoot}/Items/Consumables";
            var dancesFolder = $"{dataRoot}/Items/Dances";
            var configRoot = $"{dataRoot}/Config";
            var upgradeConfigPath = $"{configRoot}/UpgradeConfig.asset";

            var weaponResult = WeaponImporter.Import($"{specRoot}/{fileNames.Weapons}", weaponsFolder, upgradeConfigPath, report);
            var enemyResult = EnemyImporter.Import($"{specRoot}/{fileNames.Enemies}", enemiesFolder, weaponsFolder, $"{configRoot}/EnemyCombatConfig.asset", report);
            var characterResult = CharacterImporter.Import($"{specRoot}/{fileNames.Player}", charactersFolder, $"{configRoot}/MovementConfig.asset", upgradeConfigPath, report);
            var economyConfig = EconomyConfigImporter.Import($"{specRoot}/{fileNames.Economy}", $"{configRoot}/EconomyConfig.asset", report);
            var itemsResult = ItemsImporter.Import($"{specRoot}/{fileNames.Items}", accessoriesFolder, boostsFolder, consumablesFolder, dancesFolder, report);
            var powerUpResult = PowerUpImporter.Import($"{specRoot}/{fileNames.PowerUps}", powerUpsFolder, $"{configRoot}/PowerUpConfig.asset", report);
            var waveConfig = WaveConfigImporter.Import($"{specRoot}/{fileNames.Waves}", $"{configRoot}/WaveConfig.asset", report);

            var databasePath = $"{dataRoot}/GameDatabase.asset";
            var database = AssetImportUtil.FindOrCreateSingleton<GameDatabase>(databasePath);
            database.weapons = weaponResult.Weapons.ToArray();
            database.enemies = enemyResult.Enemies.ToArray();
            database.characters = characterResult.Characters.ToArray();
            database.powerUps = powerUpResult.PowerUps.ToArray();
            database.accessories = itemsResult.Accessories.ToArray();
            database.boosts = itemsResult.Boosts.ToArray();
            database.consumables = itemsResult.Consumables.ToArray();
            database.dances = itemsResult.Dances.ToArray();
            database.movementConfig = characterResult.MovementConfig;
            database.enemyCombatConfig = enemyResult.CombatConfig;
            database.waveConfig = waveConfig;
            database.economyConfig = economyConfig;
            database.upgradeConfig = characterResult.UpgradeConfig;
            database.powerUpConfig = powerUpResult.Config;
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();

            return database;
        }
    }
}
```

Update `SpecImporterTests.cs`'s two calls from `SpecImporter.RunImport(SpecRoot, DataRoot, report)` to `SpecImporter.RunImport(SpecRoot, DataRoot, new SpecFileNames { Weapons = "weapons.test.json", Enemies = "enemies.test.json", Player = "player.test.json", Economy = "economy.test.json", Items = "items.test.json", PowerUps = "powerups.test.json", Waves = "waves.test.json" }, report)` (both occurrences, in `RunImport_BuildsGameDatabaseFromAllDomains` and `RunImport_SecondRunIsIdempotent`).

- [ ] **Step 5: Run to verify pass**

`mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__run_tests` filtered to `Game.Data.Tests.SpecImporterTests`, poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, 2 tests, 0 failures.

- [ ] **Step 6: Run the full test suite**

`mcp__UnityMCP__run_tests` with no filter (or filtered to `Game.Data.Tests`), poll `mcp__UnityMCP__get_test_job`.
Expected: PASS, all tests across every task in this plan, 0 failures.

- [ ] **Step 7: Commit**

```bash
git add -A -- Assets/_Game/Scripts/Data/GameDatabase.cs Assets/_Game/Scripts/Editor/Import/SpecImporter.cs Assets/_Game/Tests/Editor/SpecImporterTests.cs
git commit -m "feat: add GameDatabase and SpecImporter orchestrator menu command"
```

- [ ] **Step 8: Manual smoke test against the real unity-spec/ files**

In the Unity Editor (via MCP or manually): run `Tools/Game/Import Spec from JSON`, confirm the dialog, and after completion check `Assets/_Game/Data/GameDatabase.asset` in the Inspector — it should reference 15 `WeaponData`, 15 `EnemyData` (`ghost` included with a logged warning), 3 `CharacterData`, 24 `PowerUpData`, 7 `AccessoryData`, 6 `BoostData`, 7 `ConsumableData`, 6 `DanceData`, and all 6 config singletons plus a `PlaceholderLibrary` (leave `PlaceholderLibrary`'s fields empty for now — that's a manual art-assignment task tracked separately, not part of this plan). Re-run the command a second time and confirm the result dialog reports 0 created / 0 updated (only "Unchanged").

---

## Post-plan follow-ups (explicitly out of scope here)

- Assigning real prefabs/sprites into `PlaceholderLibrary` and per-entry `modelPrefab`/`icon`/`image` fields — manual Inspector work, not code.
- Runtime consumers of `GameDatabase` (weapon firing, enemy AI, shop UI) — Phase 2 of `docs/migration-unity.md`.
- Extracting the still-missing data (achievements, daily contracts, killstreaks, detailed consumable stats, war dog, prestige) into `unity-spec/*.json` — tracked in `unity-spec/NEXT-STEPS.md`, not part of this import pipeline.
