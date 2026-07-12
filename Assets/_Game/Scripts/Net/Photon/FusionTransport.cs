// Adaptateur Photon Fusion 2 pour INetTransport.
//
// ⚠️ INERTE PAR DÉFAUT. Cet assembly (Game.Net.Photon.asmdef) porte la contrainte de define
// "FUSION2" : tant que ce symbole n'est pas défini, Unity NE COMPILE PAS ce fichier — donc le
// projet actuel n'est jamais impacté, même si Fusion n'est pas installé. Le corps est aussi
// gardé par #if FUSION2 (double sécurité). Voir README.md de ce dossier pour l'activation.
//
// Ce fichier est un SQUELETTE à compléter quand Photon Fusion 2 sera installé : il montre où
// brancher NetworkRunner et les RPC sur la frontière INetTransport. Il n'est pas testable ici
// (dépend du SDK Fusion et d'un App ID).
#if FUSION2
using System;
using Fusion;                 // fourni par le package Photon Fusion 2 une fois installé
using Fusion.Sockets;
using UnityEngine;
using Game.Net;

namespace Game.Net.Photon
{
    /// Implémente INetTransport au-dessus d'un Fusion NetworkRunner. La simulation coop
    /// (Game.Net.CoopSession) reste inchangée : elle ne voit que INetTransport.
    ///
    /// Répartition Fusion :
    ///  - L'hôte (StateAuthority) porte la CoopSession et diffuse WorldSnapshot.
    ///  - Les clients envoient PlayerInputCommand / DamageClaim via RPC vers l'hôte.
    ///  - Le snapshot part de l'hôte vers tous via RPC (ou NetworkBehaviour répliqué).
    public class FusionTransport : SimulationBehaviour, INetTransport, INetworkRunnerCallbacks
    {
        NetworkRunner runner;

        public NetworkRole Role =>
            runner == null ? NetworkRole.Offline :
            runner.IsServer ? NetworkRole.Host : NetworkRole.Client;

        public PlayerRef LocalPlayer =>
            runner != null ? new PlayerRef(runner.LocalPlayer.PlayerId + 1) : PlayerRef.None; // +1 : 0 = None

        public bool IsServer => runner != null && runner.IsServer;

        public event Action<PlayerRef> PlayerConnected;
        public event Action<PlayerRef> PlayerDisconnected;
        public event Action<PlayerRef, PlayerInputCommand> CommandReceived;
        public event Action<PlayerRef, DamageClaim> DamageClaimReceived;
        public event Action<WorldSnapshot> SnapshotReceived;

        public void Bind(NetworkRunner networkRunner)
        {
            runner = networkRunner;
            runner.AddCallbacks(this);
        }

        // --- INetTransport : envoi ---

        public void SendCommand(PlayerInputCommand cmd)
        {
            // TODO(fusion) : RPC client -> hôte. Ex : RpcSendCommand(Pack(cmd)).
            // Sur l'hôte (listen-server), appliquer localement sans passer par le réseau.
            if (IsServer) CommandReceived?.Invoke(LocalPlayer, cmd);
            else RpcSendCommand(cmd);
        }

        public void SendDamageClaim(DamageClaim claim)
        {
            // TODO(fusion) : RPC client -> hôte, validé côté hôte par CoopSession.ApplyDamageClaim.
            if (IsServer) DamageClaimReceived?.Invoke(LocalPlayer, claim);
            else RpcSendDamageClaim(claim);
        }

        public void BroadcastSnapshot(WorldSnapshot snapshot)
        {
            // TODO(fusion) : RPC hôte -> tous. Attention à la taille : n'envoyer que le delta
            // ou compresser (WorldSnapshot peut être volumineux ; utiliser les NetworkArray/
            // NetworkedProperties de Fusion pour la réplication d'état plutôt qu'un gros RPC).
            if (!IsServer) return;
            RpcBroadcastSnapshot(snapshot);
        }

        public void Poll() { /* Fusion pompe le réseau tout seul via son NetworkRunner. */ }

        // --- RPC (à implémenter avec les attributs Fusion [Rpc]) ---

        // [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        void RpcSendCommand(PlayerInputCommand cmd /*, RpcInfo info = default */)
        {
            // CommandReceived?.Invoke(new PlayerRef(info.Source.PlayerId + 1), cmd);
            throw new NotImplementedException("Brancher l'attribut [Rpc] Fusion.");
        }

        // [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        void RpcSendDamageClaim(DamageClaim claim /*, RpcInfo info = default */)
        {
            // DamageClaimReceived?.Invoke(new PlayerRef(info.Source.PlayerId + 1), claim);
            throw new NotImplementedException("Brancher l'attribut [Rpc] Fusion.");
        }

        // [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RpcBroadcastSnapshot(WorldSnapshot snapshot)
        {
            // SnapshotReceived?.Invoke(snapshot);
            throw new NotImplementedException("Brancher l'attribut [Rpc] Fusion.");
        }

        // --- Callbacks Fusion (extrait : à compléter selon INetworkRunnerCallbacks) ---

        public void OnPlayerJoined(NetworkRunner r, Fusion.PlayerRef player)
            => PlayerConnected?.Invoke(new PlayerRef(player.PlayerId + 1));

        public void OnPlayerLeft(NetworkRunner r, Fusion.PlayerRef player)
            => PlayerDisconnected?.Invoke(new PlayerRef(player.PlayerId + 1));

        // NOTE : INetworkRunnerCallbacks a beaucoup d'autres méthodes (OnInput, OnConnectedToServer,
        // OnShutdown, …). Les implémenter (vides si inutilisées) pour satisfaire l'interface Fusion.
    }
}
#endif
