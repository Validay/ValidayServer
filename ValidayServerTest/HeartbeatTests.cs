using System;
using Xunit;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;

namespace ValidayServerTest
{
    public class HeartbeatTests
    {
        /// <summary>
        /// Testable subclass that exposes a controllable clock and the CheckHeartbeats method.
        /// </summary>
        class TestableHeartbeatManager : HeartbeatManager
        {
            public DateTime FakeNow { get; set; } = DateTime.UtcNow;
            protected override DateTime Now => FakeNow;

            public TestableHeartbeatManager(IServer server, ILogger logger,
                TimeSpan interval, TimeSpan timeout)
                : base(server, logger, interval, timeout) { }

            public void Tick() => CheckHeartbeats();
        }

        static (FakeServer server, TestableHeartbeatManager manager) MakeManager()
        {
            var server = new FakeServer();
            var logger = new ConsoleLogger(LogType.Info);
            var manager = new TestableHeartbeatManager(
                server, logger,
                interval: TimeSpan.FromSeconds(30),
                timeout: TimeSpan.FromSeconds(10));
            return (server, manager);
        }

        [Fact]
        public void HeartbeatManager_Name_IsCorrect()
        {
            var (_, manager) = MakeManager();
            Assert.Equal(nameof(HeartbeatManager), manager.Name);
        }

        [Fact]
        public void HeartbeatManager_NullServer_ThrowsArgumentNullException()
        {
            var logger = new ConsoleLogger(LogType.Info);
            Assert.Throws<ArgumentNullException>(() =>
                new HeartbeatManager(null!, logger,
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void HeartbeatManager_NullLogger_ThrowsArgumentNullException()
        {
            var server = new FakeServer();
            Assert.Throws<ArgumentNullException>(() =>
                new HeartbeatManager(server, null!,
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void HeartbeatManager_AfterStart_IsActiveTrue()
        {
            var (_, manager) = MakeManager();
            manager.Start();
            Assert.True(manager.IsActive);
            manager.Stop();
        }

        [Fact]
        public void HeartbeatManager_AfterStop_IsActiveFalse()
        {
            var (_, manager) = MakeManager();
            manager.Start();
            manager.Stop();
            Assert.False(manager.IsActive);
        }

        [Fact]
        public void HeartbeatManager_ClientWithinTimeout_SendsPing()
        {
            var (server, manager) = MakeManager();
            manager.Start();

            var client = new FakeClient();
            server.SimulateClientConnected(client);

            // Don't advance time — client is well within timeout.
            manager.Tick();

            Assert.True(server.SentCommands.Count > 0);
            Assert.Equal(client, server.SentCommands[0].Client);

            manager.Stop();
        }

        [Fact]
        public void HeartbeatManager_ClientExceedsTimeout_Disconnected()
        {
            var (server, manager) = MakeManager();
            manager.Start();

            var client = new FakeClient();
            server.SimulateClientConnected(client);

            // Advance time past the 10-second timeout.
            manager.FakeNow = manager.FakeNow.AddSeconds(11);
            manager.Tick();

            Assert.Equal(client, server.LastDisconnected);
            Assert.Equal(1, server.DisconnectCallCount);

            manager.Stop();
        }

        [Fact]
        public void HeartbeatManager_PongReceived_UpdatesLastSeen()
        {
            var (server, manager) = MakeManager();
            manager.Start();

            var client = new FakeClient();
            server.SimulateClientConnected(client);

            // Advance time past timeout.
            manager.FakeNow = manager.FakeNow.AddSeconds(11);

            // Send pong — this should reset the last-seen time to FakeNow.
            byte[] pong = BitConverter.GetBytes(HeartbeatManager.DefaultPongCommandId);
            server.SimulateDataReceived(client, pong);

            // Tick — client should NOT be disconnected because pong just arrived.
            manager.Tick();

            Assert.Equal(0, server.DisconnectCallCount);

            manager.Stop();
        }

        [Fact]
        public void HeartbeatManager_PongReceived_ClientNotDisconnected()
        {
            var (server, manager) = MakeManager();
            manager.Start();

            var client = new FakeClient();
            server.SimulateClientConnected(client);

            // Advance time but not past timeout.
            manager.FakeNow = manager.FakeNow.AddSeconds(5);

            // Pong received — reinforces the connection is alive.
            byte[] pong = BitConverter.GetBytes(HeartbeatManager.DefaultPongCommandId);
            server.SimulateDataReceived(client, pong);

            manager.Tick();

            Assert.Equal(0, server.DisconnectCallCount);

            manager.Stop();
        }
    }
}
