using System;
using System.Collections.Generic;
using Game.Net;
using NUnit.Framework;

namespace Game.Net.Tests
{
    public class CoopSessionTests
    {
        CoopSessionConfig cfg;
        Func<int, string[]> plan3;
        Func<string, float> hp1;

        [SetUp]
        public void SetUp()
        {
            cfg = new CoopSessionConfig
            {
                maxPlayers = 4,
                intermissionSeconds = 0.5f,
                baseEnemyHpMult = 1f,
                perKillMoney = 15,
                bleedOutSeconds = 15f,
                reviveSeconds = 3f,
                reviveRadius = 2.5f,
                respawnSeconds = 5f,
                reviveHealthFraction = 0.5f,
                respawnHealthFraction = 1f,
                enemyCountPerExtraPlayer = 1f,
            };
            plan3 = w => new[] { "shooter", "shooter", "shooter" };
            hp1 = id => 1f;
        }

        CoopSession NewSession() => new CoopSession(cfg, plan3, hp1);

        // ---------- Lobby ----------

        [Test]
        public void AddPlayer_RespectsMaxAndDedup()
        {
            var s = NewSession();
            Assert.IsNotNull(s.AddPlayer(new PlayerRef(1), "a", null, 100f));
            Assert.IsNotNull(s.AddPlayer(new PlayerRef(2), "b", null, 100f));
            Assert.IsNotNull(s.AddPlayer(new PlayerRef(3), "c", null, 100f));
            Assert.IsNotNull(s.AddPlayer(new PlayerRef(4), "d", null, 100f));
            Assert.IsNull(s.AddPlayer(new PlayerRef(5), "e", null, 100f), "au-delà de maxPlayers");
            Assert.AreSame(s.GetPlayer(new PlayerRef(1)), s.AddPlayer(new PlayerRef(1), "a", null, 100f), "doublon");
            Assert.AreEqual(4, s.PlayerCount);
        }

