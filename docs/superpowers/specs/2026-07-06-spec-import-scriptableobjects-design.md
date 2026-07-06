# Design — Import ScriptableObjects depuis unity-spec/*.json

Date : 2026-07-06
Contexte : étape 3 de `unity-spec/NEXT-STEPS.md` — premier script de code du projet Unity `FPS`, qui transforme les JSON de `unity-spec/` (spec gameplay extraite du jeu web) en ScriptableObjects Unity.

## Objectif

Fournir un script Editor qui lit `unity-spec/*.json` et génère/actualise les ScriptableObjects de gameplay, en gardant `unity-spec/` comme unique source de vérité pour les valeurs de jeu, tout en laissant les assets d'art (modèles 3D, icônes) totalement découplés — ils arrivent au fur et à mesure sans jamais bloquer la logique (principe déjà posé dans `unity-spec/README.md`).

## Décisions actées

- **Les refs d'assets (modèle, icône) sont assignées à la main dans l'Inspector Unity et ne sont JAMAIS écrites par l'importeur.** Le bloc `assets` du JSON reste informatif côté Unity (utile pour savoir ce qui manque). Seuls les champs gameplay (stats, prix, rareté, description…) sont écrasés à chaque import. Exception : `EnemyData.weapon`, qui référence un `WeaponData` généré (donc lui aussi géré par l'importeur, cf. section Ordre d'import).
- **Les formules et règles en pseudo-code (waves.json, economy.json) sont transposées à la main en C#**, pas générées depuis le JSON. L'importeur ne produit que les *constantes* (seuils, multiplicateurs de difficulté, coûts de base…) sous forme de ScriptableObjects de config. Si une formule change côté web, on met à jour le C# à la main — les formules changent rarement, contrairement aux valeurs.
- **Granularité hybride** : un `.asset` par entrée de catalogue (armes, ennemis, persos, power-ups, items) + un `.asset` singleton par bloc de constantes (mouvement, économie, vagues, upgrades, combat ennemis). Ça donne des refs individuelles stables (utile pour lier une arme à un prefab, une anim, etc.) sans avoir d'assets artificiels pour de simples constantes.
- **Fixtures JSON dédiées pour les tests EditMode**, pas les vrais fichiers `unity-spec/*.json` — tests rapides, stables, indépendants de l'évolution de la vraie spec.

## Structure des données

### Enums partagés

`Assets/_Game/Scripts/Data/Enums.cs` : `Rarity` (Common/Rare/Epic/Legendary), `WeaponCategory` (Assault/Smg/Sniper/Shotgun/Pistol/Lmg/Melee), `ArmorType` (None/Torso/Full), `PvpPolicy` (Allowed/Rebalance/Exclude), `BoostSlot` (Vest/Boots/Medal), `BoostStat` (Health/Regen/Speed).

