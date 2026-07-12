# Mode en ligne (coop) — Plan d'implémentation

> **Statut : EXÉCUTÉ en autonomie le 2026-07-11/12** (session Claude « fais le mode en ligne en full autonomie »). Ce document décrit ce qui a été livré, tâche par tâche. Spec associée : `docs/superpowers/specs/2026-07-11-online-coop-mode-design.md`.

**But :** poser tout le **cœur logique du mode en ligne** (coop contre vagues, hôte autoritaire), transposable sur n'importe quel transport, **pur et testé**, sans dépendre de Photon ni d'un éditeur Unity ouvert. Premier jalon de la **Phase 6** de la roadmap.

**Architecture :** logique pure (Game.Net, engine-free, testée EditMode) séparée d'une glue MonoBehaviour fine (Game.Runtime) et d'un adaptateur transport isolé (Game.Net.Photon, inerte par contrainte de define). Cf. spec §3.

**Contraintes globales :**
- Identifiants/code en **anglais**, textes affichés en **français** (aucun texte affiché ici, c'est de la logique).
- Aucune valeur d'équilibrage en dur : intermission/argent/difficulté **lus** depuis `GameDatabase` (`CoopConfigFactory`) ; les tunables coop absents du web sont centralisés dans `CoopSessionConfig`, marqués `// TODO(coop-balance)`.
- **Le build actuel ne doit jamais casser** : l'intégration Photon est derrière `defineConstraints: ["FUSION2"]` (assembly non compilé sans Fusion) + `#if FUSION2`.
- **Vérification hors éditeur** : la partie engine-free de `Game.Net` est compilée et **exécutée** avec `mcs`/`mono` (harness de scénarios) — 41 assertions vertes. En éditeur (prochaine session MCP) : `refresh_unity` + `read_console` = 0 erreur, `run_tests(EditMode)` = tests `Game.Net.Tests` verts, aucun test existant cassé.

## Vue d'ensemble des tâches (toutes livrées)

1. **Assembly `Game.Net`** (`Assets/_Game/Scripts/Net/`, réf. `Game.Data`) + primitives engine-free : `NetVector3`, `PlayerRef`, `NetworkRole`/`SessionPhase`/`EnemyLifecycle`/`ButtonFlags`.
2. **Messages réseau** (`NetMessages.cs`) : `PlayerInputCommand`, `DamageClaim`, `PlayerNetState`, `EnemyNetState`, `WorldSnapshot`.
3. **Config & scaling** : `CoopSessionConfig` (tunables), `CoopScaling` (PV/nombre par nb de joueurs), `CoopConfigFactory` (lit `GameDatabase` — seul fichier lié à Game.Data).
4. **États runtime** : `CoopPlayerState`, `EnemyRuntimeState`.
5. **Directeur de vagues** `CoopWaveDirector` : intermission → spawn (SpawnRequest, netId alloués) → combat → clear → vague+1 ; réutilise `WaveRules` + composition injectée + `CoopScaling`.
6. **Validation** `DamageValidator` : rejette dégât non positif / attaquant invalide / ennemi inconnu ou mort / hors portée / cadence excessive.
7. **Orchestrateur** `CoopSession` : registre joueurs (join/leave, max, dédup), argent partagé, application/validation des dégâts, boucle **down → relève → respawn → wipe/GameOver**, `Tick(dt)`, `BuildSnapshot()`.
8. **Transport** : `INetTransport` (frontière) + `LoopbackNetwork`/`LoopbackTransport` (mémoire, mono-process, latence simulable).
9. **Tests EditMode** (`Assets/_Game/Tests/Net/`, `Game.Net.Tests.asmdef`) : `NetVector3`, `CoopScaling`, `CoopWaveDirector`, `DamageValidator`, `CoopSession`, `LoopbackTransport` (≈35 tests).
10. **Glue runtime** `CoopRunner` (`Game.Runtime/Runtime/Net/`, ajout de la réf. `Game.Net`) : relie transport ↔ session, avance la simulation, diffuse les snapshots ; ignorant du rendu (événements pour un futur binder de scène).
11. **Adaptateur Photon** (`Game.Net.Photon.asmdef` + `FusionTransport.cs` + `README.md`) : squelette `INetTransport` sur Fusion 2, **inerte** tant que `FUSION2` n'est pas défini.
12. **Metas Unity** générés pour tous les nouveaux assets (GUID stables) + **docs** (spec, ce plan, ROADMAP, CLAUDE).

## Ce qui reste (Phase 6, prochaines sessions — en éditeur)

- **Vérifier en éditeur** : compilation Unity + `run_tests(EditMode)` verts (les tests pure logique devraient passer tels quels).
- **Binder de scène coop** : instancier les visuels ennemis sur `CoopRunner.EnemySpawnRequested` (réutiliser `EnemySpawner`), pousser positions/kills vers la session, appliquer les snapshots côté client (proxies interpolés). Réconcilier avec `EnemyHealth`/`EnemyController` existants (autorité hôte).
- **Installer Photon Fusion 2** + App ID, définir `FUSION2`, compléter `FusionTransport` (RPC + callbacks). Voir `Assets/_Game/Scripts/Net/Photon/README.md`.
- **Régler l'équilibrage coop** (`// TODO(coop-balance)`) au playtest.
- **PvP** (seconde moitié de la Phase 6) : hors périmètre de cette étape.
