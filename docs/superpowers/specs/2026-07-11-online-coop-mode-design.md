# Spec de design — Phase 6 (début) : le mode en ligne, cœur coop réseau

> **Statut :** validée (brainstorm autonome du 2026-07-11, mandaté par Nathan — « fais le mode en ligne en full autonomie, brainstorm → spec → plan → exécution »). Questions tranchées ci-dessous avec l'option recommandée.
> **Objectif de l'étape :** poser **tout le cœur logique du mode en ligne** — une simulation coop **autoritaire côté hôte**, transposable sur n'importe quel transport réseau, entièrement pure et testée en EditMode — sans dépendre d'un package payant (Photon) ni d'un éditeur Unity ouvert. C'est le premier jalon de la **Phase 6** de `docs/ROADMAP.md` (multijoueur : coop d'abord, PvP ensuite).

---

## 0. Contexte et contrainte forte

La roadmap acte : **Photon Fusion** pour le netcode, **coop contre vagues d'abord** (2–4 joueurs, l'hôte fait autorité sur les ennemis), PvP ensuite, dégâts validés côté serveur. Deux contraintes de réalité guident tout le design :

1. **Photon Fusion n'est pas installé** (pas de licence / App ID sans intervention de Nathan) et est un package externe. On ne peut donc pas coder « dans » Fusion tout de suite.
2. **Aucun éditeur Unity n'est ouvert** dans cet environnement (exécution distante) : impossible de compiler/tester le C# via le pont MCP. Il faut donc du code dont la correction ne dépende pas de l'éditeur — c'est-à-dire de la **logique pure**, vérifiable au raisonnement et en EditMode plus tard.

Ces deux contraintes convergent vers **exactement** l'architecture que le projet prône déjà (« logique pure séparée des MonoBehaviours fins ») : on écrit un **cœur netcode indépendant du transport**, engine-free, déterministe, testé ; et le **branchement Photon devient un adaptateur fin**, isolé derrière une interface et une compilation conditionnelle, que Nathan activera quand il installera Fusion.

C'est une bonne décision d'ingénierie en soi (pas un pis-aller) : le gameplay ne se couple jamais à un SDK réseau, le netcode reste testable hors moteur, et on pourrait même changer de solution (Netcode for GameObjects, Mirror…) sans toucher la simulation.

---

## 1. Décisions de design (questions tranchées)

