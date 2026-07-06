# Vertical Slice Solo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Une arène greybox où l'on tue des vagues d'ennemis (clavier/souris en éditeur, joysticks virtuels au tactile), toutes les valeurs lues depuis les ScriptableObjects de `Assets/_Game/Data/`.

**Architecture:** Logique pure (classes C# testées EditMode) séparée de MonoBehaviours fins, comme à l'étape 3. Nouvel assembly `Game.Runtime` (+ 2 assemblies de tests). Scène construite par un menu éditeur idempotent `Tools/Game/Build Slice Scene`. HUD et contrôles tactiles générés par code au runtime (uGUI). Spec : `docs/superpowers/specs/2026-07-06-vertical-slice-solo-design.md`.

**Tech Stack:** Unity 6, URP, Input System 1.19 (actions construites en code), AI Navigation 2.0 (NavMeshSurface/NavMeshAgent), uGUI (Text legacy, pas de TMP), NUnit via Unity Test Framework.

## Global Constraints

- Identifiants/code en **anglais**, texte affiché en **français**.
- Aucune valeur d'équilibrage en dur : tout vient de `GameDatabase.asset` (exceptions sanctionnées : les 4 « trous de spec » §2 de la spec, champs sérialisés marqués `// TODO(spec-web)`).
- La logique n'exige jamais un asset : fallback primitive si `PlaceholderLibrary` absente/vide, aucun NRE si `database.placeholders` est null.
- **Vérification par tâche (pas de CLI)** : compiler via MCP `mcp__UnityMCP__refresh_unity(compile="request", wait_for_ready=true)` puis `mcp__UnityMCP__read_console(types=["error"])` → attendu : 0 erreur. Tests via `mcp__UnityMCP__run_tests(mode="EditMode")` puis `mcp__UnityMCP__get_test_job(job_id, wait_timeout=60)` → attendu : tous verts (31 tests existants inclus — ne jamais les casser).
- Branche de travail : `feat/vertical-slice-solo` (créée depuis `main` avant la tâche 1). Un commit par tâche minimum. Pas de trailer `Co-Authored-By`.
- Les vérifications manuelles (Play Mode visuel, feel, tactile) ne bloquent pas l'exécution : les consigner dans la liste finale pour Nathan (tâche 14).
- Ne jamais modifier les assets générés de `Assets/_Game/Data/` ni les scripts d'import de l'étape 3.

## Vue d'ensemble des tâches

1. Assembly `Game.Runtime` + `GameContext` + `PrimitiveFactory` (+ asmdef de tests EditMode)
2. `HitZoneResolver` (zones de dégâts)
3. `StaminaModel`, `FallDamageRule`, `DashModel`
4. `WaveComposition` (table de composition des vagues)
5. `AmmoModel` + `AutoFireDecider`
6. `EnemyBrain` (FSM pure)
7. Joueur : `PlayerMotor`, `PlayerLook`, `PlayerInputBridge` (input construit en code)
8. `PlayerHealth` (régén, second wind, dégâts de chute)
9. `WeaponController` (tir auto raycast, pellets, reload)
10. `EnemyHealth` + `EnemySpawner`
11. `EnemyController` (NavMesh + FSM ranged/melee) + `WaveManager`
12. Scène : `SliceSceneBuilder` (menu éditeur) + `SliceBootstrap` (câblage runtime)
13. HUD runtime (barre de vie, munitions, vague, radar, écran de mort) + contrôles tactiles
14. Tests PlayMode smoke + docs + liste de vérifs manuelles pour Nathan

---

### Task 1: Assembly Game.Runtime + GameContext + PrimitiveFactory

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Game.Runtime.asmdef`
- Create: `Assets/_Game/Scripts/Runtime/Core/GameContext.cs`
- Create: `Assets/_Game/Scripts/Runtime/Core/PrimitiveFactory.cs`
- Create: `Assets/_Game/Tests/Runtime/Game.Runtime.Tests.asmdef`
- Test: `Assets/_Game/Tests/Runtime/GameContextTests.cs`

**Interfaces:**
- Consumes: `Game.Data` (`GameDatabase`, `EconomyConfig.difficulties : DifficultySettings[]`, `EnemyData`, `CharacterData`, `PlaceholderLibrary`).
- Produces: `GameContext : MonoBehaviour` avec `public GameDatabase database`, `static GameContext Instance`, `DifficultySettings Difficulty`, `static DifficultySettings ResolveDifficulty(GameDatabase db)`, `GameObject ResolveEnemyModel(EnemyData data)`, `GameObject ResolveCharacterModel(CharacterData data)` ; `PrimitiveFactory.CreateCapsule(string name, Color color) : GameObject` (capsule visuelle sans collider). Toutes les tâches suivantes vivent dans le namespace `Game.Runtime` sous cet asmdef.

- [ ] **Step 1: Créer les deux asmdef**

`Assets/_Game/Scripts/Runtime/Game.Runtime.asmdef` :

```json
{
    "name": "Game.Runtime",
    "rootNamespace": "Game.Runtime",
    "references": [
        "Game.Data",
        "Unity.InputSystem",
        "Unity.AI.Navigation"
    ],
    "autoReferenced": true,
    "noEngineReferences": false
}
```

`Assets/_Game/Tests/Runtime/Game.Runtime.Tests.asmdef` (EditMode uniquement) :

```json
{
    "name": "Game.Runtime.Tests",
    "rootNamespace": "Game.Runtime.Tests",
    "references": [
        "Game.Runtime",
        "Game.Data"
    ],
    "includePlatforms": ["Editor"],
    "overrideReferences": false,
    "precompiledReferences": [],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "optionalUnityReferences": ["TestAssemblies"]
}
```

- [ ] **Step 2: Écrire le test qui échoue (GameContextTests)**

`Assets/_Game/Tests/Runtime/GameContextTests.cs` :

```csharp
using Game.Data;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class GameContextTests
    {
        [Test]
        public void ResolveDifficulty_FindsNormalEntry()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var eco = ScriptableObject.CreateInstance<EconomyConfig>();
            eco.difficulties = new[]
            {
                new DifficultySettings { id = "facile", enemyHpMult = 0.7f, enemyDmgMult = 0.7f, moneyMult = 1.2f },
                new DifficultySettings { id = "normal", enemyHpMult = 1f, enemyDmgMult = 1f, moneyMult = 1f },
            };
            db.economyConfig = eco;

            var diff = GameContext.ResolveDifficulty(db);

            Assert.AreEqual("normal", diff.id);
            Assert.AreEqual(1f, diff.enemyHpMult);
        }

        [Test]
        public void ResolveDifficulty_FallsBackToNeutralWhenMissing()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();

            var diff = GameContext.ResolveDifficulty(db);

            Assert.AreEqual(1f, diff.enemyHpMult);
            Assert.AreEqual(1f, diff.enemyDmgMult);
            Assert.AreEqual(1f, diff.moneyMult);
        }

        [Test]
        public void ResolveEnemyModel_NullPlaceholdersDoesNotThrow()
        {
            var go = new GameObject("ctx");
            try
            {
                var ctx = go.AddComponent<GameContext>();
                ctx.database = ScriptableObject.CreateInstance<GameDatabase>();
                var enemy = ScriptableObject.CreateInstance<EnemyData>();

                Assert.IsNull(ctx.ResolveEnemyModel(enemy));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
```

- [ ] **Step 3: Vérifier l'échec** — compiler via MCP (`refresh_unity` + `read_console`) : erreurs attendues (`GameContext` inexistant). C'est l'échec attendu du cycle TDD (en C# compilé, « le test échoue » = il ne compile pas encore).

- [ ] **Step 4: Implémenter GameContext et PrimitiveFactory**

`Assets/_Game/Scripts/Runtime/Core/GameContext.cs` :

```csharp
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    public class GameContext : MonoBehaviour
    {
        public GameDatabase database;

        public static GameContext Instance { get; private set; }

        void Awake()
        {
            if (database == null)
            {
                Debug.LogError("GameContext: no GameDatabase assigned - disabling.");
                enabled = false;
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public DifficultySettings Difficulty => ResolveDifficulty(database);

        public static DifficultySettings ResolveDifficulty(GameDatabase db)
        {
            var list = db != null && db.economyConfig != null ? db.economyConfig.difficulties : null;
            if (list != null)
                foreach (var d in list)
                    if (d != null && d.id == "normal")
                        return d;
            return new DifficultySettings
            {
                id = "normal", displayName = "Normal",
                enemyHpMult = 1f, enemyDmgMult = 1f, moneyMult = 1f
            };
        }

        public GameObject ResolveEnemyModel(EnemyData data)
        {
            if (data != null && data.modelPrefab != null) return data.modelPrefab;
            var lib = database != null ? database.placeholders : null;
            return lib != null ? lib.GetEnemyPlaceholder() : null;
        }

        public GameObject ResolveCharacterModel(CharacterData data)
        {
            if (data != null && data.modelPrefab != null) return data.modelPrefab;
            var lib = database != null ? database.placeholders : null;
            return lib != null ? lib.GetCharacterPlaceholder() : null;
        }
    }
}
```

`Assets/_Game/Scripts/Runtime/Core/PrimitiveFactory.cs` :

```csharp
using UnityEngine;

namespace Game.Runtime
{
    /// Visuels de secours quand ni le modèle ni la PlaceholderLibrary ne fournissent d'asset.
    public static class PrimitiveFactory
    {
        public static GameObject CreateCapsule(string name, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>()); // visuel pur : le collider appartient au parent
            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = color;
            return go;
        }
    }
}
```

- [ ] **Step 5: Compiler + tests via MCP** — `refresh_unity` puis `read_console(types=["error"])` (0 erreur), puis `run_tests(mode="EditMode")` + `get_test_job` : 31 anciens + 3 nouveaux = 34 verts.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/Runtime Assets/_Game/Tests/Runtime
git commit -m "feat: Game.Runtime assembly with GameContext and primitive fallbacks"
```

---

### Task 2: HitZoneResolver

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Combat/HitZoneResolver.cs`
- Test: `Assets/_Game/Tests/Runtime/HitZoneResolverTests.cs`

**Interfaces:**
- Consumes: `Game.Data.HitZoneSettings` (headAboveY=1.5, headDamage=60, torsoDamage=25, legsBelowY=0.6, legsDamage=15), `Game.Data.ArmorType`.
- Produces: `enum HitZone { Head, Torso, Legs }` ; `static HitZone HitZoneResolver.Resolve(float yLocal, float scale, HitZoneSettings zones)` ; `static int HitZoneResolver.Damage(HitZone zone, HitZoneSettings zones, float weaponDmgMult, ArmorType armor, float armorDamageMult)`.

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/_Game/Tests/Runtime/HitZoneResolverTests.cs` :

```csharp
using Game.Data;
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class HitZoneResolverTests
    {
        static HitZoneSettings Zones() => new HitZoneSettings
        {
            headAboveY = 1.5f, headDamage = 60,
            torsoDamage = 25,
            legsBelowY = 0.6f, legsDamage = 15
        };

        [TestCase(1.6f, ExpectedResult = HitZone.Head)]
        [TestCase(1.5f, ExpectedResult = HitZone.Head)]   // frontière incluse tête
        [TestCase(1.49f, ExpectedResult = HitZone.Torso)]
        [TestCase(0.6f, ExpectedResult = HitZone.Torso)]  // frontière incluse torse
        [TestCase(0.59f, ExpectedResult = HitZone.Legs)]
        [TestCase(0.1f, ExpectedResult = HitZone.Legs)]
        public HitZone Resolve_ByHeight(float yLocal) => HitZoneResolver.Resolve(yLocal, 1f, Zones());

        [Test]
        public void Resolve_ScaleNormalizesHeight()
        {
            // boss scale 1.9 : un impact à 2.85 m = tête (2.85 / 1.9 = 1.5)
            Assert.AreEqual(HitZone.Head, HitZoneResolver.Resolve(2.85f, 1.9f, Zones()));
            Assert.AreEqual(HitZone.Torso, HitZoneResolver.Resolve(1.5f, 1.9f, Zones()));
        }

        [Test]
        public void Damage_UsesZoneTimesWeaponMult()
        {
            Assert.AreEqual(60, HitZoneResolver.Damage(HitZone.Head, Zones(), 1f, ArmorType.None, 0.5f));
            Assert.AreEqual(25, HitZoneResolver.Damage(HitZone.Torso, Zones(), 1f, ArmorType.None, 0.5f));
            Assert.AreEqual(15, HitZoneResolver.Damage(HitZone.Legs, Zones(), 1f, ArmorType.None, 0.5f));
            Assert.AreEqual(180, HitZoneResolver.Damage(HitZone.Head, Zones(), 3f, ArmorType.None, 0.5f)); // sniper dmgMult 3
        }

        [Test]
        public void Damage_TorsoArmorOnlyProtectsTorso()
        {
            Assert.AreEqual(13, HitZoneResolver.Damage(HitZone.Torso, Zones(), 1f, ArmorType.Torso, 0.5f)); // 12.5 -> 13 (away from zero)
            Assert.AreEqual(60, HitZoneResolver.Damage(HitZone.Head, Zones(), 1f, ArmorType.Torso, 0.5f));  // headshot passe intégralement
            Assert.AreEqual(15, HitZoneResolver.Damage(HitZone.Legs, Zones(), 1f, ArmorType.Torso, 0.5f));
        }

        [Test]
        public void Damage_FullArmorProtectsEverything()
        {
            Assert.AreEqual(30, HitZoneResolver.Damage(HitZone.Head, Zones(), 1f, ArmorType.Full, 0.5f));
            Assert.AreEqual(13, HitZoneResolver.Damage(HitZone.Torso, Zones(), 1f, ArmorType.Full, 0.5f));
            Assert.AreEqual(8, HitZoneResolver.Damage(HitZone.Legs, Zones(), 1f, ArmorType.Full, 0.5f)); // 7.5 -> 8
        }
    }
}
```

- [ ] **Step 2: Vérifier l'échec** — compile MCP : erreurs `HitZone`/`HitZoneResolver` inexistants.

- [ ] **Step 3: Implémenter**

`Assets/_Game/Scripts/Runtime/Combat/HitZoneResolver.cs` :

```csharp
using System;
using Game.Data;

namespace Game.Runtime
{
    public enum HitZone { Head, Torso, Legs }

    /// Spec web (enemies.json -> hitZones) : zone par hauteur d'impact relative aux pieds,
    /// normalisée par le scale du modèle (référence 1.8 m).
    public static class HitZoneResolver
    {
        public static HitZone Resolve(float yLocal, float scale, HitZoneSettings zones)
        {
            float y = scale > 0f ? yLocal / scale : yLocal;
            if (y >= zones.headAboveY) return HitZone.Head;
            if (y < zones.legsBelowY) return HitZone.Legs;
            return HitZone.Torso;
        }

        public static int Damage(HitZone zone, HitZoneSettings zones, float weaponDmgMult, ArmorType armor, float armorDamageMult)
        {
            int zoneDamage = zone == HitZone.Head ? zones.headDamage
                : zone == HitZone.Legs ? zones.legsDamage
                : zones.torsoDamage;
            bool isProtected = armor == ArmorType.Full || (armor == ArmorType.Torso && zone == HitZone.Torso);
            double damage = zoneDamage * weaponDmgMult * (isProtected ? armorDamageMult : 1f);
            return (int)Math.Round(damage, MidpointRounding.AwayFromZero);
        }
    }
}
```

- [ ] **Step 4: Compiler + tests MCP** — 0 erreur, tous les tests verts (34 + 11 nouveaux cas).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Combat/HitZoneResolver.cs Assets/_Game/Tests/Runtime/HitZoneResolverTests.cs
git commit -m "feat: hit zone resolution with armor rules from web spec"
```

---

### Task 3: StaminaModel, FallDamageRule, DashModel

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Player/StaminaModel.cs`
- Create: `Assets/_Game/Scripts/Runtime/Player/FallDamageRule.cs`
- Create: `Assets/_Game/Scripts/Runtime/Player/DashModel.cs`
- Test: `Assets/_Game/Tests/Runtime/MovementModelsTests.cs`

**Interfaces:**
- Consumes: `Game.Data.StaminaSettings` (max=100, drainPerSec=25, regenPerSec=15, minToRun=20), `FallDamageSettings` (safeHeight=3, damagePerMeter=5, maxDamage=40), `DashSettings` (distance=2.6, speed=16, cooldown=1.4).
- Produces: `StaminaModel(StaminaSettings)` avec `float Current`, `bool Tick(float dt, bool wantsRun)` (retourne « court effectivement ») ; `static int FallDamageRule.Damage(float fallHeight, FallDamageSettings s)` ; `DashModel(DashSettings)` avec `float Duration`, `float Speed`, `bool IsActive(float now)`, `bool TryStart(float now)`.

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/_Game/Tests/Runtime/MovementModelsTests.cs` :

```csharp
using Game.Data;
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class MovementModelsTests
    {
        static StaminaSettings Stamina() => new StaminaSettings { max = 100f, drainPerSec = 25f, regenPerSec = 15f, minToRun = 20f };
        static FallDamageSettings Fall() => new FallDamageSettings { safeHeight = 3f, damagePerMeter = 5f, maxDamage = 40f };
        static DashSettings Dash() => new DashSettings { distance = 2.6f, speed = 16f, cooldown = 1.4f };

        [Test]
        public void Stamina_DrainsWhileRunningAndRegensOtherwise()
        {
            var s = new StaminaModel(Stamina());
            Assert.IsTrue(s.Tick(1f, true));           // 100 -> 75
            Assert.AreEqual(75f, s.Current, 0.001f);
            s.Tick(1f, false);                          // 75 -> 90
            Assert.AreEqual(90f, s.Current, 0.001f);
        }

        [Test]
        public void Stamina_ExhaustionBlocksRunUntilMinToRun()
        {
            var s = new StaminaModel(Stamina());
            s.Tick(4f, true);                           // 100 -> 0 : épuisé
            Assert.AreEqual(0f, s.Current, 0.001f);
            Assert.IsFalse(s.Tick(1f, true));           // épuisé : régénère (0 -> 15) au lieu de courir
            Assert.AreEqual(15f, s.Current, 0.001f);
            Assert.IsFalse(s.Tick(0.2f, true));         // 15 -> 18 : toujours < minToRun (20)
            s.Tick(0.14f, true);                        // -> ~20.1 : seuil franchi
            Assert.IsTrue(s.Tick(0.1f, true));          // repart
        }

        [Test]
        public void Stamina_ClampsAtMax()
        {
            var s = new StaminaModel(Stamina());
            s.Tick(10f, false);
            Assert.AreEqual(100f, s.Current, 0.001f);
        }

        [TestCase(3f, ExpectedResult = 0)]
        [TestCase(2f, ExpectedResult = 0)]
        [TestCase(4f, ExpectedResult = 5)]    // (4-3)*5
        [TestCase(8f, ExpectedResult = 25)]   // (8-3)*5
        [TestCase(20f, ExpectedResult = 40)]  // plafonné
        public int FallDamage_MatchesWebFormula(float height) => FallDamageRule.Damage(height, Fall());

        [Test]
        public void Dash_DurationAndCooldown()
        {
            var d = new DashModel(Dash());
            Assert.AreEqual(2.6f / 16f, d.Duration, 0.0001f);
            Assert.IsTrue(d.TryStart(10f));
            Assert.IsTrue(d.IsActive(10.1f));
            Assert.IsFalse(d.IsActive(10.3f));           // durée ~0.1625 s écoulée
            Assert.IsFalse(d.TryStart(10.5f));           // cooldown 1.4 s pas fini
            Assert.IsTrue(d.TryStart(11.4f));
        }
    }
}
```

- [ ] **Step 2: Vérifier l'échec** — compile MCP : types inexistants.

- [ ] **Step 3: Implémenter les trois modèles**

`Assets/_Game/Scripts/Runtime/Player/StaminaModel.cs` :

```csharp
using System;
using Game.Data;

namespace Game.Runtime
{
    /// Spec web (player.json -> movement.stamina). Une fois à 0, il faut remonter
    /// au-dessus de minToRun pour pouvoir recourir.
    public class StaminaModel
    {
        readonly StaminaSettings settings;
        bool exhausted;

        public float Current { get; private set; }

        public StaminaModel(StaminaSettings settings)
        {
            this.settings = settings;
            Current = settings.max;
        }

        public bool Tick(float dt, bool wantsRun)
        {
            if (exhausted && Current >= settings.minToRun) exhausted = false;
            bool running = wantsRun && !exhausted && Current > 0f;
            if (running)
            {
                Current = Math.Max(0f, Current - settings.drainPerSec * dt);
                if (Current <= 0f) { exhausted = true; running = false; }
            }
            else
            {
                Current = Math.Min(settings.max, Current + settings.regenPerSec * dt);
            }
            return running;
        }
    }
}
```

`Assets/_Game/Scripts/Runtime/Player/FallDamageRule.cs` :

```csharp
using System;
using Game.Data;

namespace Game.Runtime
{
    /// Spec web : h <= safeHeight ? 0 : min(maxDamage, round((h - safeHeight) * damagePerMeter)).
    public static class FallDamageRule
    {
        public static int Damage(float fallHeight, FallDamageSettings s)
        {
            if (fallHeight <= s.safeHeight) return 0;
            double raw = Math.Round((fallHeight - s.safeHeight) * s.damagePerMeter, MidpointRounding.AwayFromZero);
            return (int)Math.Min(s.maxDamage, raw);
        }
    }
}
```

`Assets/_Game/Scripts/Runtime/Player/DashModel.cs` :

```csharp
using Game.Data;

namespace Game.Runtime
{
    /// Dash web : ruée rectiligne de `distance` mètres à `speed` m/s, cooldown depuis le début du dash.
    public class DashModel
    {
        readonly DashSettings settings;
        float lastStart = float.NegativeInfinity;

        public DashModel(DashSettings settings) { this.settings = settings; }

        public float Duration => settings.speed > 0f ? settings.distance / settings.speed : 0f;
        public float Speed => settings.speed;

        public bool IsActive(float now) => now < lastStart + Duration;

        public bool TryStart(float now)
        {
            if (now < lastStart + settings.cooldown) return false;
            lastStart = now;
            return true;
        }
    }
}
```

- [ ] **Step 4: Compiler + tests MCP** — 0 erreur, tout vert.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Player Assets/_Game/Tests/Runtime/MovementModelsTests.cs
git commit -m "feat: stamina, fall damage and dash models from MovementConfig spec"
```

---

### Task 4: WaveComposition

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Waves/WaveComposition.cs`
- Test: `Assets/_Game/Tests/Runtime/WaveCompositionTests.cs`

**Interfaces:**
- Consumes: `Game.Data.Rules.WaveRules` (`IsBossWave`, `EnemiesForWave`).
- Produces: `static string WaveComposition.TypeForSlot(int wave, int slot)` et `static string[] WaveComposition.ForWave(int wave)` — ids = `EnemyData.id` (« shooter », « rusher », …). Vague boss = `["boss","shooter","shooter"]`, slot 0 des autres vagues toujours « shooter ». (Le `WaveSpawnPlan` de la spec §3 est fusionné dans `ForWave` : une classe séparée n'aurait fait que combiner `EnemiesForWave` + `TypeForSlot`.)

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/_Game/Tests/Runtime/WaveCompositionTests.cs` :

```csharp
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class WaveCompositionTests
    {
        [Test]
        public void Wave1_IsThreeShooters()
        {
            CollectionAssert.AreEqual(new[] { "shooter", "shooter", "shooter" }, WaveComposition.ForWave(1));
        }

        [Test]
        public void Wave2_IntroducesRusherOnOddSlots()
        {
            CollectionAssert.AreEqual(new[] { "shooter", "rusher", "shooter", "rusher" }, WaveComposition.ForWave(2));
        }

        [Test]
        public void Wave3_IntroducesTankOnSlot3()
        {
            CollectionAssert.AreEqual(new[] { "shooter", "rusher", "shooter", "tank", "shooter" }, WaveComposition.ForWave(3));
        }

        [Test]
        public void Wave4_IntroducesBomberAndShielder()
        {
            // n = min(2+4, 7) = 6 ; i2 -> bomber (w>=4, i%3==2), i4 -> shielder (4<=w<6, i%5==4), i5 -> bomber
            CollectionAssert.AreEqual(new[] { "shooter", "rusher", "bomber", "tank", "shielder", "bomber" }, WaveComposition.ForWave(4));
        }

        [Test]
        public void BossWaves_AreBossPlusTwoShooters()
        {
            CollectionAssert.AreEqual(new[] { "boss", "shooter", "shooter" }, WaveComposition.ForWave(5));
            CollectionAssert.AreEqual(new[] { "boss", "shooter", "shooter" }, WaveComposition.ForWave(10));
        }

        [Test]
        public void Wave6_EvenWaveVariants()
        {
            // n = 7 ; i1 -> medic (w>=6, i%6==1), i2 -> toxic (w pair), i3 -> tank, i4 -> berserker (w>=6, i%5==4), i5 -> toxic (i%3==2), i6 -> sniper (w>=5, i%7==6)
            CollectionAssert.AreEqual(new[] { "shooter", "medic", "toxic", "tank", "berserker", "toxic", "sniper" }, WaveComposition.ForWave(6));
        }

        [Test]
        public void Wave7_OddWaveVariants()
        {
            // n = 7 ; i1 -> medic, i2 -> bomber (w impair), i3 -> tank, i4 -> berserker, i5 -> stunner (w>=7, i%6==5, impair), i6 -> sniper (w>=5, i%7==6)
            CollectionAssert.AreEqual(new[] { "shooter", "medic", "bomber", "tank", "berserker", "stunner", "sniper" }, WaveComposition.ForWave(7));
        }

        [Test]
        public void Wave12_UnlocksJuggernaut()
        {
            // n = 7 ; w=12 pair : i1 medic, i2 toxic, i3 juggernaut (w>=10), i4 commander (w>=9 pair), i5 cryo (w>=7, i%6==5, pair), i6 grenadier (w>=8 pair)
            CollectionAssert.AreEqual(new[] { "shooter", "medic", "toxic", "juggernaut", "commander", "cryo", "grenadier" }, WaveComposition.ForWave(12));
        }

        [Test]
        public void Slot0_IsAlwaysShooterOnNonBossWaves()
        {
            for (int w = 1; w <= 30; w++)
            {
                if (w % 5 == 0) continue;
                Assert.AreEqual("shooter", WaveComposition.ForWave(w)[0], $"wave {w}");
            }
        }
    }
}
```

- [ ] **Step 2: Vérifier l'échec** — compile MCP : `WaveComposition` inexistant.

- [ ] **Step 3: Implémenter**

`Assets/_Game/Scripts/Runtime/Waves/WaveComposition.cs` :

```csharp
using Game.Data.Rules;

namespace Game.Runtime
{
    /// Transposition exacte de waves.json -> composition.rules (premier match gagne).
    /// Slot 0 des vagues normales toujours forcé "shooter" ; vague boss = [boss, shooter, shooter].
    public static class WaveComposition
    {
        public static string TypeForSlot(int w, int i)
        {
            if (w >= 10 && i % 4 == 3) return "juggernaut";
            if (w >= 3 && i % 4 == 3) return "tank";
            if (w >= 7 && i % 6 == 5) return w % 2 == 0 ? "cryo" : "stunner";
            if (w >= 8 && i % 7 == 6) return w % 2 == 0 ? "grenadier" : "sniper";
            if (w >= 5 && i % 7 == 6) return "sniper";
            if (w >= 9 && i % 5 == 4) return w % 2 == 0 ? "commander" : "berserker";
            if (w >= 6 && i % 5 == 4) return "berserker";
            if (w >= 4 && w < 6 && i % 5 == 4) return "shielder";
            if (w >= 6 && i % 3 == 2) return w % 2 == 0 ? "toxic" : "bomber";
            if (w >= 4 && i % 3 == 2) return "bomber";
            if (w >= 6 && i % 6 == 1) return "medic";
            if (w >= 2 && i % 2 == 1) return "rusher";
            return "shooter";
        }

        public static string[] ForWave(int wave)
        {
            if (WaveRules.IsBossWave(wave)) return new[] { "boss", "shooter", "shooter" };
            int count = WaveRules.EnemiesForWave(wave);
            var result = new string[count];
            for (int i = 0; i < count; i++) result[i] = TypeForSlot(wave, i);
            result[0] = "shooter";
            return result;
        }
    }
}
```

- [ ] **Step 4: Compiler + tests MCP.** Attention : si un cas attendu des tests des vagues 6/7/12 diverge, revérifier la table contre `unity-spec/waves.json` (l'ordre des règles est significatif) — la table est la source de vérité, pas le test.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Waves/WaveComposition.cs Assets/_Game/Tests/Runtime/WaveCompositionTests.cs
git commit -m "feat: wave composition table transposed from waves.json"
```

---

### Task 5: AmmoModel + AutoFireDecider

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Combat/AmmoModel.cs`
- Create: `Assets/_Game/Scripts/Runtime/Combat/AutoFireDecider.cs`
- Test: `Assets/_Game/Tests/Runtime/AmmoAndAutoFireTests.cs`

**Interfaces:**
- Consumes: rien (types primitifs).
- Produces: `AmmoModel(int magSize, float reloadTime)` avec `int InMag`, `int MagSize`, `bool IsReloading`, `bool TryConsume()`, `void StartReload()`, `void Tick(float dt)` (réserve infinie — TODO(spec-web) documenté dans la spec §2) ; `static bool AutoFireDecider.ShouldFire(bool enemyUnderCrosshair, float distance, float weaponRange, float cooldownRemaining, bool hasAmmoReady)`.

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/_Game/Tests/Runtime/AmmoAndAutoFireTests.cs` :

```csharp
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class AmmoAndAutoFireTests
    {
        [Test]
        public void Ammo_ConsumeUntilEmptyThenReload()
        {
            var a = new AmmoModel(2, 1.2f);
            Assert.IsTrue(a.TryConsume());
            Assert.IsTrue(a.TryConsume());
            Assert.IsFalse(a.TryConsume());     // chargeur vide
            a.StartReload();
            Assert.IsTrue(a.IsReloading);
            Assert.IsFalse(a.TryConsume());     // pas de tir pendant le reload
            a.Tick(1.2f);
            Assert.IsFalse(a.IsReloading);
            Assert.AreEqual(2, a.InMag);        // chargeur plein (réserve infinie)
        }

        [Test]
        public void Ammo_ReloadIgnoredWhenFullOrAlreadyReloading()
        {
            var a = new AmmoModel(3, 1f);
            a.StartReload();
            Assert.IsFalse(a.IsReloading);      // déjà plein : no-op
            a.TryConsume();
            a.StartReload();
            a.StartReload();                     // double appel inoffensif
            a.Tick(0.5f);
            Assert.IsTrue(a.IsReloading);
            a.Tick(0.5f);
            Assert.AreEqual(3, a.InMag);
        }

        [TestCase(true, 5f, 60f, 0f, true, ExpectedResult = true)]
        [TestCase(false, 5f, 60f, 0f, true, ExpectedResult = false)]  // rien sous le viseur
        [TestCase(true, 70f, 60f, 0f, true, ExpectedResult = false)]  // hors portée
        [TestCase(true, 5f, 60f, 0.05f, true, ExpectedResult = false)] // cooldown en cours
        [TestCase(true, 5f, 60f, 0f, false, ExpectedResult = false)]  // reload / chargeur vide
        public bool AutoFire_Decision(bool onTarget, float dist, float range, float cd, bool ammoReady) =>
            AutoFireDecider.ShouldFire(onTarget, dist, range, cd, ammoReady);
    }
}
```

- [ ] **Step 2: Vérifier l'échec** — compile MCP : types inexistants.

- [ ] **Step 3: Implémenter**

`Assets/_Game/Scripts/Runtime/Combat/AmmoModel.cs` :

```csharp
using System;

namespace Game.Runtime
{
    /// Chargeur + rechargement. Réserve infinie. // TODO(spec-web): vérifier si le web a une réserve finie.
    public class AmmoModel
    {
        readonly float reloadTime;
        float reloadRemaining;

        public int MagSize { get; }
        public int InMag { get; private set; }
        public bool IsReloading { get; private set; }

        public AmmoModel(int magSize, float reloadTime)
        {
            MagSize = Math.Max(1, magSize);
            this.reloadTime = Math.Max(0f, reloadTime);
            InMag = MagSize;
        }

        public bool TryConsume()
        {
            if (IsReloading || InMag <= 0) return false;
            InMag--;
            return true;
        }

        public void StartReload()
        {
            if (IsReloading || InMag == MagSize) return;
            IsReloading = true;
            reloadRemaining = reloadTime;
        }

        public void Tick(float dt)
        {
            if (!IsReloading) return;
            reloadRemaining -= dt;
            if (reloadRemaining <= 0f)
            {
                IsReloading = false;
                InMag = MagSize;
            }
        }
    }
}
```

`Assets/_Game/Scripts/Runtime/Combat/AutoFireDecider.cs` :

```csharp
namespace Game.Runtime
{
    /// Tir automatique façon Guns of Boom (décision D1 de la spec).
    public static class AutoFireDecider
    {
        public static bool ShouldFire(bool enemyUnderCrosshair, float distance, float weaponRange, float cooldownRemaining, bool hasAmmoReady) =>
            enemyUnderCrosshair && distance <= weaponRange && cooldownRemaining <= 0f && hasAmmoReady;
    }
}
```

- [ ] **Step 4: Compiler + tests MCP** — tout vert.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Combat Assets/_Game/Tests/Runtime/AmmoAndAutoFireTests.cs
git commit -m "feat: ammo magazine model and auto-fire decision"
```

---

### Task 6: EnemyBrain (FSM pure)

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Enemies/EnemyBrain.cs`
- Test: `Assets/_Game/Tests/Runtime/EnemyBrainTests.cs`

**Interfaces:**
- Consumes: rien (types primitifs — les valeurs viennent d'`EnemyBaseStats` au moment de la construction par `EnemyController`, tâche 11).
- Produces: `enum EnemyState { Idle, Chase, Attack }` ; `EnemyBrain(float detectRadius, float loseRadius, float loseTime, float attackRange)` avec `EnemyState State` et `EnemyState Tick(float dt, float distToPlayer)`.

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/_Game/Tests/Runtime/EnemyBrainTests.cs` :

```csharp
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class EnemyBrainTests
    {
        // Valeurs spec : detect 12, lose 18, loseTime 3, shootRange 9.
        static EnemyBrain Brain() => new EnemyBrain(12f, 18f, 3f, 9f);

        [Test]
        public void Idle_DetectsPlayerInsideDetectRadius()
        {
            var b = Brain();
            Assert.AreEqual(EnemyState.Idle, b.Tick(0.1f, 13f));
            Assert.AreEqual(EnemyState.Chase, b.Tick(0.1f, 11f));
        }

        [Test]
        public void Chase_AttacksInsideAttackRange()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);                              // Idle -> Chase
            Assert.AreEqual(EnemyState.Attack, b.Tick(0.1f, 8f));
        }

        [Test]
        public void Attack_BacksToChaseWhenOutOfRange()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);
            b.Tick(0.1f, 8f);                               // Attack
            Assert.AreEqual(EnemyState.Chase, b.Tick(0.1f, 10f));
        }

        [Test]
        public void Chase_GivesUpAfterLoseTimeBeyondLoseRadius()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);                              // Chase
            Assert.AreEqual(EnemyState.Chase, b.Tick(2.9f, 20f)); // 2.9 s < loseTime
            Assert.AreEqual(EnemyState.Idle, b.Tick(0.2f, 20f));  // 3.1 s cumulés
        }

        [Test]
        public void Chase_LoseTimerResetsWhenPlayerComesBack()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);
            b.Tick(2.9f, 20f);                              // presque perdu
            b.Tick(0.1f, 15f);                              // revient sous loseRadius : reset
            Assert.AreEqual(EnemyState.Chase, b.Tick(2.9f, 20f)); // le timer repart de zéro
        }

        [Test]
        public void SniperRange_UsesOverride()
        {
            var sniper = new EnemyBrain(12f, 18f, 3f, 28f); // shootRangeOverride 28
            sniper.Tick(0.1f, 11f);                          // Chase (détection inchangée)
            Assert.AreEqual(EnemyState.Attack, sniper.Tick(0.1f, 11f)); // déjà à portée de tir
        }
    }
}
```

- [ ] **Step 2: Vérifier l'échec** — compile MCP : types inexistants.

- [ ] **Step 3: Implémenter**

`Assets/_Game/Scripts/Runtime/Enemies/EnemyBrain.cs` :

```csharp
namespace Game.Runtime
{
    public enum EnemyState { Idle, Chase, Attack }

    /// FSM du web (idle/chase/attack) : détection par rayon, abandon après loseTime
    /// au-delà de loseRadius. Pure : l'EnemyController fournit dt et distance.
    public class EnemyBrain
    {
        readonly float detectRadius, loseRadius, loseTime, attackRange;
        float lostTimer;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        public EnemyBrain(float detectRadius, float loseRadius, float loseTime, float attackRange)
        {
            this.detectRadius = detectRadius;
            this.loseRadius = loseRadius;
            this.loseTime = loseTime;
            this.attackRange = attackRange;
        }

        public EnemyState Tick(float dt, float distToPlayer)
        {
            switch (State)
            {
                case EnemyState.Idle:
                    if (distToPlayer <= detectRadius) { State = EnemyState.Chase; lostTimer = 0f; }
                    break;
                case EnemyState.Chase:
                    if (distToPlayer <= attackRange) { State = EnemyState.Attack; lostTimer = 0f; }
                    else if (distToPlayer > loseRadius)
                    {
                        lostTimer += dt;
                        if (lostTimer >= loseTime) { State = EnemyState.Idle; lostTimer = 0f; }
                    }
                    else lostTimer = 0f;
                    break;
                case EnemyState.Attack:
                    if (distToPlayer > attackRange) State = EnemyState.Chase;
                    break;
            }
            return State;
        }
    }
}
```

- [ ] **Step 4: Compiler + tests MCP** — tout vert.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Enemies/EnemyBrain.cs Assets/_Game/Tests/Runtime/EnemyBrainTests.cs
git commit -m "feat: enemy FSM (idle/chase/attack) with lose timer"
```

---

### Task 7: PlayerMotor + PlayerLook + PlayerInputBridge

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Player/PlayerMotor.cs`
- Create: `Assets/_Game/Scripts/Runtime/Player/PlayerLook.cs`
- Create: `Assets/_Game/Scripts/Runtime/Player/PlayerInputBridge.cs`

**Interfaces:**
- Consumes: `MovementConfig` (tous champs), `StaminaModel`, `DashModel` (tâche 3), `StatFormulas.SpeedMult` (existant).
- Produces: `PlayerMotor` avec `void Init(MovementConfig config, float speedMult)`, `void SetMoveInput(Vector2 input, bool wantsRun)`, `void RequestJump()`, `void RequestDash()`, `event Action<float> FellFrom` (hauteur de chute en mètres, émise à l'atterrissage), `float StaminaCurrent`, `float StaminaMax`, `bool IsRunning` ; `PlayerLook` avec `Transform cameraPivot`, `void AddLook(Vector2 delta)` ; `PlayerInputBridge` avec refs publiques `motor`, `look`, `weapon` (le champ `weapon` est du type `WeaponController` de la tâche 9 — **dans cette tâche, déclarer le champ mais laisser l'appel à `RequestReload()` derrière un null-check**, la tâche 9 le branchera).

Pas de logique testable en EditMode ici (tout est lié au moteur physique/temps réel) : la couverture vient des modèles purs (tâche 3), des smoke tests PlayMode (tâche 14) et de la vérif manuelle de Nathan.

**Note d'ordre :** pour éviter la référence avant : cette tâche crée aussi un stub minimal `Assets/_Game/Scripts/Runtime/Combat/WeaponController.cs` :

```csharp
using UnityEngine;

namespace Game.Runtime
{
    public class WeaponController : MonoBehaviour
    {
        public void RequestReload() { }
    }
}
```

(La tâche 9 remplace intégralement ce fichier.)

- [ ] **Step 1: Implémenter PlayerMotor**

`Assets/_Game/Scripts/Runtime/Player/PlayerMotor.cs` :

```csharp
using System;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        CharacterController controller;
        MovementConfig config;
        StaminaModel stamina;
        DashModel dash;
        float speedMult = 1f;

        Vector2 moveInput;
        bool wantsRun;
        bool jumpRequested;
        float verticalVelocity;
        Vector3 dashDirection;
        bool isAirborne;
        float highestY;

        public event Action<float> FellFrom;

        public bool IsRunning { get; private set; }
        public float StaminaCurrent => stamina != null ? stamina.Current : 0f;
        public float StaminaMax => config != null ? config.stamina.max : 0f;

        public void Init(MovementConfig config, float speedMult)
        {
            this.config = config;
            this.speedMult = speedMult;
            stamina = new StaminaModel(config.stamina);
            dash = new DashModel(config.dash);
            controller = GetComponent<CharacterController>();
            controller.stepOffset = config.stepUpMax;
        }

        public void SetMoveInput(Vector2 input, bool run)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
            wantsRun = run;
        }

        public void RequestJump() { jumpRequested = true; }

        public void RequestDash()
        {
            if (config == null || !dash.TryStart(Time.time)) return;
            Vector3 dir = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));
            dashDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
        }

        void Update()
        {
            if (config == null) return;
            float dt = Time.deltaTime;

            bool moving = moveInput.sqrMagnitude > 0.01f;
            IsRunning = stamina.Tick(dt, wantsRun && moving);
            float speed = (IsRunning ? config.runSpeed : config.walkSpeed) * speedMult;
            Vector3 horizontal = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)) * speed;
            if (dash.IsActive(Time.time)) horizontal = dashDirection * dash.Speed;

            if (controller.isGrounded)
            {
                if (isAirborne)
                {
                    float fallHeight = highestY - transform.position.y;
                    if (fallHeight > 0f) FellFrom?.Invoke(fallHeight);
                    isAirborne = false;
                }
                verticalVelocity = -2f; // colle au sol (stepDownSnap approximé par CharacterController)
                if (jumpRequested) verticalVelocity = config.jumpSpeed;
            }
            else
            {
                if (!isAirborne) { isAirborne = true; highestY = transform.position.y; }
                highestY = Mathf.Max(highestY, transform.position.y);
                verticalVelocity -= config.gravity * dt;
            }
            jumpRequested = false;

            Vector3 velocity = horizontal;
            velocity.y = verticalVelocity;
            controller.Move(velocity * dt);
        }
    }
}
```

- [ ] **Step 2: Implémenter PlayerLook**

`Assets/_Game/Scripts/Runtime/Player/PlayerLook.cs` :

```csharp
using UnityEngine;

namespace Game.Runtime
{
    public class PlayerLook : MonoBehaviour
    {
        public Transform cameraPivot;
        public float sensitivity = 0.12f;

        float pitch;

        public void AddLook(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * sensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -85f, 85f);
            if (cameraPivot != null)
                cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }
}
```

- [ ] **Step 3: Implémenter PlayerInputBridge (actions construites en code) + stub WeaponController**

`Assets/_Game/Scripts/Runtime/Player/PlayerInputBridge.cs` :

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime
{
    /// Actions Input System construites en code (décision D3) : clavier/souris en éditeur,
    /// et chemins Gamepad alimentés par les contrôles on-screen (tâche 13) sur mobile.
    public class PlayerInputBridge : MonoBehaviour
    {
        public PlayerMotor motor;
        public PlayerLook look;
        public WeaponController weapon;

        InputActionMap map;
        InputAction move, lookAction, jump, dash, reload, sprint;

        void Awake()
        {
            map = new InputActionMap("Gameplay");

            move = map.AddAction("Move");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            lookAction = map.AddAction("Look", binding: "<Mouse>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

            jump = map.AddAction("Jump", binding: "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            dash = map.AddAction("Dash", binding: "<Keyboard>/leftCtrl");
            dash.AddBinding("<Gamepad>/buttonEast");

            reload = map.AddAction("Reload", binding: "<Keyboard>/r");
            reload.AddBinding("<Gamepad>/buttonWest");

            sprint = map.AddAction("Sprint", binding: "<Keyboard>/leftShift");
        }

        void OnEnable() { map.Enable(); }
        void OnDisable() { map.Disable(); }

        void Update()
        {
            if (motor == null) return;
            Vector2 m = move.ReadValue<Vector2>();
            bool wantsRun = sprint.IsPressed() || m.sqrMagnitude >= 0.81f; // stick à fond = courir (D11)
            motor.SetMoveInput(m, wantsRun);
            if (look != null) look.AddLook(lookAction.ReadValue<Vector2>());
            if (jump.WasPressedThisFrame()) motor.RequestJump();
            if (dash.WasPressedThisFrame()) motor.RequestDash();
            if (reload.WasPressedThisFrame() && weapon != null) weapon.RequestReload();
        }
    }
}
```

`Assets/_Game/Scripts/Runtime/Combat/WeaponController.cs` (stub, remplacé en tâche 9) :

```csharp
using UnityEngine;

namespace Game.Runtime
{
    public class WeaponController : MonoBehaviour
    {
        public void RequestReload() { }
    }
}
```

- [ ] **Step 4: Compiler + tests MCP** — 0 erreur, les tests existants restent verts.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Player Assets/_Game/Scripts/Runtime/Combat/WeaponController.cs
git commit -m "feat: player motor, look and input bridge (keyboard/mouse + gamepad paths)"
```

*Ajouter à la liste des vérifs manuelles : marche/course/saut/dash au clavier, sensibilité souris, drain de stamina visible (tâche 13 l'affichera).*

---

### Task 8: PlayerHealth

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Player/PlayerHealth.cs`
- Test: `Assets/_Game/Tests/Runtime/PlayerHealthTests.cs`

**Interfaces:**
- Consumes: `CharacterData` (baseHealth, baseRegen), `MovementConfig` (secondWindInvulnSeconds, secondWindUsesPerRun, fallDamage), `StatFormulas`, `FallDamageRule` (tâche 3).
- Produces: `PlayerHealth` avec `void Init(CharacterData character, MovementConfig movement)`, `void TakeDamage(float amount)`, `void ApplyFallDamage(float fallHeight)`, `float Health`, `float MaxHealth`, `event Action<float,float> HealthChanged` (current, max), `event Action Died`. Horloge injectable **publique** `Func<float> timeSource` (défaut `() => Time.time`) — seam de test pour l'invulnérabilité en EditMode (les tests sont dans un autre assembly, `internal` ne suffirait pas).

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/_Game/Tests/Runtime/PlayerHealthTests.cs` :

```csharp
using Game.Data;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class PlayerHealthTests
    {
        GameObject go;
        PlayerHealth health;
        float fakeTime;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("player");
            health = go.AddComponent<PlayerHealth>();
            fakeTime = 100f;
            health.timeSource = () => fakeTime;
            var character = ScriptableObject.CreateInstance<CharacterData>();
            character.baseHealth = 100f;
            character.baseRegen = 10f;
            var movement = ScriptableObject.CreateInstance<MovementConfig>();
            movement.secondWindInvulnSeconds = 2f;
            movement.secondWindUsesPerRun = 1;
            movement.fallDamage = new FallDamageSettings { safeHeight = 3f, damagePerMeter = 5f, maxDamage = 40f };
            health.Init(character, movement);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(go); }

        [Test]
        public void Init_UsesStatFormulas()
        {
            Assert.AreEqual(100f, health.MaxHealth);
            Assert.AreEqual(100f, health.Health);
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            health.TakeDamage(30f);
            Assert.AreEqual(70f, health.Health);
        }

        [Test]
        public void SecondWind_SavesFromFatalBlowOnce()
        {
            bool died = false;
            health.Died += () => died = true;

            health.TakeDamage(150f);                 // coup fatal n°1 : second wind
            Assert.AreEqual(1f, health.Health);
            Assert.IsFalse(died);

            health.TakeDamage(50f);                  // pendant l'invulnérabilité : ignoré
            Assert.AreEqual(1f, health.Health);

            fakeTime += 2.1f;                        // invulnérabilité expirée
            health.TakeDamage(50f);                  // coup fatal n°2 : mort
            Assert.AreEqual(0f, health.Health);
            Assert.IsTrue(died);
        }

        [Test]
        public void ApplyFallDamage_UsesWebFormula()
        {
            health.ApplyFallDamage(8f);              // (8-3)*5 = 25
            Assert.AreEqual(75f, health.Health);
            health.ApplyFallDamage(2f);              // sous safeHeight : rien
            Assert.AreEqual(75f, health.Health);
        }
    }
}
```

- [ ] **Step 2: Vérifier l'échec** — compile MCP : `PlayerHealth` inexistant.

- [ ] **Step 3: Implémenter**

`Assets/_Game/Scripts/Runtime/Player/PlayerHealth.cs` :

```csharp
using System;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] float regenDelaySeconds = 3f; // TODO(spec-web): délai de régén non extrait du web (0 = continue)

        public Func<float> timeSource = () => Time.time; // seam de test (assembly de tests séparé)

        float regenRate;
        float lastDamageTime = float.NegativeInfinity;
        float invulnerableUntil = float.NegativeInfinity;
        int secondWindRemaining;
        float secondWindInvulnSeconds;
        FallDamageSettings fallDamage;

        public float Health { get; private set; }
        public float MaxHealth { get; private set; }

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public void Init(CharacterData character, MovementConfig movement)
        {
            MaxHealth = StatFormulas.MaxHealth(character.baseHealth, 0, 0f);
            regenRate = StatFormulas.RegenRate(character.baseRegen, 0, 0f);
            Health = MaxHealth;
            secondWindRemaining = movement.secondWindUsesPerRun;
            secondWindInvulnSeconds = movement.secondWindInvulnSeconds;
            fallDamage = movement.fallDamage;
            HealthChanged?.Invoke(Health, MaxHealth);
        }

        public void TakeDamage(float amount)
        {
            float now = timeSource();
            if (amount <= 0f || Health <= 0f || now < invulnerableUntil) return;
            lastDamageTime = now;
            Health -= amount;
            if (Health <= 0f)
            {
                if (secondWindRemaining > 0)
                {
                    secondWindRemaining--;
                    Health = 1f;
                    invulnerableUntil = now + secondWindInvulnSeconds;
                }
                else
                {
                    Health = 0f;
                    HealthChanged?.Invoke(Health, MaxHealth);
                    Died?.Invoke();
                    return;
                }
            }
            HealthChanged?.Invoke(Health, MaxHealth);
        }

        public void ApplyFallDamage(float fallHeight)
        {
            if (fallDamage == null) return;
            int damage = FallDamageRule.Damage(fallHeight, fallDamage);
            if (damage > 0) TakeDamage(damage);
        }

        void Update()
        {
            if (Health <= 0f || Health >= MaxHealth) return;
            if (timeSource() - lastDamageTime < regenDelaySeconds) return;
            Health = Mathf.Min(MaxHealth, Health + regenRate * Time.deltaTime);
            HealthChanged?.Invoke(Health, MaxHealth);
        }
    }
}
```

- [ ] **Step 4: Compiler + tests MCP** — tout vert.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Player/PlayerHealth.cs Assets/_Game/Tests/Runtime/PlayerHealthTests.cs
git commit -m "feat: player health with regen delay, second wind and fall damage"
```

---

### Task 9: WeaponController (tir auto raycast)

**Files:**
- Modify: `Assets/_Game/Scripts/Runtime/Combat/WeaponController.cs` (remplace intégralement le stub de la tâche 7)

**Interfaces:**
- Consumes: `WeaponData` (mag, cooldown, reload, dmgMult, recoil, range, pellets), `AmmoModel`, `AutoFireDecider` (tâche 5), `EnemyHealth.TakeHit(Vector3 worldPoint, float weaponDmgMult)` (tâche 10 — cette tâche crée un stub `EnemyHealth`, voir note).
- Produces: `WeaponController` avec `Camera aimCamera`, `LayerMask hitMask`, `void Init(WeaponData weapon)`, `void RequestReload()`, `WeaponData Weapon`, `event Action<int,int> AmmoChanged` (inMag, magSize), `event Action<bool> ReloadingChanged`.

**Note d'ordre :** créer aussi le stub `Assets/_Game/Scripts/Runtime/Enemies/EnemyHealth.cs` (remplacé intégralement en tâche 10) :

```csharp
using UnityEngine;

namespace Game.Runtime
{
    public class EnemyHealth : MonoBehaviour
    {
        public void TakeHit(Vector3 worldPoint, float weaponDmgMult) { }
    }
}
```

La logique décisionnelle (quand tirer, munitions) est déjà testée en tâche 5 ; ici c'est la coquille moteur (raycasts, événements). Couverture : smoke test PlayMode (tâche 14) + vérif manuelle.

- [ ] **Step 1: Implémenter**

`Assets/_Game/Scripts/Runtime/Combat/WeaponController.cs` :

```csharp
using System;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// Tir automatique (D1) : tire quand le réticule est sur un ennemi à portée.
    /// Raycast hitscan depuis le centre caméra, pellets indépendants pour les shotguns.
    public class WeaponController : MonoBehaviour
    {
        public Camera aimCamera;
        public LayerMask hitMask = ~0;

        AmmoModel ammo;
        float cooldownRemaining;
        bool wasReloading;

        public WeaponData Weapon { get; private set; }

        public event Action<int, int> AmmoChanged;
        public event Action<bool> ReloadingChanged;

        public void Init(WeaponData weapon)
        {
            Weapon = weapon;
            ammo = new AmmoModel(weapon.mag, weapon.reload);
            AmmoChanged?.Invoke(ammo.InMag, ammo.MagSize);
        }

        public void RequestReload()
        {
            if (Weapon != null) ammo.StartReload();
        }

        void Update()
        {
            if (Weapon == null || aimCamera == null) return;
            float dt = Time.deltaTime;

            ammo.Tick(dt);
            if (wasReloading != ammo.IsReloading)
            {
                wasReloading = ammo.IsReloading;
                ReloadingChanged?.Invoke(ammo.IsReloading);
                AmmoChanged?.Invoke(ammo.InMag, ammo.MagSize);
            }
            cooldownRemaining -= dt;

            Ray center = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool onTarget = false;
            float distance = 0f;
            if (Physics.Raycast(center, out RaycastHit hit, Weapon.range, hitMask))
            {
                onTarget = hit.collider.GetComponentInParent<EnemyHealth>() != null;
                distance = hit.distance;
            }

            bool ammoReady = !ammo.IsReloading && ammo.InMag > 0;
            if (AutoFireDecider.ShouldFire(onTarget, distance, Weapon.range, cooldownRemaining, ammoReady))
                Fire();

            if (ammo.InMag == 0 && !ammo.IsReloading)
            {
                ammo.StartReload();
                // l'événement ReloadingChanged partira au prochain Update via wasReloading
            }
        }

        void Fire()
        {
            if (!ammo.TryConsume()) return;
            cooldownRemaining = Weapon.cooldown;
            int pellets = Mathf.Max(1, Weapon.pellets);
            for (int i = 0; i < pellets; i++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * Weapon.recoil * 0.01f;
                Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f + spread.x, 0.5f + spread.y, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, Weapon.range, hitMask))
                    hit.collider.GetComponentInParent<EnemyHealth>()?.TakeHit(hit.point, Weapon.dmgMult);
            }
            AmmoChanged?.Invoke(ammo.InMag, ammo.MagSize);
        }
    }
}
```

- [ ] **Step 2: Compiler + tests MCP** — 0 erreur, tests existants verts.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Combat/WeaponController.cs Assets/_Game/Scripts/Runtime/Enemies/EnemyHealth.cs
git commit -m "feat: auto-fire raycast weapon controller with pellets and reload"
```

*Vérif manuelle à consigner : le réticule sur un ennemi déclenche le tir, cadence/reload perceptibles, spread shotgun.*

---

### Task 10: EnemyHealth + EnemySpawner

**Files:**
- Modify: `Assets/_Game/Scripts/Runtime/Enemies/EnemyHealth.cs` (remplace le stub de la tâche 9)
- Create: `Assets/_Game/Scripts/Runtime/Enemies/EnemySpawner.cs`
- Test: `Assets/_Game/Tests/Runtime/EnemyHealthTests.cs`

**Interfaces:**
- Consumes: `EnemyData`, `HitZoneSettings`, `HitZoneResolver` (tâche 2), `WaveRules.EnemyHpForWave`, `GameContext` + `PrimitiveFactory` (tâche 1), `EnemyController.Init(...)` (tâche 11 — stub créé ici, voir note).
- Produces: `EnemyHealth` avec `void Init(EnemyData data, int wave, float difficultyHpMult, HitZoneSettings zones)`, `void TakeHit(Vector3 worldPoint, float weaponDmgMult)`, `int Health`, `EnemyData Data`, `event Action<EnemyHealth> Died` ; `EnemySpawner` (refs publiques `GameContext context`, `Transform player`) avec `EnemyHealth Spawn(EnemyData data, Vector3 position, int wave)` (null si pas de NavMesh à ≤ 2 m — warning console).

**Note d'ordre :** créer aussi le stub `Assets/_Game/Scripts/Runtime/Enemies/EnemyController.cs` (remplacé en tâche 11) :

```csharp
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    public class EnemyController : MonoBehaviour
    {
        public void Init(EnemyData data, EnemyCombatConfig combat, Transform player, DifficultySettings difficulty) { }
    }
}
```

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/_Game/Tests/Runtime/EnemyHealthTests.cs` :

```csharp
using Game.Data;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class EnemyHealthTests
    {
        GameObject go;
        EnemyHealth health;
        EnemyData shooter;

        static HitZoneSettings Zones() => new HitZoneSettings
        {
            headAboveY = 1.5f, headDamage = 60,
            torsoDamage = 25,
            legsBelowY = 0.6f, legsDamage = 15
        };

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("enemy");
            health = go.AddComponent<EnemyHealth>();
            shooter = ScriptableObject.CreateInstance<EnemyData>();
            shooter.id = "shooter";
            shooter.hpMult = 1f;
            shooter.scale = 1f;
            shooter.armor = ArmorType.None;
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(go); }

        [Test]
        public void Init_Wave1NormalDifficulty_Is100Hp()
        {
            health.Init(shooter, 1, 1f, Zones());
            Assert.AreEqual(100, health.Health);
        }

        [Test]
        public void Init_AppliesWaveFormulaAndHpMult()
        {
            shooter.hpMult = 2.6f; // tank
            health.Init(shooter, 3, 1f, Zones());
            // EnemyHpForWave(3, 1) = round(100 * 1.18^2) = 139 ; * 2.6 = 361.4 -> 361
            Assert.AreEqual(361, health.Health);
        }

        [Test]
        public void TakeHit_HeadAtWorldHeightDealsHeadDamage()
        {
            health.Init(shooter, 1, 1f, Zones());
            go.transform.position = Vector3.zero;
            health.TakeHit(new Vector3(0f, 1.6f, 0f), 1f);   // tête
            Assert.AreEqual(40, health.Health);               // 100 - 60
        }

        [Test]
        public void TakeHit_KillEmitsDiedOnce()
        {
            health.Init(shooter, 1, 1f, Zones());
            int deaths = 0;
            health.Died += _ => deaths++;
            health.TakeHit(new Vector3(0f, 1.6f, 0f), 3f);    // 180 dégâts : mort
            health.TakeHit(new Vector3(0f, 1.6f, 0f), 3f);    // déjà mort : ignoré
            Assert.AreEqual(1, deaths);
            Assert.LessOrEqual(health.Health, 0);
        }
    }
}
```

- [ ] **Step 2: Vérifier l'échec** — compile MCP : les nouveaux membres n'existent pas encore sur le stub.

- [ ] **Step 3: Implémenter EnemyHealth**

`Assets/_Game/Scripts/Runtime/Enemies/EnemyHealth.cs` :

```csharp
using System;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] float armorDamageMult = 0.5f; // TODO(spec-web): réduction d'armure non extraite du web

        EnemyData data;
        HitZoneSettings zones;

        public int Health { get; private set; }
        public EnemyData Data => data;

        public event Action<EnemyHealth> Died;

        public void Init(EnemyData data, int wave, float difficultyHpMult, HitZoneSettings zones)
        {
            this.data = data;
            this.zones = zones;
            Health = Mathf.Max(1, Mathf.RoundToInt(WaveRules.EnemyHpForWave(wave, difficultyHpMult) * data.hpMult));
        }

        public void TakeHit(Vector3 worldPoint, float weaponDmgMult)
        {
            if (Health <= 0 || data == null) return;
            float yLocal = worldPoint.y - transform.position.y; // racine du GO = pieds
            HitZone zone = HitZoneResolver.Resolve(yLocal, data.scale, zones);
            Health -= HitZoneResolver.Damage(zone, zones, weaponDmgMult, data.armor, armorDamageMult);
            if (Health <= 0)
            {
                Died?.Invoke(this);
                if (Application.isPlaying) Destroy(gameObject); // Destroy interdit en EditMode (tests)
            }
        }
    }
}
```

- [ ] **Step 4: Implémenter EnemySpawner**

`Assets/_Game/Scripts/Runtime/Enemies/EnemySpawner.cs` :

```csharp
using Game.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Runtime
{
    public class EnemySpawner : MonoBehaviour
    {
        public GameContext context;
        public Transform player;

        const float ReferenceHeight = 1.8f; // modèle web de référence

        public EnemyHealth Spawn(EnemyData data, Vector3 position, int wave)
        {
            if (!NavMesh.SamplePosition(position, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"EnemySpawner: no NavMesh near {position} for '{data.id}' - skipping spawn.");
                return null;
            }

            var db = context.database;
            var stats = db.enemyCombatConfig.baseStats;
            var difficulty = GameContext.ResolveDifficulty(db);

            var root = new GameObject($"Enemy_{data.id}");
            root.transform.position = navHit.position;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.speed = stats.runSpeed * data.speedMult;
            agent.radius = stats.radius;
            agent.height = ReferenceHeight * data.scale;
            agent.angularSpeed = stats.turnSpeed * 60f;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = ReferenceHeight * data.scale;
            capsule.radius = stats.radius;
            capsule.center = new Vector3(0f, ReferenceHeight * data.scale * 0.5f, 0f);

            var health = root.AddComponent<EnemyHealth>();
            health.Init(data, wave, difficulty.enemyHpMult, db.enemyCombatConfig.hitZones);

            var controller = root.AddComponent<EnemyController>();
            controller.Init(data, db.enemyCombatConfig, player, difficulty);

            var model = context.ResolveEnemyModel(data);
            GameObject visual = model != null
                ? Object.Instantiate(model, root.transform)
                : PrimitiveFactory.CreateCapsule("Placeholder", RadarColor(data));
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, ReferenceHeight * data.scale * 0.5f, 0f);
            visual.transform.localScale = Vector3.one * (0.9f * data.scale); // capsule Unity = 2 m -> 1.8 m

            return health;
        }

        public static Color RadarColor(EnemyData data)
        {
            string hex = string.IsNullOrEmpty(data.radarColor) ? "#ffb84d" : data.radarColor;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.red;
        }
    }
}
```

- [ ] **Step 5: Compiler + tests MCP** — tout vert.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Enemies Assets/_Game/Tests/Runtime/EnemyHealthTests.cs
git commit -m "feat: enemy health with hit zones and data-driven enemy spawner"
```

