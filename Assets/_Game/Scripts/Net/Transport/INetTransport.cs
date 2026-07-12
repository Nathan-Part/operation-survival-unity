using System;

namespace Game.Net
{
    /// Frontière entre la simulation coop (pure) et le netcode concret (Photon Fusion, loopback
    /// en mémoire, ou autre). La simulation ne connaît QUE cette interface : on peut changer de
    /// solution réseau sans toucher au gameplay, et tester tout le cœur avec LoopbackTransport.
    public interface INetTransport
    {
        NetworkRole Role { get; }
        PlayerRef LocalPlayer { get; }
        bool IsServer { get; }

        event Action<PlayerRef> PlayerConnected;
        event Action<PlayerRef> PlayerDisconnected;

        // Client -> hôte
        event Action<PlayerRef, PlayerInputCommand> CommandReceived;
        event Action<PlayerRef, DamageClaim> DamageClaimReceived;

        // Hôte -> clients
        event Action<WorldSnapshot> SnapshotReceived;

        void SendCommand(PlayerInputCommand cmd);
        void SendDamageClaim(DamageClaim claim);
        void BroadcastSnapshot(WorldSnapshot snapshot);

        /// À appeler une fois par frame : distribue les messages en attente.
        void Poll();
    }
}
