using Game.Net;
using NUnit.Framework;

namespace Game.Net.Tests
{
    public class LoopbackTransportTests
    {
        [Test]
        public void ClientCommand_ReachesHostOnPoll()
        {
            var net = new LoopbackNetwork();
            var host = net.CreateHost();
            var client = net.CreateClient();

            PlayerRef from = PlayerRef.None;
            bool got = false;
            host.CommandReceived += (f, cmd) => { from = f; got = true; };

            client.SendCommand(new PlayerInputCommand { tick = 1, aimYaw = 42f });
            Assert.IsFalse(got, "pas encore livré avant Poll");

            host.Poll();
            Assert.IsTrue(got);
            Assert.AreEqual(client.LocalPlayer, from);
        }

        [Test]
        public void HostSnapshot_ReachesClientsOnPoll()
        {
            var net = new LoopbackNetwork();
            var host = net.CreateHost();
            var client = net.CreateClient();

            int wave = -1;
            client.SnapshotReceived += s => wave = s.wave;

            host.BroadcastSnapshot(new WorldSnapshot { wave = 7, players = new PlayerNetState[0], enemies = new EnemyNetState[0] });
            client.Poll();
            Assert.AreEqual(7, wave);
        }

        [Test]
        public void HostLocalCommand_IsImmediate()
        {
            var net = new LoopbackNetwork();
            var host = net.CreateHost();

            bool got = false;
            host.CommandReceived += (f, cmd) => got = true;
            host.SendCommand(new PlayerInputCommand()); // hôte = listen-server : appliqué tout de suite
            Assert.IsTrue(got);
        }

        [Test]
        public void Latency_DelaysDeliveryByPolls()
        {
            var net = new LoopbackNetwork { DelayPolls = 2 };
            var host = net.CreateHost();
            var client = net.CreateClient();

            int count = 0;
            host.CommandReceived += (f, cmd) => count++;
            client.SendCommand(new PlayerInputCommand());

            host.Poll();            // poll 1
            host.Poll();            // poll 2
            Assert.AreEqual(0, count, "retardé de 2 polls");
            host.Poll();            // poll 3 -> livré
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ClientJoin_NotifiesHost()
        {
            var net = new LoopbackNetwork();
            var host = net.CreateHost();

            PlayerRef joined = PlayerRef.None;
            host.PlayerConnected += p => joined = p;

            var client = net.CreateClient();
            Assert.AreEqual(client.LocalPlayer, joined);
        }

        [Test]
        public void Roles_AreCorrect()
        {
            var net = new LoopbackNetwork();
            var host = net.CreateHost();
            var client = net.CreateClient();

            Assert.IsTrue(host.IsServer);
            Assert.AreEqual(NetworkRole.Host, host.Role);
            Assert.IsFalse(client.IsServer);
            Assert.AreEqual(NetworkRole.Client, client.Role);
        }
    }
}