        [Test]
        public void StartMatch_MovesToActive()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "a", null, 100f);
            Assert.AreEqual(SessionPhase.Lobby, s.Phase);
            s.StartMatch();
            Assert.AreEqual(SessionPhase.Active, s.Phase);
        }

        // ---------- Vagues & dégâts ennemis ----------

        List<int> SpawnFirstWave(CoopSession s)
        {
            s.StartMatch();
            s.Tick(1f); // > intermission -> vague 1 apparaît
            var ids = new List<int>();
            foreach (var kv in s.Enemies) ids.Add(kv.Key);
            return ids;
        }

        [Test]
        public void Tick_SpawnsEnemiesAfterIntermission()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "host", null, 100f);
            var ids = SpawnFirstWave(s);
            Assert.AreEqual(3, ids.Count);
            Assert.AreEqual(3, s.AliveEnemyCount);
        }

        [Test]
        public void DamageClaim_KillsEnemy_AwardsMoneyAndKill()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "host", null, 100f);
            var ids = SpawnFirstWave(s);
            int target = ids[0];

            var result = s.ApplyDamageClaim(new DamageClaim
            {
                attacker = new PlayerRef(1),
                enemyNetId = target,
                amount = 1000f,
                hitPoint = NetVector3.Zero,
            });

            Assert.AreEqual(DamageValidator.Result.Accepted, result);
            Assert.AreEqual(2, s.AliveEnemyCount);
            Assert.AreEqual(15, s.SharedMoney);
            Assert.AreEqual(1, s.GetPlayer(new PlayerRef(1)).kills);
        }

        [Test]
        public void DamageClaim_UnknownEnemy_Rejected()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "host", null, 100f);
            SpawnFirstWave(s);
            var result = s.ApplyDamageClaim(new DamageClaim
            {
                attacker = new PlayerRef(1),
                enemyNetId = 99999,
                amount = 10f,
                hitPoint = NetVector3.Zero,
            });
            Assert.AreEqual(DamageValidator.Result.UnknownEnemy, result);
        }

        [Test]
        public void KillingWholeWave_ClearsAndAdvances()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "host", null, 100f);
            var ids = SpawnFirstWave(s);
            foreach (int id in ids)
                s.ApplyDamageClaim(new DamageClaim { attacker = new PlayerRef(1), enemyNetId = id, amount = 1000f, hitPoint = NetVector3.Zero });

            Assert.AreEqual(0, s.AliveEnemyCount);
            s.Tick(0f); // détecte le nettoyage
            Assert.AreEqual(WaveState.Intermission, s.Director.State);
            s.Tick(1f); // intermission -> vague 2
            Assert.AreEqual(2, s.Director.CurrentWave);
        }

        // ---------- Down / relève / respawn / wipe ----------

        [Test]
        public void FatalDamage_WithTeammate_DownsNotKills()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "a", null, 100f);
            s.AddPlayer(new PlayerRef(2), "b", null, 100f);
            s.StartMatch();

            s.DamagePlayer(new PlayerRef(1), 150f);
            var p1 = s.GetPlayer(new PlayerRef(1));
            Assert.IsTrue(p1.isDown);
            Assert.IsFalse(p1.isDead);
            Assert.AreEqual(SessionPhase.Active, s.Phase);
        }

        [Test]
        public void DownedPlayer_BleedsOutToDeath_WhenNoReviver()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "a", null, 100f);
            s.AddPlayer(new PlayerRef(2), "b", null, 100f);
            s.StartMatch();
            s.GetPlayer(new PlayerRef(2)).position = new NetVector3(100f, 0f, 0f); // hors de portée

            s.DamagePlayer(new PlayerRef(1), 150f);
            s.Tick(16f); // > bleedOutSeconds

            var p1 = s.GetPlayer(new PlayerRef(1));
            Assert.IsTrue(p1.isDead);
            Assert.AreEqual(1, p1.deaths);
            Assert.AreEqual(SessionPhase.Active, s.Phase); // p2 tient encore
        }

        [Test]
        public void DownedPlayer_RevivedByTeammateInRange()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "a", null, 100f);
            s.AddPlayer(new PlayerRef(2), "b", null, 100f); // à Zero, dans le rayon
            s.StartMatch();

            s.DamagePlayer(new PlayerRef(1), 150f);
            s.Tick(3.1f); // >= reviveSeconds

            var p1 = s.GetPlayer(new PlayerRef(1));
            Assert.IsFalse(p1.isDown);
            Assert.IsFalse(p1.isDead);
            Assert.AreEqual(50f, p1.health, 1e-3f); // maxHealth * reviveHealthFraction
        }

        [Test]
        public void DeadPlayer_RespawnsWhileTeammateAlive()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "a", null, 100f);
            s.AddPlayer(new PlayerRef(2), "b", null, 100f);
            s.StartMatch();
            s.GetPlayer(new PlayerRef(2)).position = new NetVector3(100f, 0f, 0f);

            s.DamagePlayer(new PlayerRef(1), 150f);
            s.Tick(16f); // bleed out -> mort
            Assert.IsTrue(s.GetPlayer(new PlayerRef(1)).isDead);

            s.Tick(6f);  // > respawnSeconds
            var p1 = s.GetPlayer(new PlayerRef(1));
            Assert.IsFalse(p1.isDead);
            Assert.AreEqual(100f, p1.health, 1e-3f);
        }

        [Test]
        public void AllPlayersDown_TriggersWipeGameOver()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "solo", null, 100f);
            s.StartMatch();

            bool gameOver = false;
            s.GameOver += () => gameOver = true;

            s.DamagePlayer(new PlayerRef(1), 150f); // seul joueur à terre -> wipe

            Assert.AreEqual(SessionPhase.GameOver, s.Phase);
            Assert.IsTrue(gameOver);
        }

        [Test]
        public void GameOver_FreezesTick()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "solo", null, 100f);
            s.StartMatch();
            s.DamagePlayer(new PlayerRef(1), 150f);
            Assert.AreEqual(SessionPhase.GameOver, s.Phase);

            int waveBefore = s.Director.CurrentWave;
            s.Tick(100f);
            Assert.AreEqual(waveBefore, s.Director.CurrentWave); // rien ne bouge après GameOver
        }

        // ---------- Snapshot ----------

        [Test]
        public void BuildSnapshot_ReflectsState()
        {
            var s = NewSession();
            s.AddPlayer(new PlayerRef(1), "a", null, 100f);
            s.AddPlayer(new PlayerRef(2), "b", null, 100f);
            SpawnFirstWave(s);

            var snap = s.BuildSnapshot();
            Assert.AreEqual(2, snap.players.Length);
            // 2 joueurs -> +1 ennemi bonus (CoopScaling) : 3 de base + 1 = 4.
            Assert.AreEqual(4, snap.enemies.Length);
            Assert.AreEqual(s.Enemies.Count, snap.enemies.Length);
            Assert.AreEqual(1, snap.wave);
            Assert.AreEqual(SessionPhase.Active, snap.phase);
            Assert.AreEqual(0, snap.sharedMoney);
        }
    }
}