---

### Task 11: EnemyController + WaveManager

**Files:**
- Modify: `Assets/_Game/Scripts/Runtime/Enemies/EnemyController.cs` (remplace le stub de la tâche 10)
- Create: `Assets/_Game/Scripts/Runtime/Waves/WaveManager.cs`

**Interfaces:**
- Consumes: `EnemyBrain`/`EnemyState` (tâche 6), `EnemyData` (melee, damageMult, burst, hasShootOverride, shootCdOverride, shootRangeOverride, speedMult), `EnemyBaseStats`, `PlayerHealth.TakeDamage` (tâche 8), `WaveComposition.ForWave` (tâche 4), `EnemySpawner.Spawn` (tâche 10), `WaveConfig` (intermissionSeconds), `EconomyConfig.perKillMoney`.
- Produces: `EnemyController.Init(EnemyData data, EnemyCombatConfig combat, Transform player, DifficultySettings difficulty)` (signature identique au stub) ; `WaveManager` (refs publiques `context`, `spawner`, `spawnPoints`) avec `int CurrentWave`, `int Money`, `int AliveCount`, `event Action<int> WaveStarted`, `event Action<int> AliveCountChanged`, `event Action<float> IntermissionStarted`, `void Begin()` (démarre la boucle — appelé par `SliceBootstrap`, pas de démarrage auto dans `Start`).

