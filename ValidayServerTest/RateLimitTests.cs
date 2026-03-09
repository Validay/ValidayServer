using System;
using Xunit;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;

namespace ValidayServerTest
{
    public class RateLimitTests
    {
        class TestableRateLimitManager : RateLimitManager
        {
            public DateTime FakeNow { get; set; } = DateTime.UtcNow;
            protected override DateTime Now => FakeNow;

            public TestableRateLimitManager(IServer server, ILogger logger,
                int maxPacketsPerSecond, bool disconnectOnExceed = false)
                : base(server, logger, maxPacketsPerSecond, disconnectOnExceed) { }
        }

        static (FakeServer server, TestableRateLimitManager manager) MakeManager(
            int maxPerSecond = 5,
            bool disconnectOnExceed = false)
        {
            var server = new FakeServer();
            var logger = new ConsoleLogger(LogType.Info);
            var manager = new TestableRateLimitManager(server, logger, maxPerSecond, disconnectOnExceed);
            manager.Start();
            return (server, manager);
        }

        static byte[] AnyPacket() => new byte[] { 0x01, 0x00 };

        [Fact]
        public void RateLimitManager_Name_IsCorrect()
        {
            var (_, manager) = MakeManager();
            Assert.Equal(nameof(RateLimitManager), manager.Name);
            manager.Stop();
        }

        [Fact]
        public void RateLimitManager_NullServer_ThrowsArgumentNullException()
        {
            var logger = new ConsoleLogger(LogType.Info);
            Assert.Throws<ArgumentNullException>(() =>
                new RateLimitManager(null!, logger, 10));
        }

        [Fact]
        public void RateLimitManager_NullLogger_ThrowsArgumentNullException()
        {
            var server = new FakeServer();
            Assert.Throws<ArgumentNullException>(() =>
                new RateLimitManager(server, null!, 10));
        }

        [Fact]
        public void RateLimitManager_AfterStart_IsActiveTrue()
        {
            var (_, manager) = MakeManager();
            Assert.True(manager.IsActive);
            manager.Stop();
        }

        [Fact]
        public void RateLimitManager_AfterStop_IsActiveFalse()
        {
            var (_, manager) = MakeManager();
            manager.Stop();
            Assert.False(manager.IsActive);
        }

        [Fact]
        public void RateLimitManager_UnderLimit_DoesNotDisconnect()
        {
            var (server, manager) = MakeManager(maxPerSecond: 5, disconnectOnExceed: true);
            var client = new FakeClient();
            server.SimulateClientConnected(client);

            // Send exactly at the limit.
            for (int i = 0; i < 5; i++)
                server.SimulateDataReceived(client, AnyPacket());

            Assert.Equal(0, server.DisconnectCallCount);
            manager.Stop();
        }

        [Fact]
        public void RateLimitManager_ExceedsLimit_WithDisconnect_DisconnectsClient()
        {
            var (server, manager) = MakeManager(maxPerSecond: 3, disconnectOnExceed: true);
            var client = new FakeClient();
            server.SimulateClientConnected(client);

            for (int i = 0; i < 4; i++)
                server.SimulateDataReceived(client, AnyPacket());

            Assert.True(server.DisconnectCallCount > 0);
            Assert.Equal(client, server.LastDisconnected);
            manager.Stop();
        }

        [Fact]
        public void RateLimitManager_ExceedsLimit_WithoutDisconnect_DoesNotDisconnect()
        {
            var (server, manager) = MakeManager(maxPerSecond: 3, disconnectOnExceed: false);
            var client = new FakeClient();
            server.SimulateClientConnected(client);

            for (int i = 0; i < 10; i++)
                server.SimulateDataReceived(client, AnyPacket());

            Assert.Equal(0, server.DisconnectCallCount);
            manager.Stop();
        }

        [Fact]
        public void RateLimitManager_WindowReset_AllowsNewPackets()
        {
            var (server, manager) = MakeManager(maxPerSecond: 3, disconnectOnExceed: true);
            var client = new FakeClient();
            server.SimulateClientConnected(client);

            // Use up the limit in window 1.
            for (int i = 0; i < 3; i++)
                server.SimulateDataReceived(client, AnyPacket());

            Assert.Equal(0, server.DisconnectCallCount);

            // Advance past the 1-second window.
            manager.FakeNow = manager.FakeNow.AddSeconds(1.5);

            // Send at the limit again — window has reset, should not disconnect.
            for (int i = 0; i < 3; i++)
                server.SimulateDataReceived(client, AnyPacket());

            Assert.Equal(0, server.DisconnectCallCount);
            manager.Stop();
        }

        [Fact]
        public void RateLimitManager_ClientDisconnected_TrackerRemoved()
        {
            var (server, manager) = MakeManager(maxPerSecond: 3, disconnectOnExceed: true);
            var client = new FakeClient();
            server.SimulateClientConnected(client);
            server.SimulateClientDisconnected(client);

            // After disconnect, data from this client should be silently ignored.
            var ex = Record.Exception(() =>
                server.SimulateDataReceived(client, AnyPacket()));

            Assert.Null(ex);
            Assert.Equal(0, server.DisconnectCallCount);
            manager.Stop();
        }
    }
}
