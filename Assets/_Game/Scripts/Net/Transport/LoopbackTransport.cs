using System;
using System.Collections.Generic;

namespace Game.Net
{
    /// Réseau en mémoire, mono-process, déterministe : un hôte + N clients dans le même process.
    /// Sert aux tests EditMode ET à faire tourner une session « hôte seul + bots » sans Photon.
    /// La livraison passe par Poll() de chaque endpoint (retardable de DelayPolls pour simuler
    /// de la latence). Les messages sont passés par référence (pas de sérialisation ici — c'est
    /// la responsabilité de l'adaptateur transport réel).
    public class LoopbackNetwork
    {
        readonly List<LoopbackTransport> endpoints = new List<LoopbackTransport>();
        int nextId = 1;

        /// Retard de livraison, en nombre de Poll, pour simuler de la latence (0 = quasi immédiat).
        public int DelayPolls = 0;

        public LoopbackTransport CreateHost()
        {
            var t = new LoopbackTransport(this, new PlayerRef(nextId++), NetworkRole.Host);
            endpoints.Add(t);
            Announce(t.LocalPlayer);
            return t;
        }

        public LoopbackTransport CreateClient()
        {
            var t = new LoopbackTransport(this, new PlayerRef(nextId++), NetworkRole.Client);
            endpoints.Add(t);
            Announce(t.LocalPlayer);
            return t;
        }

        LoopbackTransport Host()
        {
            foreach (var e in endpoints)
                if (e.Role == NetworkRole.Host) return e;
            return null;
        }

        void Announce(PlayerRef joined)
        {
            foreach (var e in endpoints)
                e.RaisePlayerConnected(joined);
        }

        internal void RouteCommand(PlayerRef from, PlayerInputCommand cmd)
        {
            var host = Host();
            if (host != null) host.Enqueue(() => host.RaiseCommand(from, cmd), DelayPolls);
        }

        internal void RouteDamageClaim(PlayerRef from, DamageClaim claim)
        {
            var host = Host();
            if (host != null) host.Enqueue(() => host.RaiseDamageClaim(from, claim), DelayPolls);
        }

        internal void RouteSnapshot(WorldSnapshot snap)
        {
            foreach (var e in endpoints)
            {
                if (e.Role != NetworkRole.Client) continue;
                var target = e;
                target.Enqueue(() => target.RaiseSnapshot(snap), DelayPolls);
            }
        }
    }

    /// Un endpoint (hôte ou client) du LoopbackNetwork. Implémente INetTransport en mémoire.
    public class LoopbackTransport : INetTransport
    {
        struct Pending
        {
            public Action action;
            public int dueAtPoll;
        }

        readonly LoopbackNetwork net;
        readonly Queue<Pending> pending = new Queue<Pending>();
        int pollCount;

        public LoopbackTransport(LoopbackNetwork net, PlayerRef local, NetworkRole role)
        {
            this.net = net;
            LocalPlayer = local;
            Role = role;
        }

        public NetworkRole Role { get; }
        public PlayerRef LocalPlayer { get; }
        public bool IsServer => Role == NetworkRole.Host;

        public event Action<PlayerRef> PlayerConnected;
        public event Action<PlayerRef> PlayerDisconnected;
        public event Action<PlayerRef, PlayerInputCommand> CommandReceived;
        public event Action<PlayerRef, DamageClaim> DamageClaimReceived;
        public event Action<WorldSnapshot> SnapshotReceived;

        public void SendCommand(PlayerInputCommand cmd)
        {
            if (IsServer) { RaiseCommand(LocalPlayer, cmd); return; }
            net.RouteCommand(LocalPlayer, cmd);
        }

        public void SendDamageClaim(DamageClaim claim)
        {
            if (IsServer) { RaiseDamageClaim(LocalPlayer, claim); return; }
            net.RouteDamageClaim(LocalPlayer, claim);
        }

        public void BroadcastSnapshot(WorldSnapshot snapshot)
        {
            if (!IsServer) return;
            net.RouteSnapshot(snapshot);
        }

        public void Poll()
        {
            pollCount++;
            int n = pending.Count;
            for (int i = 0; i < n; i++)
            {
                var p = pending.Dequeue();
                if (p.dueAtPoll <= pollCount) p.action?.Invoke();
                else pending.Enqueue(p);
            }
        }

        internal void Enqueue(Action action, int delayPolls)
        {
            int delay = delayPolls < 0 ? 0 : delayPolls;
            pending.Enqueue(new Pending { action = action, dueAtPoll = pollCount + delay + 1 });
        }

        internal void RaisePlayerConnected(PlayerRef p) => PlayerConnected?.Invoke(p);
        internal void RaisePlayerDisconnected(PlayerRef p) => PlayerDisconnected?.Invoke(p);
        internal void RaiseCommand(PlayerRef from, PlayerInputCommand cmd) => CommandReceived?.Invoke(from, cmd);
        internal void RaiseDamageClaim(PlayerRef from, DamageClaim claim) => DamageClaimReceived?.Invoke(from, claim);
        internal void RaiseSnapshot(WorldSnapshot snap) => SnapshotReceived?.Invoke(snap);
    }
}