- [ ] **Step 1: Implémenter EnemyController**

`Assets/_Game/Scripts/Runtime/Enemies/EnemyController.cs` :

```csharp
using Game.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Runtime
{
    /// Archétypes ranged (rafales hitscan à probabilité `accuracy`, fidèle au web)
    /// et melee (coup au contact). Comportements spéciaux différés (décision D5).
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        NavMeshAgent agent;
        EnemyBrain brain;
        EnemyData data;
        EnemyBaseStats stats;
        Transform player;
        PlayerHealth playerHealth;

        float attackDamage;
        float attackCooldown;
        float attackRange;
        float attackTimer;
        int burstRemaining;
        float burstTimer;

        public EnemyState State => brain != null ? brain.State : EnemyState.Idle;

        public void Init(EnemyData data, EnemyCombatConfig combat, Transform player, DifficultySettings difficulty)
        {
            this.data = data;
            stats = combat.baseStats;
            this.player = player;
            playerHealth = player != null ? player.GetComponentInParent<PlayerHealth>() : null;
            agent = GetComponent<NavMeshAgent>();

            attackRange = data.melee ? stats.meleeRange
                : data.hasShootOverride && data.shootRangeOverride > 0f ? data.shootRangeOverride
                : stats.shootRange;
            attackCooldown = data.melee ? stats.meleeCooldown
                : data.hasShootOverride && data.shootCdOverride > 0f ? data.shootCdOverride
                : stats.fireCooldown;
            float baseDamage = data.melee ? stats.meleeDamage : stats.damage;
            attackDamage = baseDamage * data.damageMult * difficulty.enemyDmgMult;

            brain = new EnemyBrain(stats.detectRadius, stats.loseRadius, stats.loseTime, attackRange);
            agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.5f);
        }

        void Update()
        {
            if (player == null || brain == null) return;
            float dt = Time.deltaTime;
            float dist = Vector3.Distance(transform.position, player.position);
            var state = brain.Tick(dt, dist);
            attackTimer -= dt;

            switch (state)
            {
                case EnemyState.Idle:
                    if (agent.hasPath) agent.ResetPath();
                    break;
                case EnemyState.Chase:
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    break;
                case EnemyState.Attack:
                    agent.isStopped = true;
                    FaceTarget(dt);
                    TickAttack(dt);
                    break;
            }
        }

        void FaceTarget(float dt)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), stats.turnSpeed * dt);
        }

        void TickAttack(float dt)
        {
            if (playerHealth == null) return;
            if (data.melee)
            {
                if (attackTimer <= 0f)
                {
                    playerHealth.TakeDamage(attackDamage);
                    attackTimer = attackCooldown;
                }
                return;
            }
            if (burstRemaining > 0)
            {
                burstTimer -= dt;
                if (burstTimer <= 0f)
                {
                    if (Random.value <= stats.accuracy) playerHealth.TakeDamage(attackDamage);
                    burstRemaining--;
                    burstTimer = stats.burstGap;
                }
            }
            else if (attackTimer <= 0f)
            {
                burstRemaining = Mathf.Max(1, data.burst);
                burstTimer = 0f;
                attackTimer = attackCooldown;
            }
        }
    }
}
```