| # | Question | Décision | Justification |
|---|---|---|---|
| D1 | Modèle d'autorité | **Hôte autoritaire** (host-authoritative / listen-server). L'hôte simule les vagues, les ennemis, l'économie partagée, et **valide tous les dégâts**. Les clients envoient leur transform + des *réclamations de dégât* ; ils n'appliquent jamais un état de jeu de leur propre chef. | Directement la roadmap (« l'hôte fait autorité sur les ennemis », « dégâts validés côté serveur »). Modèle standard coop PvE, simple, anti-triche suffisant pour du coop. |
| D2 | Netcode concret | **Photon Fusion** derrière une interface `INetTransport`. Le cœur ne référence **jamais** Fusion. Un adaptateur `FusionTransport` vit dans un assembly **compilé sous contrainte de define** (`FUSION2`) → inerte tant que Fusion n'est pas installé, donc **le build actuel ne casse jamais**. | Respecte la décision actée Fusion sans bloquer sur la licence/App ID, et sans figer le projet à un seul SDK. |
| D3 | Découplage transport | Interface `INetTransport` minimale : rôle (Host/Client), joueur local, événements *join/leave/commande reçue/snapshot reçu*, envoi commande (client→hôte) et diffusion snapshot (hôte→clients). | Frontière étroite et testable ; tout le reste est pur. |
| D4 | Transport de test | **`LoopbackTransport`** en mémoire, mono-process, déterministe (livraison immédiate ou retardée d'un nombre de ticks paramétrable). Sert aux tests EditMode **et** à faire tourner une session « hôte seul + bots » sans réseau. | On peut valider toute la logique coop sans téléphone ni Photon ni éditeur. Le solo actuel devient un cas particulier (hôte, 1 joueur). |
| D5 | Périmètre de l'étape | **Coop contre vagues uniquement**, et **uniquement la couche logique + le seam transport**. Pas de PvP, pas de matchmaking/lobby en ligne réel, pas d'UI réseau, pas de sauvegarde cloud. Le rendu/interpolation côté client est un `MonoBehaviour` fin, non testable ici, écrit défensivement. | La roadmap fait la coop avant le PvP ; et sans éditeur on maximise la valeur en livrant le cœur testable, pas des MonoBehaviours invérifiables. |
| D6 | Réutilisation du solo | La simulation coop **réutilise la spec de vagues existante** : `WaveRules` (formules, source de vérité) directement, et la **composition** `WaveComposition.ForWave` **injectée en délégué** (`Func<int,string[]>`) pour éviter un cycle d'assembly `Game.Net ↔ Game.Runtime`. | Zéro duplication d'équilibrage. `WaveRules` est déjà engine-free (`Game.Data.Rules`). |
| D7 | Scaling par nb de joueurs | **Rééquilibrage coop** paramétré (le TTK web est pensé solo/PvE contre bots) : PV ennemis et nombre d'ennemis montent avec le nombre de joueurs, via des formules pures `CoopScaling`. Valeurs par défaut marquées `// TODO(coop-balance)` (le web n'a pas de coop → pas de JSON de référence ; à régler au playtest). | La roadmap exige un « rééquilibrage obligatoire » en réseau. On l'isole dans une formule testable et retunable, sans valeur en dur dispersée. |
| D8 | Mort / relève | Modèle coop classique : à 0 PV → **à terre** (*downed*) avec un timer de saignement ; un coéquipier proche **relève** (progression de revive) ; si le timer expire → **mort** puis **respawn** après délai tant que la partie continue ; si **tous** les joueurs sont à terre/morts en même temps → **wipe → GameOver**. | Boucle coop attendue, entièrement déterministe et testable. Le « second wind » solo reste une propriété du perso ; ici c'est la couche session qui arbitre. |
| D9 | Validation des dégâts | `DamageValidator` pur : rejette une réclamation si l'ennemi n'existe pas/est déjà mort, si le tireur n'est pas un joueur vivant, si la cadence dépasse un plafond, si le point d'impact est hors d'une portée max plausible. | Anti-triche minimal **testable**. La validation vit côté hôte, jamais côté client. |
| D10 | Déterminisme / temps | La simulation avance par `Tick(float dt)` explicite (pas de `Time.time`, pas de coroutine). Positions via `NetVector3` maison (pas `UnityEngine.Vector3`). | Reproductible en test, indépendant du moteur, et prêt pour une éventuelle simulation à pas fixe façon Fusion. |
| D11 | Identité joueur | `PlayerRef` (struct, `int Id`, 0 = invalide) + `NetworkRole` (Offline/Host/Client). L'hôte porte aussi un `PlayerRef` (c'est un joueur comme les autres). | Léger, comparable, sérialisable. |
| D12 | Sérialisation | **Hors périmètre concret** : les types réseau (`PlayerInputCommand`, `DamageClaim`, `WorldSnapshot`) sont des `struct`/classes simples ; leur *encodage* sur le fil est la responsabilité de l'adaptateur transport (Fusion a son propre système). Le `LoopbackTransport` passe les objets par référence. | On ne réinvente pas un sérialiseur ; on garde la frontière au bon endroit. |
| D13 | Join en cours de partie | **Autorisé** : un joueur qui rejoint une partie déjà lancée entre à l'état vivant au prochain respawn/vague (pas de spectateur pour l'instant). Un départ retire le joueur et ses effets. | Simplicité ; comportement coop courant. |
| D14 | Friendly fire | **Off par défaut** (paramétrable). Les réclamations de dégât joueur→joueur sont ignorées si off. | PvE coop standard ; le flag est prêt pour un mode « hardcore ». |

---

## 2. Trous de spec (valeurs absentes des JSON) — défauts configurables

Le jeu web n'ayant **pas** de coop, ces valeurs n'ont pas de source JSON. Chacune est un champ avec défaut raisonnable, marqué `// TODO(coop-balance)`, centralisé dans `CoopSessionConfig` (retunable au playtest, puis à porter dans un futur `coop.json` si on veut).

