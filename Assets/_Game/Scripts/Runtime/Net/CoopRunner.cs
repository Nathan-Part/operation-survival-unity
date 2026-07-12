using System;
using Game.Data;
using Game.Net;
using UnityEngine;

namespace Game.Runtime.Net
{
    /// Pilote fin qui relie un transport réseau (LoopbackTransport en local, FusionTransport
    /// plus tard) à la simulation coop pure (Game.Net.CoopSession). Côté hôte, il avance la
    /// simulation dans Update et diffuse un snapshot ; côté client, il expose les snapshots reçus.
    ///
    /// Volontairement ignorant du rendu : il ne connaît ni NavMesh ni prefab. Un « binder de
    /// scène » (à écrire en Phase 6 quand une scène coop existera) s'abonne à EnemySpawnRequested
    /// pour instancier les visuels et pousse les positions/kills via Session.SetEnemyPosition /
    /// Session.RemoveEnemy. Ce composant touche Unity (Time, MonoBehaviour) donc il n'est PAS
    /// testé hors moteur — toute la logique testée vit dans Game.Net. À vérifier en éditeur.
    public class CoopRunner : MonoBehaviour
    {
        [Tooltip("Fréquence de diffusion des snapshots (Hz).")]
        public float snapshotHz = 20f;

        INetTransport transport;
        CoopSession session;
        float snapshotAccumulator;

        public CoopSession Session => session;
        public INetTransport Transport => transport;
        public bool IsHost => transport != null && transport.IsServer;

        /// Côté hôte : un ennemi doit apparaître (le binder de scène l'instancie et lui donne une position).
        public event Action<SpawnRequest> EnemySpawnRequested;
        /// Côté hôte : un ennemi vient d'être tué (démarrer l'anim de mort, puis Session.RemoveEnemy).
        public event Action<EnemyRuntimeState> EnemyKilled;
        /// Côté hôte : un snapshot vient d'être produit (utile pour du debug/HUD local).
        public event Action<WorldSnapshot> SnapshotProduced;
        /// Côté client : un snapshot autoritaire vient d'être reçu (à appliquer aux proxies).
        public event Action<WorldSnapshot> SnapshotApplied;

        /// Initialise ce runner en HÔTE : crée la session à partir des données du jeu.
        public void InitHost(INetTransport netTransport, GameDatabase db, Func<int, string[]> composition)
        {
            transport = netTransport;
            var cfg = CoopConfigFactory.FromDatabase(db);
            var hpLookup = CoopConfigFactory.EnemyHpMultLookup(db);
            session = new CoopSession(cfg, composition, hpLookup);

            session.EnemySpawned += r => EnemySpawnRequested?.Invoke(r);
            session.EnemyKilled += e => EnemyKilled?.Invoke(e);

            transport.PlayerConnected += OnPlayerConnected;
            transport.PlayerDisconnected += OnPlayerDisconnected;
            transport.CommandReceived += OnCommand;
            transport.DamageClaimReceived += OnDamageClaim;

            // L'hôte est lui-même un joueur de la partie.
            session.AddPlayer(transport.LocalPlayer, "Host", null, 100f);
        }

        /// Initialise ce runner en CLIENT : il ne fait qu'écouter les snapshots de l'hôte.
        public void InitClient(INetTransport netTransport)
        {
            transport = netTransport;
            transport.SnapshotReceived += s => SnapshotApplied?.Invoke(s);
        }

        public void StartMatch() => session?.StartMatch();

        void OnPlayerConnected(PlayerRef p)
        {
            if (session == null || p == transport.LocalPlayer) return;
            session.AddPlayer(p, p.ToString(), null, 100f);
        }

        void OnPlayerDisconnected(PlayerRef p) => session?.RemovePlayer(p);
        void OnCommand(PlayerRef from, PlayerInputCommand cmd) => session?.ApplyInput(from, cmd);
        void OnDamageClaim(PlayerRef from, DamageClaim claim) => session?.ApplyDamageClaim(claim);

        void Update()
        {
            if (transport == null) return;
            transport.Poll();

            if (session == null) return; // client : rien à simuler
            if (session.Phase != SessionPhase.Active) return;

            session.Tick(Time.deltaTime);

            float interval = snapshotHz > 0f ? 1f / snapshotHz : 0.05f;
            snapshotAccumulator += Time.deltaTime;
            if (snapshotAccumulator >= interval)
            {
                snapshotAccumulator = 0f;
                var snap = session.BuildSnapshot();
                transport.BroadcastSnapshot(snap);
                SnapshotProduced?.Invoke(snap);
            }
        }
    }
}
