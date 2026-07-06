# CLAUDE.md — projet Unity (FPS mobile)

> **Pour toute session Claude qui démarre : lis ce fichier en entier, puis les 3 fichiers pointés en section « Par où reprendre ». Ils te disent exactement où on en est et quoi faire ensuite.**

## C'est quoi ce projet (en simple)

On porte un jeu de tir web (fait en Three.js/JavaScript, presque fini) vers **Unity en C#**, pour une **sortie mobile Android + iOS**. Le jeu : du PvP multijoueur **et** de la coop contre des vagues d'ennemis. Niveau visuel visé : « Guns of Boom ».

Le jeu web sert de **spécification** (les valeurs d'équilibrage, les formules, l'IA, l'économie). On ne le retraduit pas ligne à ligne : on a extrait toutes ses données de gameplay dans des fichiers JSON (`unity-spec/`), et on reconstruit proprement côté Unity à partir de là.

**Règle importante :** le jeu web n'est PAS dans ce dépôt et on n'y touche pas depuis ici. Ici c'est **uniquement** le projet Unity.

## Où on en est (résumé plain-language)

Le plan de migration a 6 grandes phases (voir `docs/migration-unity.md`).

- ✅ **Étape 3 de `unity-spec/NEXT-STEPS.md` TERMINÉE (2026-07-06) et mergée dans `main`** : le pipeline d'import est fini. Le menu **`Tools/Game/Import Spec from JSON`** lit `unity-spec/*.json` et génère toutes les « fiches de données » Unity (91 assets : armes, ennemis, persos, power-ups, items, configs) + un `GameDatabase.asset` qui référence tout. 10 tâches en TDD, 31/31 tests verts, revue finale « ready to merge ».
- ⬅️ **PROCHAINE ÉTAPE = étape 4** (vertical slice solo jouable) : contrôleur du joueur, tir, ennemis, vagues, HUD. **C'est là que le jeu devient enfin visible et jouable à l'écran** (jusqu'ici tout était de la plomberie de données invisible). Plus grosse étape → refaire un brainstorm dédié avant de coder.

⚠️ **Un seul TODO manuel laissé de l'étape 3 :** remplir le `PlaceholderLibrary` dans l'Inspector Unity (voir plus bas). Rien ne le presse tant qu'on n'affiche pas les objets en jeu.

## Par où reprendre (les fichiers à lire, dans l'ordre)

1. **`unity-spec/NEXT-STEPS.md`** — le suivi d'avancement global de la migration. LE fichier de référence pour savoir quelle est la prochaine grande action (aujourd'hui : l'étape 4).
2. **`docs/ROADMAP.md`** — la vue d'ensemble simple, une ligne par étape, pour situer où on en est d'un coup d'œil.
3. **`.superpowers/sdd/progress.md`** — l'archive détaillée du pipeline d'import (étape 3, terminée) : chaque tâche et son commit. Utile seulement pour comprendre ce qui a déjà été fait, pas pour la suite.
4. **`docs/superpowers/plans/…-spec-import-scriptableobjects.md`** + `…/specs/…-design.md` — le plan et la spec de l'étape 3 (terminée), pour référence.

Pour comprendre le contenu du jeu (les données) : `unity-spec/README.md` explique comment sont structurés les 7 fichiers JSON (`weapons.json`, `enemies.json`, `player.json`, `economy.json`, `items.json`, `powerups.json`, `waves.json`).

## Tous les fichiers `.md` du projet, à quoi ils servent

| Fichier | À quoi il sert |
|---|---|
| `CLAUDE.md` (ce fichier) | Point d'entrée : contexte général du projet, décisions actées, comment vérifier le code. |
| `docs/ROADMAP.md` | Vue simplifiée, une ligne par étape, de tout le projet (7 grandes phases) + détail de la tâche en cours. À lire pour une vue d'ensemble rapide. |
| `docs/migration-unity.md` | Le plan complet et détaillé des 6 phases de migration (web → Unity). Référence complète, plus détaillée que la roadmap. |
| `unity-spec/README.md` | Explique la structure des 7 fichiers JSON de spec gameplay (armes, ennemis, persos, économie...). |
| `unity-spec/NEXT-STEPS.md` | Suivi d'avancement global de la migration : où on en est et la prochaine action à faire. Le fichier à relire à chaque reprise. |
| `docs/superpowers/specs/2026-07-06-spec-import-scriptableobjects-design.md` | La spec de design de la tâche en cours (pipeline d'import JSON → ScriptableObjects) : ce qu'on construit et pourquoi. |
| `docs/superpowers/plans/2026-07-06-spec-import-scriptableobjects.md` | Le plan d'implémentation détaillé (10 tâches) de ce pipeline, écrit pour être exécuté en TDD étape par étape. |
| `.superpowers/sdd/progress.md` | Journal d'avancement de la tâche en cours : quelles tâches du plan sont terminées (avec leur commit) et où reprendre. |
| `.superpowers/sdd/task-N-brief.md` | Pour chaque tâche du plan : les instructions données à l'agent qui l'exécute (extrait du plan). |
| `.superpowers/sdd/task-N-report.md` | Pour chaque tâche du plan : le rapport de ce qui a été fait une fois la tâche terminée. |

## Dernier travail terminé : pipeline d'import JSON → ScriptableObjects (mergé dans `main`)

- **Statut :** ✅ terminé et mergé dans `main` (la branche `feat/spec-import-scriptableobjects` a été fusionnée puis supprimée). Journal détaillé des 10 tâches + commits : `.superpowers/sdd/progress.md`.
- **Ce que ça produit :** des fichiers `.asset` dans `Assets/_Game/Data/` (données pures : dégâts, PV, prix…), déjà générés et commités. Point d'entrée runtime : `Assets/_Game/Data/GameDatabase.asset`.
- **Ré-générer après un changement de JSON :** menu Unity **Tools → Game → Import Spec from JSON** (idempotent, ne touche jamais aux assets d'art assignés à la main).
- **Dette technique connue (non bloquante) :** `docs/superpowers/tech-debt-import-pipeline.md`.

### C'est quoi le `PlaceholderLibrary` (le TODO manuel)

Les fiches de données (`WeaponData`, `EnemyData`, `CharacterData`…) ne contiennent que des **chiffres et du texte** — pas de modèle 3D ni d'icône (ces assets d'art arrivent plus tard, au fur et à mesure). Pour que le jeu ne plante jamais quand un objet n'a pas encore son modèle, la règle est : **la logique n'exige jamais un asset**. Le `PlaceholderLibrary` est un ScriptableObject unique qui contient les **modèles/icônes de secours par défaut** (un cube générique par catégorie d'arme, un ennemi générique, etc.). Quand un `WeaponData` n'a pas de modèle assigné, le code affiche le placeholder à la place.

**Le TODO manuel :** créer l'asset (clic droit dans `Assets/_Game/Data/` → Create → Game → Config → Placeholder Library) et y glisser quelques prefabs/sprites de secours dans l'Inspector. Ça ne se génère pas automatiquement — c'est un choix d'art. Tant que le runtime n'affiche rien à l'écran (avant l'étape 4), ce n'est pas urgent.

## Comment on vérifie le code (important)

L'Unity Editor 6 est ouvert sur ce projet avec un **pont MCP for Unity** connecté (transport HTTP). On ne peut PAS compiler/tester le C# en ligne de commande — tout passe par les outils `mcp__UnityMCP__*` :
- Après avoir écrit un `.cs` : `refresh_unity(compile="request", wait_for_ready=true)` puis `read_console(types=["error"])` pour vérifier qu'il n'y a pas d'erreur de compilation.
- Tests : `run_tests(mode="EditMode", ...)` → renvoie un `job_id`, puis `get_test_job(job_id, wait_timeout=60)` jusqu'à complétion.
- **Prérequis : l'Unity Editor doit être ouvert sur ce projet.** Sinon, demander à Nathan de l'ouvrir avant d'utiliser les tools MCP.

## Prochaine grande étape après le pipeline d'import (étape 4)

Vertical slice solo jouable (détail dans `docs/migration-unity.md`, Phase 2) : contrôleur joueur (`CharacterController`), input tactile (joysticks virtuels), tir raycast avec les `WeaponData`, un ennemi (NavMesh + FSM) avec les `EnemyData`, les vagues, un HUD minimal. C'est là que le jeu devient visible et jouable. Prévoir un brainstorm dédié avant de coder cette étape.

## Décisions déjà actées (ne pas re-débattre)

- Unity 6 + C#, pipeline **URP** (mobile). Pas Built-in, pas HDRP, pas Unreal, pas de moteur maison.
- Android + iOS. Le **solo complet sort en premier** (v1 publiable), le réseau ensuite (coop avant PvP).
- Multi pressenti : **Photon Fusion** (rien à coder avant la phase 5).
- Les maps viennent des `.blend` de Nathan, pas des JSON ni des GLB web.
- Convention de code : identifiants en **anglais**, contenu affiché (noms, descriptions) en **français** tel qu'extrait des JSON.