| Trou | Défaut retenu | Où |
|---|---|---|
| Nombre max de joueurs | 4 | `CoopSessionConfig.maxPlayers` |
| PV ennemi par joueur supplémentaire | +35 % par joueur au-delà du 1er | `CoopScaling.EnemyHpMult` |
| Ennemis en plus par joueur suppl. | +1 slot par joueur au-delà du 1er (plafonné) | `CoopScaling.EnemyCountBonus` |
| Délai de saignement (downed→mort) | 15 s | `CoopSessionConfig.bleedOutSeconds` |
| Durée de relève (revive) | 3 s de proximité cumulée | `CoopSessionConfig.reviveSeconds` |
| Rayon de relève | 2,5 m | `CoopSessionConfig.reviveRadius` |
| Délai de respawn (après mort, partie en cours) | 5 s | `CoopSessionConfig.respawnSeconds` |
| PV au respawn/relève | 100 % / 50 % du max | `CoopSessionConfig` |
| Cadence max de dégât acceptée (anti-triche) | 20 hits/s par joueur | `DamageValidator` |
| Portée d'impact max plausible | 200 m | `DamageValidator` |
| Friendly fire | off | `CoopSessionConfig.friendlyFire` |

L'intermission entre vagues et les formules de PV/nombre/argent restent **lues depuis les données existantes** (`WaveConfig`, `WaveRules`, `EconomyConfig`) — pas de nouveau nombre en dur pour ça.

---

## 3. Architecture

### Assemblies (nouveau graphe de dépendances, sans cycle)

```
Game.Data  (ScriptableObjects + Game.Data.Rules : WaveRules, formules — engine-free)
   ▲
   │
Game.Net   (NOUVEAU — cœur netcode pur ; référence Game.Data uniquement)
   ▲   ▲
   │   └────────────── Game.Net.Photon  (NOUVEAU — adaptateur Fusion ; defineConstraint FUSION2 → inerte sans Photon)
   │
Game.Runtime  (MonoBehaviours ; référence Game.Data + Game.Net ; porte le CoopRunner fin)

Game.Net.Tests  (EditMode ; référence Game.Net + Game.Data)
```

- **`Game.Net`** (`Assets/_Game/Scripts/Net/`) : **engine-free autant que possible** (System + Game.Data.Rules). Aucune référence à `UnityEngine` dans la simulation → syntaxe vérifiable hors moteur, déterminisme garanti. `noEngineReferences: true` **n'est pas** posé (référence Game.Data qui, elle, tire UnityEngine), mais le code du cœur n'utilise aucun type moteur.
- **`Game.Net.Photon`** (`Assets/_Game/Scripts/Net/Photon/`) : `defineConstraints: ["FUSION2"]`. Tant que le symbole `FUSION2` n'est pas défini (Fusion absent), **l'assembly n'est pas compilé du tout** → aucune référence pendante, aucun risque pour le build. Le `.cs` est en plus gardé `#if FUSION2` (double sécurité). README d'activation fourni.
- **`Game.Runtime`** : on lui **ajoute** `Game.Net` en référence. Il gagne un `CoopRunner` (MonoBehaviour fin) et un `LoopbackCoopBootstrap` (analogue à `SliceBootstrap`).
- **`Game.Net.Tests`** : EditMode, comme `Game.Runtime.Tests`.

### Types du cœur (`Game.Net`)

**Primitives réseau (engine-free)**
- `NetVector3` — struct `{ float x, y, z }` + distance, lerp, opérateurs. Remplace `Vector3` dans la simulation.
- `PlayerRef` — struct `{ int Id }`, égalité, `IsValid`, `None`.
- `NetworkRole` — enum `Offline | Host | Client`.
- `ButtonFlags` — `[Flags]` byte : Jump, Dash, Reload, Fire.