- [ ] **Step 2: Implémenter WaveManager**

`Assets/_Game/Scripts/Runtime/Waves/WaveManager.cs` :

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    public class WaveManager : MonoBehaviour
    {
        public GameContext context;
        public EnemySpawner spawner;
        public Transform[] spawnPoints = Array.Empty<Transform>();

        readonly List<EnemyHealth> alive = new List<EnemyHealth>();
        Dictionary<string, EnemyData> byId;

        public int CurrentWave { get; private set; }
        public int Money { get; private set; }
        public int AliveCount => alive.Count;
        public IReadOnlyList<EnemyHealth> Alive => alive;

        public event Action<int> WaveStarted;
        public event Action<int> AliveCountChanged;
        public event Action<float> IntermissionStarted;

        public void Begin()
        {
            byId = new Dictionary<string, EnemyData>();
            foreach (var e in context.database.enemies)
                if (e != null && !string.IsNullOrEmpty(e.id))
                    byId[e.id] = e;
            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            var config = context.database.waveConfig;
            while (true)
            {
                CurrentWave++;
                IntermissionStarted?.Invoke(config.intermissionSeconds);
                yield return new WaitForSeconds(config.intermissionSeconds);
                SpawnWave(CurrentWave);
                WaveStarted?.Invoke(CurrentWave);
                while (alive.Count > 0) yield return null;
            }
        }

        void SpawnWave(int wave)
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError("WaveManager: no spawn points assigned.");
                return;
            }
            string[] plan = WaveComposition.ForWave(wave);
            for (int i = 0; i < plan.Length; i++)
            {
                if (!byId.TryGetValue(plan[i], out var data))
                {
                    Debug.LogWarning($"WaveManager: unknown enemy type '{plan[i]}' - falling back to shooter.");
                    if (!byId.TryGetValue("shooter", out data)) continue;
                }
                var enemy = spawner.Spawn(data, spawnPoints[i % spawnPoints.Length].position, wave);
                if (enemy == null) continue;
                alive.Add(enemy);
                enemy.Died += OnEnemyDied;
            }
            AliveCountChanged?.Invoke(alive.Count);
        }

        void OnEnemyDied(EnemyHealth enemy)
        {
            alive.Remove(enemy);
            var eco = context.database.economyConfig;
            if (eco != null) Money += eco.perKillMoney;
            AliveCountChanged?.Invoke(alive.Count);
        }
    }
}
```

- [ ] **Step 3: Compiler + tests MCP** — 0 erreur, tests existants verts.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/Enemies/EnemyController.cs Assets/_Game/Scripts/Runtime/Waves/WaveManager.cs
git commit -m "feat: data-driven enemy controller (ranged/melee) and wave manager loop"
```

