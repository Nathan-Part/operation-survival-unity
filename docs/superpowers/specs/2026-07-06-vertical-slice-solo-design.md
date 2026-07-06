# Spec de design — Étape 4 : vertical slice solo jouable

> **Statut :** validée (brainstorm autonome du 2026-07-06, mandaté par Nathan — questions posées et tranchées avec l'option recommandée).
> **Objectif de l'étape :** tuer des vagues d'ennemis dans une arène greybox, en Play Mode éditeur (clavier/souris) ET au tactile (joysticks virtuels), en lisant toutes les valeurs d'équilibrage depuis les ScriptableObjects générés par le pipeline d'import (étape 3). Jalon de `docs/migration-unity.md` Phase 2.

## 1. Décisions de design (questions tranchées)

| # | Question | Décision | Justification |
|---|---|---|---|
| D1 | Tir auto vs bouton | **Tir automatique** : l'arme tire quand le réticule est sur un ennemi à portée de l'arme et le cooldown écoulé. | Standard FPS mobile (Guns of Boom) ; deux pouces seulement ; équilibrage web pensé PvE. Le déclencheur de tir est une abstraction (`IFireTrigger`-like) pour permettre un bouton manuel plus tard (réglage, PvP). |
| D2 | Caméra | **Première personne**, caméra enfant du joueur. Yaw → corps, pitch → caméra (clamp ±85°). Pas de Cinemachine. | Identique au web ; zéro dépendance en plus. |
| D3 | Test sans téléphone | **Deux control schemes** sur les mêmes actions Input System : `KeyboardMouse` (éditeur) et `Touch` (joysticks virtuels uGUI). | Chaque système vérifiable en Play Mode sans build. |
| D4 | Ordre des chantiers | Scène greybox → contrôleur joueur → armes/tir → ennemi → vagues → tactile + HUD. | Le tactile est un binding fin ; le HUD consomme les événements des systèmes précédents. |
| D5 | Périmètre ennemi | **EnemyController générique data-driven** : archétypes *ranged* (FSM idle/chase/attack, rafales) et *melee* (chase + coup au contact), choisis par `EnemyData.melee`. Comportements spéciaux (bomber, medic, shielder, toxic, cryo, stunner, grenadier, commander, ghost, boss-sbires) **différés** : ces types utilisent leur archétype de base avec leurs stats propres. | La vague 2 spawne des rushers (mêlée) → mêlée obligatoire. Les stats (hpMult, speedMult, scale, damageMult, burst, overrides sniper) viennent des `EnemyData`, donc tous les types sont spawnables sans code par type. |
| D6 | Composition des vagues | Table de `waves.json → composition.rules` **écrite à la main en C# pur** (`WaveComposition`), comme `WaveRules`. Slot 0 toujours forcé `shooter`, vague boss = `[boss, shooter, shooter]`. | Même approche éprouvée que les formules de l'étape 3 ; testable EditMode. |
| D7 | HUD | **uGUI** pour toute la slice (joysticks + HUD). | `OnScreenStick`/`OnScreenButton` de l'Input System sont uGUI-natifs. Déviation assumée de `migration-unity.md` (UI Toolkit) ; UI Toolkit arrivera en phase 4 (boutique). |
| D8 | Zones de dégâts | **Hauteur du point d'impact** : y relatif aux pieds de l'ennemi, divisé par `scale`. `y ≥ 1.5` tête (60), `y < 0.6` jambes (15), sinon torse (25) — × `dmgMult` arme. | Spec web exacte (`enemies.json → hitZones`) ; marche avec des capsules sans rig. |
| D9 | Modèles manquants | Fallback runtime : `PlaceholderLibrary` d'abord, sinon **primitive générée par code** (capsule ennemi/perso, cube arme). Aucun NRE si `GameDatabase.placeholders` est null. | Règle projet : la logique n'exige jamais un asset. Le TODO manuel de Nathan reste non bloquant. |
| D10 | Difficulté | **Normal** (multiplicateurs 1.0), lue depuis `EconomyConfig.difficulties` (id `normal`). Sélecteur de difficulté hors périmètre. | La slice valide le gameplay, pas le méta. |
| D11 | Sprint/stamina mobile | Course = **déflexion complète du joystick** gauche (magnitude ≥ 0.9) ; marche sinon. Clavier : Shift. Drain/regen/minToRun depuis `MovementConfig.stamina`. | Pas de bouton sprint dédié sur mobile (pouce déjà occupé). |
| D12 | Combo au HUD | **Différé.** HUD slice = vie, munitions, vague + ennemis restants, radar. | Les killstreaks/combo ne sont pas encore extraits dans les JSON (« extraction restante » de NEXT-STEPS). |
| D13 | Arme du joueur | Arme de départ : `default` (Viper-AR), via `GameDatabase`. Pas de changement d'arme dans la slice (le loadout est phase 4). | Une arme suffit à valider tir/reload/dégâts ; le catalogue est déjà data-driven. |
| D14 | Perso joueur | `perso_blue-soldier` (santé 100, regen 10, speed 1.0) via `StatFormulas` avec niveaux d'upgrade 0. | Perso gratuit par défaut du web. |

## 2. Trous de spec (valeurs absentes des JSON) — implémentés avec défaut configurable

Chaque valeur est un champ sérialisé avec défaut raisonnable, marqué `// TODO(spec-web)` ; à confronter au `main.js` du web puis reporter dans les JSON (règle NEXT-STEPS : la spec doit rester exacte).

| Trou | Défaut retenu | Où |
|---|---|---|
| Multiplicateur de réduction d'armure (`torso`/`full`) | 0.5 sur les zones protégées | `EnemyHealth` |
| Délai de régén vie joueur après dégâts | 3 s (0 = régén continue) | `PlayerHealth` |
| Réserve de munitions | Infinie (reload illimité) | `WeaponController` |
| Portée du radar | 20 m | `HudRadar` |

## 3. Architecture

### Assemblies

- **`Game.Runtime`** (nouveau) : `Assets/_Game/Scripts/Runtime/`, référence `Game.Data`, `Unity.InputSystem`, `Unity.AI.Navigation`. Aucun code éditeur.
- **`Game.Runtime.Tests`** (nouveau, EditMode) : teste la logique pure de `Game.Runtime`.
- **`Game.Runtime.PlayTests`** (nouveau, PlayMode) : 2-3 smoke tests seulement.
- Existant inchangé : `Game.Data`, `Game.Data.Editor`, `Game.Data.Tests`.

### Principe : logique pure séparée des MonoBehaviours

Comme à l'étape 3 (`WaveRules` etc.), tout ce qui est calculable sans scène est une classe C# pure, testée en EditMode. Les MonoBehaviours sont des coquilles fines qui appliquent les résultats.

**Classes pures (testées) :**

| Classe | Rôle | Entrées → sorties |
|---|---|---|
| `HitZoneResolver` | Zone + dégâts d'un impact | (yLocal, scale, HitZoneSettings, dmgMult, armor) → dégâts int |
| `StaminaModel` | État stamina | (dt, veutCourir) → peut courir, valeur ; règles drain/regen/minToRun |
| `FallDamageRule` | Dégâts de chute | hauteur de chute → dégâts (`h ≤ safe ? 0 : min(max, round((h−safe)×perMeter))`) |
| `DashModel` | État du dash | cooldown, durée = distance/speed, direction |
| `WaveComposition` | Type d'ennemi par slot | (wave, index) → id de type ; slot 0 = shooter ; boss = [boss, shooter, shooter] |
| `WaveSpawnPlan` | Plan complet d'une vague | wave → liste d'ids (via `WaveRules.EnemiesForWave` + `WaveComposition`) |
| `AutoFireDecider` | Décision de tir auto | (cible sous réticule ?, distance, portée arme, cooldown) → tirer ? |
| `EnemyBrain` | FSM pure | (état, distances, timers, EnemyData+baseStats) → nouvel état + action (idle/chase/attack) |

**MonoBehaviours (fins) :**

| Composant | Rôle |
|---|---|
| `GameContext` | Point d'entrée scène : référence `GameDatabase`, expose configs et fallbacks placeholder. |
| `PlayerMotor` | `CharacterController` : marche/course/saut/gravité/dash/step depuis `MovementConfig` ; mesure les chutes → `FallDamageRule`. |
| `PlayerLook` | Yaw corps / pitch caméra depuis l'action Look. |
| `PlayerHealth` | PV max via `StatFormulas.MaxHealth`, régén, second wind (`invulnSeconds`, `usesPerRun`), mort → événement. |
| `PlayerInputBridge` | Lit les actions Input System, alimente Motor/Look/Weapon. |
| `WeaponController` | Arme courante (`WeaponData`), tir raycast depuis la caméra, spread `recoil` léger, pellets shotgun, reload, munitions ; tir auto via `AutoFireDecider`. |
| `EnemyHealth` | PV = `WaveRules.EnemyHpForWave × hpMult`, `TakeHit(point, dmgMult)` → `HitZoneResolver`, mort → événement (kill, argent). |
| `EnemyController` | NavMeshAgent + `EnemyBrain` : idle/chase/attack ; ranged = raycast rafales (`burst`, `burstGap`, `accuracy`, dégâts `baseStats.damage × damageMult`, overrides sniper) ; melee = coup au contact (`meleeRange/Damage/Cooldown`). |
| `EnemySpawner` | Instancie un ennemi depuis `EnemyData` (modèle résolu ou primitive), applique scale, câble Health/Controller. |
| `WaveManager` | Boucle : intermission (`WaveConfig.intermissionSeconds`) → spawn du `WaveSpawnPlan` → attente vague vide → vague suivante. Événements pour le HUD. |
| `HudController` | uGUI : barre de vie, munitions (chargeur + icône reload), n° de vague + ennemis restants, écran de mort avec restart. |
| `HudRadar` | Blips positionnés par angle/distance relatifs au joueur, couleur = `radarColor`. |
| `TouchControlsRoot` | Canvas uGUI : `OnScreenStick` gauche (move), zone droite = look (drag), boutons saut/dash/reload. Actif seulement si tactile (ou forçable pour test). |

### Flux de données

`GameDatabase.asset` (déjà généré) → `GameContext` → chaque composant lit **uniquement** ses configs (`MovementConfig`, `WeaponData`, `EnemyData`, `EnemyCombatConfig`, `WaveConfig`, `EconomyConfig`). Aucune valeur d'équilibrage en dur dans le code runtime (hors défauts des trous de spec §2).

### Scène

`Assets/_Game/Scenes/Slice.unity` : arène greybox ~40×40 (sol + murs + quelques caisses/couverts en primitives URP), lumière directionnelle, NavMesh bakée, 4+ spawn points ennemis en périphérie, spawn joueur au centre, `GameContext`, `WaveManager`, HUD. Les vraies maps arrivent en phase 3 (Blender de Nathan) — cette arène est jetable.

### Input (actions)

Map `Gameplay` : `Move` (Vector2), `Look` (Vector2 delta), `Jump`, `Dash`, `Reload`, `Fire` (bouton manuel optionnel, non affiché par défaut). Schemes : `KeyboardMouse` (WASD/ZQSD auto via layout, souris, Espace, Ctrl gauche = dash, R = reload, clic = fire manuel debug) et `Touch` (sticks/boutons on-screen). Course : magnitude du stick (D11) / Shift.

## 4. Gestion d'erreurs

- `GameContext` sans `GameDatabase` assignée → erreur console explicite + désactivation propre (pas de spam de NRE).
- `PlaceholderLibrary` absente/vide → primitives générées (D9), jamais d'exception.
- `EnemyData.weapon` null (dette connue de l'import) → sans effet : le tir ennemi utilise `baseStats`, l'arme n'est que cosmétique.
- Spawn point sans NavMesh à proximité → warning + skip du spawn (la vague reste finissable : le compteur ne compte que les ennemis réellement spawnés).
- Joueur mort → écran de mort + restart de la partie (pas de respawn en cours de vague dans la slice).

## 5. Stratégie de test

- **EditMode (l'essentiel)** : toutes les classes pures du §3 contre les valeurs de référence des JSON — composition des vagues (table complète, cas limites w=1..12), hit zones (frontières 1.5/0.6, scale ≠ 1, armure), stamina (drain/regen/minToRun), fall damage (formule exacte du JSON), dash (cooldown), auto-fire (portée/cooldown), FSM (transitions detect/lose/loseTime, overrides sniper).
- **PlayMode (smoke, 2-3 tests)** : la scène Slice se charge et `GameContext` s'initialise ; un ennemi spawné meurt sous dégâts suffisants et émet l'événement kill ; le joueur subit des dégâts de chute au-delà de `safeHeight`.
- **Vérification à chaque tâche** : compilation via `refresh_unity` + `read_console` (MCP), tests via `run_tests`/`get_test_job` (MCP). Vérifications visuelles/manuelles accumulées dans la liste finale pour Nathan (règle CLAUDE.md global).

## 6. Hors périmètre (différé, ne pas coder)

Comportements spéciaux des types d'ennemis (D5) · VIP doré · power-ups en partie · consommables/grenades/mêlée joueur · économie en partie (argent compté mais pas de boutique) · XP/combo/killstreaks · sauvegarde · audio · vraies maps · changement d'arme/loadout · ADS sniper · difficulté sélectionnable · tout le réseau.

## 7. Critères d'acceptation du jalon

1. Play Mode éditeur : je me déplace (marche/course/stamina), saute, dash, prends des dégâts de chute — valeurs = `MovementConfig`.
2. Le réticule sur un ennemi déclenche le tir auto ; headshot > torse > jambes ; le chargeur se vide et se recharge — valeurs = `WeaponData` + `hitZones`.
3. Les vagues s'enchaînent avec la composition exacte du web (shooter/rusher/tank… selon la vague), HP ennemis = formule `EnemyHpForWave`, boss vague 5.
4. Les ennemis ranged me poursuivent et me tirent dessus en rafales ; les melee me chargent ; je peux mourir (et second wind fonctionne une fois).
5. HUD lisible : vie, munitions, vague/restants, radar avec couleurs par type.
6. Sur écran tactile (ou simulateur) : joysticks virtuels fonctionnels, mêmes actions.
7. 100 % des tests EditMode/PlayMode verts ; aucune erreur console en Play Mode.