**Messages (POCO, encodage délégué au transport)**
- `PlayerInputCommand` — `{ uint tick; NetVector3 position; float aimYaw, aimPitch; ButtonFlags buttons }` (client→hôte : où je suis, où je vise, ce que j'appuie).
- `DamageClaim` — `{ uint tick; PlayerRef attacker; int enemyNetId; NetVector3 hitPoint; float amount }` (client→hôte : « j'ai touché l'ennemi N »).
- `PlayerNetState`, `EnemyNetState` — vues plates d'un joueur/ennemi pour le snapshot.
- `WorldSnapshot` — `{ int wave; int aliveEnemies; SessionPhase phase; int sharedMoney; PlayerNetState[]; EnemyNetState[] }` (hôte→clients : l'état du monde à afficher).

**Simulation (pure, déterministe, testée)**
- `CoopSessionConfig` — tous les tunables coop (§2) + fabrique `FromDatabase(GameDatabase)` (intermission, difficulté).
- `CoopScaling` — formules pures de scaling par nombre de joueurs (PV, nombre).
- `CoopPlayerState` — état runtime d'un joueur (PV, downed, revive, kills/deaths, transform, timers).
- `EnemyRuntimeState` — état runtime d'un ennemi (netId, typeId, PV, position, cycle Alive/Dying/Dead, cible).
- `CoopWaveDirector` — progression des vagues autoritaire : intermission → spawn (émet des `SpawnRequest`) → combat → clear → vague suivante ; alloue les `netId` ; applique `CoopScaling` + `WaveRules`. `composition` injectée en `Func<int,string[]>`.
- `DamageValidator` — validation pure des `DamageClaim`.
- `CoopSession` — **l'orchestrateur** : registre joueurs (join/leave), argent partagé, `CoopWaveDirector`, dictionnaire d'ennemis, machine à états `SessionPhase (Lobby → Active → GameOver)`, application/validation des dégâts, boucle down/revive/respawn/wipe, `Tick(dt)`, `BuildSnapshot()`.

**Transport (frontière)**
- `INetTransport` — abstraction (D3).
- `LoopbackTransport` — implémentation mémoire pour tests + hôte-seul.

### Types de glue (`Game.Runtime`, MonoBehaviours fins, non testables ici)
- `CoopRunner` — possède l'`INetTransport`, un `CoopSession` **si hôte**, appelle `Tick(dt)` dans `Update`, mappe `netId ↔ EnemyHealth` (réutilise `EnemySpawner`), diffuse les snapshots ; côté client, applique les snapshots à des proxies interpolés. Écrit défensivement, marqué « à vérifier en éditeur ».
- `LoopbackCoopBootstrap` — câble une session hôte-seul mono-process (démo/essai), analogue à `SliceBootstrap`.

### Adaptateur Photon (`Game.Net.Photon`, inerte)
- `FusionTransport : INetTransport` — squelette `#if FUSION2` mappant `INetTransport` sur `NetworkRunner`/RPC Fusion. README d'activation (installer Fusion 2, définir `FUSION2`, App ID).

---

## 4. Ce qui n'est PAS fait dans cette étape (et pourquoi)

- **PvP, matchmaking, lobbies en ligne réels** → Phase 6 seconde moitié (roadmap).
- **Sérialisation binaire / RPC concrets** → responsabilité de l'adaptateur Fusion, quand Fusion sera là.
- **Sauvegarde cloud, anti-triche avancé** → Phase 6/7.
- **Rendu/interpolation client, effets** → MonoBehaviours à valider dans l'éditeur (fournis en version défensive, non testés ici).
- **Réglage fin de l'équilibrage coop** → au playtest (valeurs `// TODO(coop-balance)`).

## 5. Vérification

- **Hors éditeur (ici)** : les fichiers **engine-free** du cœur `Game.Net` sont passés à `mcs` (compilateur C# Mono) avec des *stubs* minimaux de `WaveRules` pour attraper les erreurs de syntaxe. Ce n'est pas une compilation Unity, mais ça élimine les fautes de frappe/typage du cœur.
- **En éditeur (Nathan / prochaine session avec MCP)** : `refresh_unity(compile)` + `read_console(error)` = 0 erreur attendue ; `run_tests(EditMode)` = les tests `Game.Net.Tests` verts + aucun test existant cassé.

**Jalon de l'étape :** une session coop complète (join, vagues scalées, dégâts validés, down/revive/respawn, wipe) tourne **en pur C# testé**, prête à recevoir Photon comme simple adaptateur.