---

### Task 12: SliceSceneBuilder (menu éditeur) + SliceBootstrap

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/Core/SliceBootstrap.cs`
- Create: `Assets/_Game/Scripts/Editor/Scene/Game.Runtime.Editor.asmdef`
- Create: `Assets/_Game/Scripts/Editor/Scene/SliceSceneBuilder.cs`

**Interfaces:**
- Consumes: tout ce qui précède (`GameContext`, `PlayerMotor`, `PlayerLook`, `PlayerHealth`, `WeaponController`, `PlayerInputBridge`, `EnemySpawner`, `WaveManager`), `NavMeshSurface` (package AI Navigation), `StatFormulas.SpeedMult`.
- Produces: menu **`Tools/Game/Build Slice Scene`** (idempotent : reconstruit `Assets/_Game/Scenes/Slice.unity` de zéro à chaque exécution) ; `SliceBootstrap : MonoBehaviour` avec refs publiques `context`, `motor`, `look`, `health`, `weapon`, `waveManager` et un `Start()` qui fait tous les `Init(...)` et appelle `waveManager.Begin()`. La tâche 13 étendra `SliceBootstrap.Start` (HUD + tactile + écran de mort).

- [ ] **Step 1: Implémenter SliceBootstrap**

`Assets/_Game/Scripts/Runtime/Core/SliceBootstrap.cs` :

```csharp
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    /// Câblage runtime de la scène Slice : perso par défaut, arme de départ,
    /// dégâts de chute, puis lancement de la boucle de vagues.
    public class SliceBootstrap : MonoBehaviour
    {
        public GameContext context;
        public PlayerMotor motor;
        public PlayerLook look;
        public PlayerHealth health;
        public WeaponController weapon;
        public WaveManager waveManager;

        void Start()
        {
            if (context == null || context.database == null)
            {
                Debug.LogError("SliceBootstrap: missing GameContext or database.");
                return;
            }
            var db = context.database;

            CharacterData character = null;
            foreach (var c in db.characters)
                if (c != null && c.id == "perso:blue-soldier") { character = c; break; }
            if (character == null && db.characters.Length > 0) character = db.characters[0];

            WeaponData weaponData = null;
            foreach (var w in db.weapons)
                if (w != null && w.id == "default") { weaponData = w; break; }
            if (weaponData == null && db.weapons.Length > 0) weaponData = db.weapons[0];

            if (character == null || weaponData == null || db.movementConfig == null)
            {
                Debug.LogError("SliceBootstrap: database incomplete (character/weapon/movementConfig).");
                return;
            }

            motor.Init(db.movementConfig, StatFormulas.SpeedMult(character.baseSpeed, 0, 0f));
            health.Init(character, db.movementConfig);
            weapon.Init(weaponData);
            motor.FellFrom += health.ApplyFallDamage;

            waveManager.Begin();
        }
    }
}
```

- [ ] **Step 2: Créer l'asmdef éditeur + SliceSceneBuilder**

`Assets/_Game/Scripts/Editor/Scene/Game.Runtime.Editor.asmdef` :

```json
{
    "name": "Game.Runtime.Editor",
    "rootNamespace": "Game.Runtime.Editor",
    "references": [
        "Game.Runtime",
        "Game.Data",
        "Unity.AI.Navigation"
    ],
    "includePlatforms": ["Editor"]
}
```

`Assets/_Game/Scripts/Editor/Scene/SliceSceneBuilder.cs` :

```csharp
using Game.Data;
using Game.Runtime;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Runtime.Editor
{
    public static class SliceSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Slice.unity";
        const string DatabasePath = "Assets/_Game/Data/GameDatabase.asset";

        [MenuItem("Tools/Game/Build Slice Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var arena = new GameObject("Arena");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(arena.transform);
            ground.transform.localScale = new Vector3(4f, 1f, 4f); // plane 10 m -> 40 x 40 m
            AddCube(arena.transform, "WallN", new Vector3(0f, 1.5f, 20f), new Vector3(40f, 3f, 1f));
            AddCube(arena.transform, "WallS", new Vector3(0f, 1.5f, -20f), new Vector3(40f, 3f, 1f));
            AddCube(arena.transform, "WallE", new Vector3(20f, 1.5f, 0f), new Vector3(1f, 3f, 40f));
            AddCube(arena.transform, "WallW", new Vector3(-20f, 1.5f, 0f), new Vector3(1f, 3f, 40f));
            AddCube(arena.transform, "Crate1", new Vector3(5f, 0.75f, 6f), new Vector3(1.5f, 1.5f, 1.5f));
            AddCube(arena.transform, "Crate2", new Vector3(-7f, 0.75f, -4f), new Vector3(1.5f, 1.5f, 1.5f));
            AddCube(arena.transform, "Crate3", new Vector3(8f, 0.75f, -8f), new Vector3(1.5f, 1.5f, 1.5f));
            AddCube(arena.transform, "HighPlatform", new Vector3(-4f, 2.5f, 10f), new Vector3(4f, 5f, 4f)); // pour tester les dégâts de chute

            var surface = arena.AddComponent<NavMeshSurface>();
            surface.BuildNavMesh();

            var contextGo = new GameObject("GameContext");
            var context = contextGo.AddComponent<GameContext>();
            context.database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (context.database == null)
                Debug.LogError($"SliceSceneBuilder: GameDatabase introuvable a {DatabasePath}");

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1.2f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();

            var motor = player.AddComponent<PlayerMotor>();
            var look = player.AddComponent<PlayerLook>();
            look.cameraPivot = pivot.transform;
            var health = player.AddComponent<PlayerHealth>();
            var weapon = player.AddComponent<WeaponController>();
            weapon.aimCamera = cam;
            var bridge = player.AddComponent<PlayerInputBridge>();
            bridge.motor = motor;
            bridge.look = look;
            bridge.weapon = weapon;

            var wavesGo = new GameObject("Waves");
            var spawner = wavesGo.AddComponent<EnemySpawner>();
            spawner.context = context;
            spawner.player = player.transform;
            var waveManager = wavesGo.AddComponent<WaveManager>();
            waveManager.context = context;
            waveManager.spawner = spawner;
            var offsets = new[]
            {
                new Vector3(15f, 0f, 15f), new Vector3(-15f, 0f, 15f),
                new Vector3(15f, 0f, -15f), new Vector3(-15f, 0f, -15f)
            };
            var points = new Transform[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                var p = new GameObject($"SpawnPoint{i}");
                p.transform.SetParent(wavesGo.transform);
                p.transform.position = offsets[i];
                points[i] = p.transform;
            }
            waveManager.spawnPoints = points;

            var bootstrapGo = new GameObject("SliceBootstrap");
            var bootstrap = bootstrapGo.AddComponent<SliceBootstrap>();
            bootstrap.context = context;
            bootstrap.motor = motor;
            bootstrap.look = look;
            bootstrap.health = health;
            bootstrap.weapon = weapon;
            bootstrap.waveManager = waveManager;

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            Debug.Log($"SliceSceneBuilder: scene sauvegardee -> {ScenePath}");
        }

        /// Nécessaire pour recharger la scène par nom (bouton Rejouer) et pour les builds device.
        static void RegisterInBuildSettings()
        {
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == ScenePath) return;
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void AddCube(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = size;
        }
    }
}
```

- [ ] **Step 3: Compiler via MCP** (0 erreur), puis exécuter le menu : `mcp__UnityMCP__execute_menu_item(menu_path="Tools/Game/Build Slice Scene")`, puis `read_console` → attendu : log « scene sauvegardee », aucune erreur, et `Assets/_Game/Scenes/Slice.unity` existe.

- [ ] **Step 4: Tests MCP** (EditMode, tout vert) puis commit

```bash
git add Assets/_Game/Scripts/Runtime/Core/SliceBootstrap.cs Assets/_Game/Scripts/Editor/Scene Assets/_Game/Scenes
git commit -m "feat: slice scene builder menu and runtime bootstrap wiring"
```

*Vérif manuelle à consigner : ouvrir Slice.unity, Play — on se déplace au clavier, les vagues spawnent, on peut tuer les ennemis (pas encore de HUD).*

---

### Task 13: HUD runtime + contrôles tactiles

**Files:**
- Create: `Assets/_Game/Scripts/Runtime/HUD/HudController.cs`
- Create: `Assets/_Game/Scripts/Runtime/HUD/HudRadar.cs`
- Create: `Assets/_Game/Scripts/Runtime/HUD/TouchControlsRoot.cs`
- Modify: `Assets/_Game/Scripts/Runtime/Core/SliceBootstrap.cs` (ajoute HUD + tactile + écran de mort dans `Start`)

**Interfaces:**
- Consumes: événements de `PlayerHealth` (`HealthChanged`, `Died`), `WeaponController` (`AmmoChanged`, `ReloadingChanged`), `WaveManager` (`WaveStarted`, `AliveCountChanged`, `Alive`, `Money`), `PlayerMotor` (`StaminaCurrent`, `StaminaMax`), `EnemySpawner.RadarColor(EnemyData)`.
- Produces: `HudController` avec `void Init(PlayerHealth health, WeaponController weapon, WaveManager waves, PlayerMotor motor, Transform playerBody)` (construit tout le canvas uGUI en code, y compris `EventSystem` + `InputSystemUIInputModule` et le `HudRadar`) et `void ShowDeathScreen()` (rejouer = `SceneManager.LoadScene(scène active)`) ; `static GameObject TouchControlsRoot.CreateIfNeeded(PlayerLook look)` (retourne null si pas de `Touchscreen.current`).

Texte affiché en **français** : « Vague %d », « %d ennemis restants », « RECHARGE… », « Vous êtes mort », « Rejouer ». Police : `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` (pas de TMP).

- [ ] **Step 1: Implémenter HudController**

`Assets/_Game/Scripts/Runtime/HUD/HudController.cs` :

```csharp
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// HUD uGUI entièrement construit en code (D7) : vie, stamina, munitions,
    /// vague/restants, radar, écran de mort.
    public class HudController : MonoBehaviour
    {
        PlayerHealth health;
        WeaponController weapon;
        WaveManager waves;
        PlayerMotor motor;

        RectTransform healthFill;
        RectTransform staminaFill;
        Text ammoText;
        Text waveText;
        GameObject deathPanel;
        int lastWave;
        int lastAlive;
        bool reloading;
        int inMag, magSize;

        static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public void Init(PlayerHealth health, WeaponController weapon, WaveManager waves, PlayerMotor motor, Transform playerBody)
        {
            this.health = health;
            this.weapon = weapon;
            this.waves = waves;
            this.motor = motor;

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("HudCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // Barres de vie et de stamina (bas gauche)
            healthFill = MakeBar(root, new Vector2(40f, 80f), new Vector2(400f, 26f), new Color(0.85f, 0.2f, 0.2f));
            staminaFill = MakeBar(root, new Vector2(40f, 44f), new Vector2(400f, 14f), new Color(0.95f, 0.8f, 0.25f));

            // Munitions (bas droite)
            ammoText = MakeText(root, "", 40, TextAnchor.LowerRight);
            var ammoRect = ammoText.rectTransform;
            ammoRect.anchorMin = ammoRect.anchorMax = new Vector2(1f, 0f);
            ammoRect.pivot = new Vector2(1f, 0f);
            ammoRect.anchoredPosition = new Vector2(-40f, 40f);
            ammoRect.sizeDelta = new Vector2(500f, 60f);

            // Vague (haut centre)
            waveText = MakeText(root, "", 34, TextAnchor.UpperCenter);
            var waveRect = waveText.rectTransform;
            waveRect.anchorMin = waveRect.anchorMax = new Vector2(0.5f, 1f);
            waveRect.pivot = new Vector2(0.5f, 1f);
            waveRect.anchoredPosition = new Vector2(0f, -24f);
            waveRect.sizeDelta = new Vector2(800f, 50f);

            // Réticule (centre)
            var crosshair = MakeImage(root, new Color(1f, 1f, 1f, 0.8f));
            crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.rectTransform.sizeDelta = new Vector2(6f, 6f);

            // Radar (haut droite)
            var radarGo = new GameObject("Radar");
            radarGo.transform.SetParent(root, false);
            var radar = radarGo.AddComponent<HudRadar>();
            radar.Init(waves, playerBody);

            // Écran de mort
            deathPanel = BuildDeathPanel(root);

            this.health.HealthChanged += OnHealthChanged;
            this.health.Died += ShowDeathScreen;
            this.weapon.AmmoChanged += OnAmmoChanged;
            this.weapon.ReloadingChanged += OnReloadingChanged;
            this.waves.WaveStarted += w => { lastWave = w; RefreshWaveText(); };
            this.waves.AliveCountChanged += n => { lastAlive = n; RefreshWaveText(); };

            OnHealthChanged(health.Health, health.MaxHealth);
        }

        void Update()
        {
            if (motor != null && motor.StaminaMax > 0f)
                staminaFill.anchorMax = new Vector2(Mathf.Clamp01(motor.StaminaCurrent / motor.StaminaMax), 1f);
        }

        void OnHealthChanged(float current, float max)
        {
            healthFill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
        }

        void OnAmmoChanged(int mag, int size) { inMag = mag; magSize = size; RefreshAmmoText(); }
        void OnReloadingChanged(bool value) { reloading = value; RefreshAmmoText(); }

        void RefreshAmmoText() => ammoText.text = reloading ? "RECHARGE…" : $"{inMag} / {magSize}";
        void RefreshWaveText() => waveText.text = $"Vague {lastWave} — {lastAlive} ennemis restants";

        public void ShowDeathScreen()
        {
            deathPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        GameObject BuildDeathPanel(RectTransform root)
        {
            var panel = MakeImage(root, new Color(0f, 0f, 0f, 0.75f)).gameObject;
            panel.name = "DeathPanel";
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var label = MakeText(rect, "Vous êtes mort", 64, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            label.rectTransform.sizeDelta = new Vector2(900f, 90f);

            var buttonGo = new GameObject("RestartButton");
            buttonGo.transform.SetParent(rect, false);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.3f, 0.2f);
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
            buttonRect.sizeDelta = new Vector2(320f, 80f);
            var button = buttonGo.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Slice est dans les Build Settings (tâche 12)
            });
            var buttonLabel = MakeText(buttonRect, "Rejouer", 36, TextAnchor.MiddleCenter);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.sizeDelta = Vector2.zero;

            panel.SetActive(false);
            return panel;
        }

        static RectTransform MakeBar(RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            var back = MakeImage(parent, new Color(0f, 0f, 0f, 0.5f));
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = new Vector2(0f, 0f);
            back.rectTransform.pivot = new Vector2(0f, 0f);
            back.rectTransform.anchoredPosition = position;
            back.rectTransform.sizeDelta = size;

            var fill = MakeImage(back.rectTransform, color);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one; // anchorMax.x pilote le remplissage
            fill.rectTransform.sizeDelta = Vector2.zero;
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            return fill.rectTransform;
        }

        static Image MakeImage(RectTransform parent, Color color)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text MakeText(RectTransform parent, string content, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = UiFont;
            text.fontSize = size;
            text.alignment = anchor;
            text.text = content;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
```

- [ ] **Step 2: Implémenter HudRadar**

`Assets/_Game/Scripts/Runtime/HUD/HudRadar.cs` :

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Radar : blips des ennemis vivants, position relative au joueur (rotation incluse),
    /// couleur = radarColor du type. Portée fixe 20 m. // TODO(spec-web): portée radar non extraite.
    public class HudRadar : MonoBehaviour
    {
        const float RangeMeters = 20f;
        const float RadiusPixels = 110f;

        WaveManager waves;
        Transform player;
        readonly List<Image> blips = new List<Image>();

        public void Init(WaveManager waves, Transform player)
        {
            this.waves = waves;
            this.player = player;

            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -30f);
            rect.sizeDelta = new Vector2(RadiusPixels * 2f, RadiusPixels * 2f);
            var background = gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.35f);
            background.raycastTarget = false;
        }

        void LateUpdate()
        {
            if (waves == null || player == null) return;
            var alive = waves.Alive;

            while (blips.Count < alive.Count)
            {
                var go = new GameObject("Blip");
                go.transform.SetParent(transform, false);
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                ((RectTransform)go.transform).sizeDelta = new Vector2(10f, 10f);
                blips.Add(image);
            }

            for (int i = 0; i < blips.Count; i++)
            {
                bool used = i < alive.Count && alive[i] != null;
                blips[i].gameObject.SetActive(used);
                if (!used) continue;

                Vector3 relative = alive[i].transform.position - player.position;
                Vector3 local = Quaternion.Euler(0f, -player.eulerAngles.y, 0f) * relative;
                Vector2 planar = new Vector2(local.x, local.z) / RangeMeters;
                if (planar.magnitude > 1f) planar = planar.normalized;
                ((RectTransform)blips[i].transform).anchoredPosition = planar * RadiusPixels;
                blips[i].color = EnemySpawner.RadarColor(alive[i].Data);
            }
        }
    }
}
```

- [ ] **Step 3: Implémenter TouchControlsRoot**

`Assets/_Game/Scripts/Runtime/HUD/TouchControlsRoot.cs` :

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Joysticks virtuels (D3/D11) : stick gauche -> <Gamepad>/leftStick, drag droite -> PlayerLook,
    /// boutons saut/dash/recharge -> boutons gamepad. Créé seulement si un écran tactile est présent.
    public static class TouchControlsRoot
    {
        public static GameObject CreateIfNeeded(PlayerLook look)
        {
            if (Touchscreen.current == null) return null;

            var root = new GameObject("TouchControls");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            root.AddComponent<GraphicRaycaster>();
            var rootRect = (RectTransform)root.transform;

            // Zone de visée : moitié droite de l'écran
            var lookArea = new GameObject("LookArea");
            lookArea.transform.SetParent(rootRect, false);
            var lookImage = lookArea.AddComponent<Image>();
            lookImage.color = new Color(0f, 0f, 0f, 0.001f); // invisible mais raycastable
            var lookRect = (RectTransform)lookArea.transform;
            lookRect.anchorMin = new Vector2(0.5f, 0f);
            lookRect.anchorMax = new Vector2(1f, 1f);
            lookRect.sizeDelta = Vector2.zero;
            var drag = lookArea.AddComponent<TouchLookArea>();
            drag.look = look;

            // Stick gauche
            var stickBack = new GameObject("MoveStick");
            stickBack.transform.SetParent(rootRect, false);
            var backImage = stickBack.AddComponent<Image>();
            backImage.color = new Color(1f, 1f, 1f, 0.15f);
            var backRect = (RectTransform)stickBack.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 0f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.anchoredPosition = new Vector2(240f, 240f);
            backRect.sizeDelta = new Vector2(260f, 260f);

            var knob = new GameObject("Knob");
            knob.transform.SetParent(backRect, false);
            var knobImage = knob.AddComponent<Image>();
            knobImage.color = new Color(1f, 1f, 1f, 0.45f);
            ((RectTransform)knob.transform).sizeDelta = new Vector2(110f, 110f);
            var stick = knob.AddComponent<OnScreenStick>();
            stick.movementRange = 90f;
            SetControlPath(stick, "<Gamepad>/leftStick");

            AddButton(rootRect, "Saut", new Vector2(-140f, 340f), "<Gamepad>/buttonSouth");
            AddButton(rootRect, "Dash", new Vector2(-320f, 220f), "<Gamepad>/buttonEast");
            AddButton(rootRect, "Recharge", new Vector2(-140f, 220f), "<Gamepad>/buttonWest");

            return root;
        }

        static void AddButton(RectTransform parent, string label, Vector2 position, string controlPath)
        {
            var go = new GameObject($"Button_{label}");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.25f);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(140f, 100f);
            var button = go.AddComponent<OnScreenButton>();
            SetControlPath(button, controlPath);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(rect, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
            ((RectTransform)textGo.transform).sizeDelta = new Vector2(140f, 100f);
        }

        static void SetControlPath(OnScreenControl control, string path)
        {
            control.controlPath = path; // propriété publique sur OnScreenControl (Input System 1.19)
        }
    }

    public class TouchLookArea : MonoBehaviour, IDragHandler
    {
        public PlayerLook look;
        public float touchSensitivity = 2.5f;

        public void OnDrag(PointerEventData eventData)
        {
            if (look != null) look.AddLook(eventData.delta * touchSensitivity * 0.1f);
        }
    }
}
```

Note : si `control.controlPath` s'avérait en lecture seule dans la version installée de l'Input System, fallback : `control.GetType().GetProperty("controlPath").SetValue(control, path);` (réflexion). Vérifier à la compilation.

- [ ] **Step 4: Étendre SliceBootstrap.Start** — dans `Assets/_Game/Scripts/Runtime/Core/SliceBootstrap.cs`, remplacer les deux dernières lignes de `Start()` (`motor.FellFrom += ...; waveManager.Begin();`) par :

```csharp
            motor.FellFrom += health.ApplyFallDamage;

            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HudController>();
            hud.Init(health, weapon, waveManager, motor, motor.transform);

            TouchControlsRoot.CreateIfNeeded(look);

            waveManager.Begin();
```

- [ ] **Step 5: Compiler + tests MCP, reconstruire la scène** (`Tools/Game/Build Slice Scene`) et vérifier `read_console` sans erreur.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/HUD Assets/_Game/Scripts/Runtime/Core/SliceBootstrap.cs Assets/_Game/Scenes
git commit -m "feat: runtime-generated HUD (health, ammo, wave, radar, death screen) and touch controls"
```

*Vérifs manuelles à consigner : lisibilité du HUD, radar cohérent avec les positions, écran de mort + Rejouer, joysticks via Device Simulator ou build Android.*

---

### Task 14: Tests PlayMode smoke + documentation + liste finale

**Files:**
- Create: `Assets/_Game/Tests/PlayMode/Game.Runtime.PlayTests.asmdef`
- Create: `Assets/_Game/Tests/PlayMode/SliceSmokeTests.cs`
- Modify: `unity-spec/NEXT-STEPS.md` (étape 4 : état d'avancement)
- Modify: `docs/ROADMAP.md` (section « où on en est »)
- Modify: `CLAUDE.md` (section « Où on en est » + « Par où reprendre »)

**Interfaces:**
- Consumes: la scène `Assets/_Game/Scenes/Slice.unity` (tâche 12/13), `GameContext.Instance`, `WaveManager`, `EnemyHealth.TakeHit`.
- Produces: 2 tests PlayMode ; docs à jour ; la **liste des vérifications manuelles pour Nathan** consolidée dans le message final de la session (règle CLAUDE.md global : ne pas interrompre, tout livrer à la fin).

- [ ] **Step 1: Créer l'asmdef PlayMode**

`Assets/_Game/Tests/PlayMode/Game.Runtime.PlayTests.asmdef` :

```json
{
    "name": "Game.Runtime.PlayTests",
    "rootNamespace": "Game.Runtime.PlayTests",
    "references": [
        "Game.Runtime",
        "Game.Data"
    ],
    "optionalUnityReferences": ["TestAssemblies"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

- [ ] **Step 2: Écrire les smoke tests**

`Assets/_Game/Tests/PlayMode/SliceSmokeTests.cs` :

```csharp
using System.Collections;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.PlayTests
{
    public class SliceSmokeTests
    {
        const string ScenePath = "Assets/_Game/Scenes/Slice.unity";

        static void LoadSlice()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
        }

        [UnityTest]
        public IEnumerator Scene_BootstrapsWithoutErrors()
        {
            LoadSlice();
            yield return null; // Awake/Start
            yield return null;

            Assert.IsNotNull(GameContext.Instance, "GameContext.Instance absent");
            Assert.IsNotNull(GameContext.Instance.database, "GameDatabase non assignée");
            Assert.IsNotNull(Object.FindFirstObjectByType<PlayerMotor>(), "PlayerMotor absent");
            Assert.IsNotNull(Object.FindFirstObjectByType<WaveManager>(), "WaveManager absent");
        }

        [UnityTest]
        public IEnumerator FirstWave_SpawnsThreeShootersAndTheyDie()
        {
            LoadSlice();
            yield return null;
            var waves = Object.FindFirstObjectByType<WaveManager>();
            Assert.IsNotNull(waves);

            // intermission 2.5 s (WaveConfig) + marge
            float timeout = Time.time + 10f;
            while (waves.AliveCount == 0 && Time.time < timeout) yield return null;

            Assert.AreEqual(1, waves.CurrentWave);
            Assert.AreEqual(3, waves.AliveCount, "vague 1 = 3 shooters");

            // Tue tout le monde par headshots surpuissants
            var alive = new System.Collections.Generic.List<EnemyHealth>(waves.Alive);
            foreach (var enemy in alive)
                enemy.TakeHit(enemy.transform.position + Vector3.up * 1.6f, 1000f);
            yield return null;

            Assert.AreEqual(0, waves.AliveCount, "tous les ennemis doivent être morts");
        }
    }
}
```

- [ ] **Step 3: Lancer les tests PlayMode via MCP** — `run_tests(mode="PlayMode", test_filter="Game.Runtime.PlayTests")` + `get_test_job(wait_timeout=120)` → 2 verts. Relancer aussi les EditMode (tout vert).

- [ ] **Step 4: Mettre à jour la documentation**

Dans `unity-spec/NEXT-STEPS.md` : marquer l'étape 4 « ✅ FAIT (vertical slice jouable, branche `feat/vertical-slice-solo`) » avec 3 lignes de résumé (ce qui est jouable, ce qui est différé — comportements spéciaux, combo, tactile à valider sur device) et pointer la prochaine étape (Phase 3 maps OU durcissement — à décider avec Nathan).
Dans `docs/ROADMAP.md` : mettre à jour « Où on en est précisément » (étape 2 de la vue générale : slice jouable livrée).
Dans `CLAUDE.md` : mettre à jour « Où on en est », « Par où reprendre » et le tableau des `.md` (ajouter la spec + le plan de l'étape 4).

- [ ] **Step 5: Commit final**

```bash
git add Assets/_Game/Tests/PlayMode unity-spec/NEXT-STEPS.md docs/ROADMAP.md CLAUDE.md
git commit -m "test: slice PlayMode smoke tests; docs: step 4 status update"
```

- [ ] **Step 6: Clore la branche** — invoquer le skill `superpowers:finishing-a-development-branch`. Ne PAS merger ni supprimer la branche sans l'accord de Nathan : livrer dans le message final le récap + la liste complète des vérifications manuelles (Play Mode clavier, feel du tir auto, HUD, tactile sur device/simulateur, second wind, dégâts de chute depuis la plateforme haute, `PlaceholderLibrary` toujours à remplir).

---

## Récap des vérifications manuelles (à maintenir pendant l'exécution)

Chaque tâche ajoute ses items ; le message final à Nathan reprend la liste complète :

1. (T7) Marche/course/saut/dash au clavier, sensibilité souris, drain de stamina.
2. (T9) Tir auto quand le réticule est sur un ennemi ; cadence, reload, spread shotgun (tester en changeant l'arme de départ temporairement dans `SliceBootstrap`).
3. (T12) Scène Slice : Play → vagues spawnent, ennemis tuables, composition v1=3 shooters, v2 avec rushers au contact.
4. (T13) HUD lisible, radar cohérent, écran de mort + Rejouer, joysticks (Device Simulator ou build Android).
5. (T8) Second wind : survivre une fois à un coup fatal avec 1 PV + invulnérabilité brève.
6. (T12) Dégâts de chute : sauter de la plateforme haute (~5 m) → ~10 PV perdus.
7. (Étape 3, toujours ouvert) Remplir `PlaceholderLibrary` dans l'Inspector (non bloquant : primitives en secours).
8. (Spec §2) Vérifier côté web : mult. d'armure, délai de régén, réserve de munitions, portée radar — puis reporter dans les JSON et les champs `TODO(spec-web)`.