Le code (enums, noms de champs) est en anglais ; le contenu affiché (noms d'armes, descriptions) reste en français, tel qu'extrait du JSON.

### ScriptableObjects catalogues (un `.asset` par entrée)

| Classe | Source JSON | Champs gameplay (écrasés à l'import) | Refs assets (jamais touchées) |
|---|---|---|---|
| `WeaponData` | weapons.json → `weapons[]` | id, name, category, rarity, price, desc, semiAuto, melee, mag, cooldown, reload, dmgMult, recoil, range, pellets | modelPrefab, icon |
| `EnemyData` | enemies.json → `types{}` | id, name, hpMult, speedMult, scale, melee, damageMult, burst, armor, radarColor, glow, opacity, shootCd/shootRange (overrides), **weapon (ref WeaponData)** | modelPrefab |
| `CharacterData` | player.json → `characters[]` | id, name, price, rarity, desc, baseHealth, baseRegen, baseSpeed, skills[] (icon+name+desc) | modelPrefab, icon |
| `PowerUpData` | powerups.json → `powerups{}` | id, name, color, duration (0 = instant), effectDesc, pvpPolicy | icon/vfx (futur) |
| `AccessoryData` | items.json → `accessories.helmets[]` / `costumes[]` | id, accessoryType (Helmet/Costume), name, price, rarity, desc, bonuses[] | image (Sprite), modelPrefab |
| `BoostData` | items.json → `boosts.items[]` | id, slot, stat, bonus, price, rarity | icon |
| `ConsumableData` | items.json → `consumables.items[]` | key, name, price, iconName | icon |
| `DanceData` | items.json → `dances.items[]` | id, name, clipName, price, rarity | animationClip |

### ScriptableObjects de config (un `.asset` singleton)

Dans `Assets/_Game/Data/Config/` :

- `MovementConfig` — bloc `movement` + `secondWind` de player.json.
- `EnemyCombatConfig` — `baseStats`, `hitZones`, `vip`, `bossKillBonus` d'enemies.json.
- `WaveConfig` — `maxEnemiesPerWave`, `intermissionSeconds`, `bossEvery` de waves.json.
- `EconomyConfig` — perKill, vipMult, difficultés, raretés (couleurs/tiers), seuils loadout, maxLevel, leaderboard, d'economy.json.
- `UpgradeConfig` — specs d'upgrades armes (weapons.json → `upgrades`) + joueur (player.json → `playerUpgrades`) : max, baseCost, effet/perLevel.
- `PowerUpConfig` — dropChance, dropChanceLootMaster, pickupRadius, defaultDuration, instantConstants de powerups.json.
- `PlaceholderLibrary` — **écrit à la main, jamais importé.** Prefab placeholder par `WeaponCategory`, prefab ennemi générique, prefab perso générique, sprite icône générique. Point unique de résolution des assets manquants.

### Racine

`GameDatabase` (`Assets/_Game/Data/GameDatabase.asset`) : régénéré à chaque import, contient les listes de tous les catalogues + refs vers tous les configs. C'est le seul point d'entrée référencé depuis les scènes/systèmes runtime.

### Formules en C# (à la main, pas importées)

`Assets/_Game/Scripts/Data/Rules/` : `WaveRules` (composition de vague, `enemiesForWave`, `enemyHpForWave`, `waveScoreMult`, `waveMoneyBonus`), `EconomyRules` (combo, XP, argent par kill), `UpgradeFormulas` (coût par niveau, effet cumulé), `StatFormulas` (stats finales personnage). Chacune est testée en EditMode contre des valeurs calculées à la main depuis les JSON (ex. `WaveRules.EnemyHpForWave(5, Difficulty.Normal) == 194`, soit `round(100 × 1.18^4)`).

## Résolution des placeholders (runtime)

Chaque SO catalogue expose une méthode de résolution qui retourne la ref assignée si non-null, sinon délègue à `PlaceholderLibrary` :

```csharp
public GameObject ResolveModel(PlaceholderLibrary lib) =>
    modelPrefab != null ? modelPrefab : lib.GetWeaponPlaceholder(category);
```

La logique de gameplay/rendu n'exige donc jamais un asset — conforme à la règle du README.

## Pipeline d'import

### Déclenchement

Menu `Tools/Game/Import Spec from JSON` — action délibérée, pas un `AssetPostprocessor` automatique. Avant d'écrire quoi que ce soit, une fenêtre de confirmation liste ce qui sera créé / mis à jour / marqué orphelin.

### Organisation du code

`Assets/_Game/Scripts/Editor/Import/` :

- `SpecImporter.cs` — orchestrateur (le menu item). Appelle un importeur par domaine dans un ordre fixe, puis régénère `GameDatabase`.
- Un importeur par domaine : `WeaponImporter`, `EnemyImporter`, `CharacterImporter`, `PowerUpImporter`, `ItemsImporter` (accessoires/boosts/consommables/danses), plus les importeurs de config (`WaveConfigImporter`, `EconomyConfigImporter`, etc.).
- `ImportReport.cs` — struct de résultat (créés / mis à jour / orphelins / warnings), affichée en fenêtre + loggée en console à la fin de l'import.

### Ordre d'import (fixe)

1. `weapons.json` → `WeaponData[]`
2. `enemies.json` → `EnemyData[]` (résout `weapon` en ref vers les `WeaponData` créés à l'étape 1) + `EnemyCombatConfig`
3. `player.json` → `CharacterData[]` + `MovementConfig` + entrées `UpgradeConfig` (joueur)
4. `economy.json` → `EconomyConfig`
5. `items.json` → `AccessoryData[]`, `BoostData[]`, `ConsumableData[]`, `DanceData[]`
6. `powerups.json` → `PowerUpData[]` + `PowerUpConfig`
7. `waves.json` → `WaveConfig`
8. Régénération de `GameDatabase`

### Parsing JSON

Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`) — les structures imbriquées (armes, ennemis, skills, bonuses) dépassent ce que `JsonUtility` gère proprement.

### Matching et écriture des assets

Pour chaque entrée : recherche de l'asset existant par `id` via `AssetDatabase.FindAssets` filtré par type + comparaison de l'id sérialisé. Si trouvé → met à jour uniquement les champs gameplay qui diffèrent (pas de ré-écriture si rien n'a changé — condition d'idempotence). Si absent → crée un nouvel `.asset` au chemin prévu.

### Idempotence

Ré-exécuter l'import sans changement JSON ne doit produire **aucun diff Git** sur les `.asset`. Implique d'écrire une valeur uniquement si elle diffère de la valeur actuelle (`EditorUtility.SetDirty` seulement si changement réel).

### Suppression

Un `id` disparu du JSON → l'asset correspondant est listé comme orphelin dans `ImportReport`, mais **jamais supprimé automatiquement** (évite de perdre un asset lié à un renommage accidentel). Suppression manuelle si confirmé volontaire.

### Chemins de sortie

```
Assets/_Game/Data/Weapons/<id>.asset
Assets/_Game/Data/Enemies/<id>.asset
Assets/_Game/Data/Characters/<id>.asset
Assets/_Game/Data/PowerUps/<id>.asset
Assets/_Game/Data/Items/Accessories/<id>.asset
Assets/_Game/Data/Items/Boosts/<id>.asset
Assets/_Game/Data/Items/Consumables/<id>.asset
Assets/_Game/Data/Items/Dances/<id>.asset
Assets/_Game/Data/Config/<ConfigName>.asset  (singletons)
Assets/_Game/Data/GameDatabase.asset
```

### Source des JSON

Lus directement depuis `unity-spec/*.json` à la racine du projet Unity `FPS` (le dossier `unity-spec/` vit dans ce même repo, pas dans un projet séparé). Pas de copie dans `Assets/` — `unity-spec/` reste l'unique source de vérité, conformément au README.

## Cas particuliers repérés dans les JSON actuels

- **`ghost`** (enemies.json) : stats incomplètes (`"_doc": "Voir main.js pour stats exactes"`). L'importeur applique des valeurs par défaut (hpMult/speedMult = 1.0) et ajoute un warning dans `ImportReport`. À corriger quand l'extraction web sera complétée (cf. `unity-spec/README.md` section "Ce qui n'est pas encore extrait").
- **Champs `_doc`** : ignorés partout, jamais mappés sur un champ de ScriptableObject.
- **`unityNotes`** (economy.json, player.json) : notes textuelles pour l'équipe, pas importées.
- **`pvp` sur les power-ups** : mappé sur `PowerUpData.pvpPolicy` (utile dès maintenant pour préparer la passe d'équilibrage PvP en phase 5).

## Tests (Unity Test Framework, EditMode)

Fixtures dédiées dans `Assets/_Game/Tests/Editor/Fixtures/` (2-3 entrées par domaine, JSON minimal et stable) — pas les vrais fichiers `unity-spec/*.json`, pour des tests rapides et non affectés par l'évolution de la vraie spec.

- **Par importeur** : import depuis fixture → vérifie que les SO créés ont les bons champs gameplay ; simuler une ref d'asset assignée à la main, ré-importer, vérifier qu'elle survit ; ré-importer sans changement, vérifier qu'aucun asset n'est marqué modifié (idempotence).
- **Par règle métier** : `WaveRules`, `EconomyRules`, `UpgradeFormulas`, `StatFormulas` testés contre des valeurs calculées à la main depuis les formules documentées dans les JSON.
- `PlaceholderLibrary` : sanity check uniquement (chaque catégorie a un placeholder assigné) — c'est une config manuelle, pas une logique à tester en profondeur.

## Hors scope (explicitement)

- Les formules elles-mêmes ne sont pas régénérées depuis le JSON — seulement leurs constantes.
- Pas d'auto-import déclenché par modification de fichier (`AssetPostprocessor`) — toujours une action manuelle via le menu.
- Pas de suppression automatique des assets orphelins.
- Extraction des données manquantes (succès, contrats quotidiens, killstreaks, stats détaillées consommables, chien de guerre, prestige) — hors scope de ce script, à faire côté `unity-spec/` d'abord (cf. NEXT-STEPS.md).
