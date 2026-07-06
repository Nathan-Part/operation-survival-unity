# CLAUDE.md — projet Unity (FPS mobile)

> **Pour toute session Claude qui démarre : lis ce fichier en entier, puis `docs/ROADMAP.md`. Ces deux fichiers te disent exactement où on en est et quoi faire ensuite.**

## C'est quoi ce projet (en simple)

On porte un jeu de tir web (fait en Three.js/JavaScript, presque fini) vers **Unity en C#**, pour une **sortie mobile Android + iOS**. Le jeu : du PvP multijoueur **et** de la coop contre des vagues d'ennemis. Niveau visuel visé : « Guns of Boom ».

Le jeu web sert de **spécification** (les valeurs d'équilibrage, les formules, l'IA, l'économie). On ne le retraduit pas ligne à ligne : on a extrait toutes ses données de gameplay dans des fichiers JSON (`unity-spec/`), et on reconstruit proprement côté Unity à partir de là.

**Règle importante :** le jeu web n'est PAS dans ce dépôt et on n'y touche pas depuis ici. Ici c'est **uniquement** le projet Unity.

**📁 Référence : le jeu web est sur cette machine, à `C:\wamp64\www\projet\games`** (lecture seule — ne JAMAIS le modifier depuis ce projet). À consulter quand on a besoin de vérifier un comportement/une valeur non extraite dans les JSON : code source `src/game/main.js`, modèles 3D `perso/` (persos GLB), `artefact/` (armes/objets), `animation/` (animations), `pets/`, `decor/`. Des GLB en sont déjà copiés ici dans `Assets/_Game/Art/` (3 persos + 1 ennemi générique) et branchés via le menu **Tools → Game → Assign Imported Art** (idempotent, n'écrase jamais une assignation manuelle) — copier d'autres assets du web au fur et à mesure des besoins.

## Où on en est (résumé)

Le plan complet (7 phases, 0 à 6, du gel du jeu web jusqu'à la sortie sur les stores) est dans **`docs/ROADMAP.md`** — c'est le fichier de suivi unique, il contient aussi « comment tester le jeu » et les TODO ouverts.

- ✅ **Phase 1 (fondations + import des données)** : mergée dans `main`. Menu **`Tools/Game/Import Spec from JSON`** → 91 ScriptableObjects + `GameDatabase.asset`.
- ✅ **Phase 2 (slice solo jouable, version minimale)** : terminée le 2026-07-06 sur la branche **`feat/vertical-slice-solo`** (pas encore mergée — en attente du test de Nathan). **Le jeu est jouable** : scène `Assets/_Game/Scenes/Slice.unity`, vue FPS, tir automatique, vagues fidèles au web, HUD, joysticks virtuels. 83 tests EditMode + 2 PlayMode verts.
- ⬅️ **Prochaine étape :** Nathan teste et on merge, puis Phase 3 (maps + visuel) OU compléter la Phase 2 (comportements spéciaux des ennemis). Voir la roadmap.

## Tous les fichiers `.md` du projet, à quoi ils servent

| Fichier | À quoi il sert |
|---|---|
| `CLAUDE.md` (ce fichier) | Point d'entrée des sessions : contexte général, conventions, comment vérifier le code. |
| **`docs/ROADMAP.md`** | **LE fichier de suivi unique** : les 7 phases du début à la fin, où on en est, comment tester le jeu, TODO ouverts, règles, décisions actées, historique. |
| `unity-spec/README.md` | Explique la structure des 7 fichiers JSON de spec gameplay (armes, ennemis, persos, économie...). |
| `docs/superpowers/specs/…` et `docs/superpowers/plans/…` | Archives : les specs de design et plans d'implémentation des chantiers terminés (import ScriptableObjects, vertical slice). Lecture seulement si besoin de comprendre un choix passé. |
| `docs/superpowers/tech-debt-import-pipeline.md` | Dette technique connue (non bloquante) du pipeline d'import. |
| `.superpowers/sdd/…` | Archives du processus d'exécution du pipeline d'import (journal, briefs, rapports par tâche). |

## Les deux menus Unity à connaître

- **Tools → Game → Import Spec from JSON** : régénère les fiches de données depuis `unity-spec/*.json` (idempotent, ne touche jamais aux assets d'art assignés à la main).
- **Tools → Game → Build Slice Scene** : régénère entièrement la scène de test `Assets/_Game/Scenes/Slice.unity` (arène greybox + NavMesh + joueur + vagues câblés).

## C'est quoi le `PlaceholderLibrary` (TODO manuel ouvert)

Les fiches de données (`WeaponData`, `EnemyData`…) ne contiennent que des **chiffres et du texte** — pas de modèle 3D ni d'icône. La règle : **la logique n'exige jamais un asset**. Le `PlaceholderLibrary` est un ScriptableObject où Nathan glissera des modèles/icônes de secours dans l'Inspector (Create → Game → Config → Placeholder Library dans `Assets/_Game/Data/`). Tant qu'il n'existe pas, le runtime génère des **capsules colorées** en secours — rien ne plante, c'est juste moche.

## Comment on vérifie le code (important)

L'Unity Editor 6 est ouvert sur ce projet avec un **pont MCP for Unity** connecté (transport HTTP). On ne peut PAS compiler/tester le C# en ligne de commande — tout passe par les outils `mcp__UnityMCP__*` :
- Après avoir écrit un `.cs` : `refresh_unity(compile="request", wait_for_ready=true)` puis `read_console(types=["error"])` pour vérifier qu'il n'y a pas d'erreur de compilation.
- Tests : `run_tests(mode="EditMode", ...)` → renvoie un `job_id`, puis `get_test_job(job_id, wait_timeout=60)` jusqu'à complétion. PlayMode : `run_tests(mode="PlayMode", assembly_names="Game.Runtime.PlayTests", init_timeout=120000)`.
- **Prérequis : l'Unity Editor doit être ouvert sur ce projet.** Sinon, demander à Nathan de l'ouvrir avant d'utiliser les tools MCP.

## Décisions déjà actées (ne pas re-débattre)

- Unity 6 + C#, pipeline **URP** (mobile). Pas Built-in, pas HDRP, pas Unreal, pas de moteur maison.
- Android + iOS. Le **solo complet sort en premier** (v1 publiable), le réseau ensuite (coop avant PvP).
- Multi pressenti : **Photon Fusion** (rien à coder avant la phase 5).
- **Tir automatique** façon Guns of Boom (un bouton manuel optionnel pourra s'ajouter plus tard).
- Les maps viennent des `.blend` de Nathan, pas des JSON ni des GLB web.
- Convention de code : identifiants en **anglais**, contenu affiché (noms, descriptions) en **français** tel qu'extrait des JSON.
- Architecture du code runtime : logique pure (classes C# testées en EditMode) séparée des MonoBehaviours fins ; aucune valeur d'équilibrage en dur (tout vient de `GameDatabase.asset`).
