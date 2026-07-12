# Adaptateur Photon Fusion (activation)

Ce dossier contient le branchement du **mode en ligne** sur **Photon Fusion 2**. Il est
**désactivé par défaut** et n'a **aucun impact** sur le projet tant que Fusion n'est pas installé :
l'assembly `Game.Net.Photon.asmdef` porte la contrainte de define `FUSION2`, donc Unity ne le
compile pas du tout sans ce symbole (et le code de `FusionTransport.cs` est en plus gardé par
`#if FUSION2`).

Tout le **cœur du mode en ligne** (simulation coop autoritaire, vagues, dégâts, down/relève/
respawn, snapshots) vit dans l'assembly **`Game.Net`**, sans aucune dépendance à Fusion, et est
**testé** (`Assets/_Game/Tests/Net/`). Fusion n'est qu'un **transport** derrière l'interface
`INetTransport`. On pourrait le remplacer (Netcode for GameObjects, Mirror…) sans toucher au jeu.

## Activation (à faire par Nathan, une fois)

1. **Installer Photon Fusion 2** (Package Manager → depuis le site Photon, ou l'asset importé).
   Créer une app **Fusion** sur le dashboard Photon et récupérer l'**App ID** (`PhotonAppSettings`).
2. **Définir le symbole `FUSION2`** : `Project Settings → Player → Scripting Define Symbols`,
   ajouter `FUSION2` (Fusion 2 le définit souvent lui-même ; sinon l'ajouter à la main).
   Dès qu'il est défini, `Game.Net.Photon` se compile.
3. **Ajouter la référence Fusion** à `Game.Net.Photon.asmdef` (champ *Assembly Definition
   References*) : les assemblies `Fusion.Runtime` (et éventuellement `Fusion.Common`).
4. **Compléter `FusionTransport.cs`** : les trois RPC (`RpcSendCommand`, `RpcSendDamageClaim`,
   `RpcBroadcastSnapshot`) et l'implémentation complète de `INetworkRunnerCallbacks` (méthodes
   vides pour celles non utilisées). Les points d'accroche et TODO sont déjà en place.
5. **Câbler un `CoopRunner`** (dans `Game.Runtime`, dossier `Runtime/Net/`) sur une scène coop :
   `InitHost(fusionTransport, GameDatabase, WaveComposition.ForWave)` côté hôte,
   `InitClient(fusionTransport)` côté client, puis `StartMatch()`.

## Rappels de conception

- **Hôte autoritaire** : l'hôte simule ennemis + vagues et **valide tous les dégâts**
  (`DamageValidator`). Les clients envoient leur transform (`PlayerInputCommand`) et des
  réclamations de dégât (`DamageClaim`) ; ils n'appliquent jamais l'état de jeu eux-mêmes.
- **Rééquilibrage coop** : PV et nombre d'ennemis montent avec le nombre de joueurs
  (`CoopScaling`, tunables dans `CoopSessionConfig`). Valeurs à régler au playtest
  (`// TODO(coop-balance)`).
- **Snapshot** : `WorldSnapshot` peut être gros ; préférer la réplication d'état native Fusion
  (NetworkArray / propriétés répliquées) à un RPC monolithique par frame.

Voir la spec : `docs/superpowers/specs/2026-07-11-online-coop-mode-design.md`.
