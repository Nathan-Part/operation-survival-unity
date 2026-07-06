# CLAUDE.md — projet Unity (FPS mobile)

> **Pour toute session Claude qui démarre : lis ce fichier en entier, puis les 3 fichiers pointés en section « Par où reprendre ». Ils te disent exactement où on en est et quoi faire ensuite.**

## C'est quoi ce projet (en simple)

On porte un jeu de tir web (fait en Three.js/JavaScript, presque fini) vers **Unity en C#**, pour une **sortie mobile Android + iOS**. Le jeu : du PvP multijoueur **et** de la coop contre des vagues d'ennemis. Niveau visuel visé : « Guns of Boom ».

Le jeu web sert de **spécification** (les valeurs d'équilibrage, les formules, l'IA, l'économie). On ne le retraduit pas ligne à ligne : on a extrait toutes ses données de gameplay dans des fichiers JSON (`unity-spec/`), et on reconstruit proprement côté Unity à partir de là.

**Règle importante :** le jeu web n'est PAS dans ce dépôt et on n'y touche pas depuis ici. Ici c'est **uniquement** le projet Unity.

## Où on en est (résumé plain-language)

Le plan de migration a 6 grandes phases (voir `docs/migration-unity.md`). On est au **tout début de la partie code**, précisément à l'**étape 3 de `unity-spec/NEXT-STEPS.md`** : écrire le script qui lit les JSON de `unity-spec/` et génère automatiquement les « fiches de données » Unity (ScriptableObjects : armes, ennemis, persos, etc.).

⚠️ **À ce stade il n'y a encore RIEN de visuel / jouable.** On construit la plomberie de données invisible. Le premier truc jouable à l'écran arrivera à l'**étape 4** (contrôleur du joueur, tir, ennemis, vagues).

## Par où reprendre (les fichiers à lire, dans l'ordre)

1. **`unity-spec/NEXT-STEPS.md`** — le suivi d'avancement global de la migration. LE fichier de référence pour savoir quelle est la prochaine grande action.
2. **`.superpowers/sdd/progress.md`** — le journal détaillé de la tâche en cours (le pipeline d'import). Il liste les tâches déjà terminées (avec leur commit) et donc où reprendre. **Les tâches marquées « complete » sont finies — ne pas les refaire.**
3. **`docs/superpowers/plans/2026-07-06-spec-import-scriptableobjects.md`** — le plan détaillé (10 tâches) du pipeline d'import en cours. La spec de design associée est `docs/superpowers/specs/2026-07-06-spec-import-scriptableobjects-design.md`.

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

## Travail en cours : pipeline d'import JSON → ScriptableObjects

- **Branche git :** `feat/spec-import-scriptableobjects`
- **Plan :** 10 tâches, exécutées en TDD (test qui échoue → code → test qui passe → commit), une par une, chacune avec sa revue.
- **Avancement exact :** voir `.superpowers/sdd/progress.md` (mis à jour à chaque tâche terminée).
- **Ce que ça produit :** des fichiers `.asset` dans `Assets/_Game/Data/` (données pures : dégâts, PV, prix…). Aucune dépendance dure à un asset d'art : les modèles 3D / icônes s'assignent à la main plus tard, un `PlaceholderLibrary` sert de secours.

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
