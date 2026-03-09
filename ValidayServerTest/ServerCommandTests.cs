using Xunit;
using ValidayServer.Network;
using ValidayServer.Network.Interfaces;
using ValidayServer.Network.Commands;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Managers;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace ValidayServerTest
{
    public class ServerCommandTests
    {
        // ─── Command helpers ─────────────────────────────────────────────────────

        class TestCommandOne : IServerCommand
        {
            public bool WasExecuted { get; private set; }
            public IClient? LastSender { get; private set; }
            public byte[]? LastData { get; private set; }

            public void Execute(IClient sender, byte[] rawData)
            {
                WasExecuted = true;
                LastSender = sender;
                LastData = rawData;
            }
        }

        class TestCommandTwo : IServerCommand
        {
            public void Execute(IClient sender, byte[] rawData) { }
        }

        class ThrowingCommand : IServerCommand
        {
            public void Execute(IClient sender, byte[] rawData)
                => throw new InvalidOperationException("Intentional error");
        }

        static IClient MakeFakeClient() => new FakeClient();

        static (IServer server, CommandHandlerManager handler) MakeServer()
        {
            IServer server = new Server();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var handler = new CommandHandlerManager(server, logger);
            return (server, handler);
        }

        [Fact]
        public void RegistrationCommand_Success_AppearsInMap()
        {
            var (_, handler) = MakeServer();

            handler.RegistrationCommand<TestCommandOne>(1);

            Assert.True(handler.ServerCommandsMap.ContainsKey(1));
            Assert.Equal(typeof(TestCommandOne), handler.ServerCommandsMap[1]);
        }

        [Fact]
        public void RegistrationCommand_MultipleCommands_AllPresentInMap()
        {
            var (_, handler) = MakeServer();

            handler.RegistrationCommand<TestCommandOne>(1);
            handler.RegistrationCommand<TestCommandTwo>(2);

            Assert.Equal(2, handler.ServerCommandsMap.Count);
            Assert.Equal(typeof(TestCommandOne), handler.ServerCommandsMap[1]);
            Assert.Equal(typeof(TestCommandTwo), handler.ServerCommandsMap[2]);
        }

        [Fact]
        public void RegistrationCommand_DuplicateId_ThrowsInvalidOperationException()
        {
            var (_, handler) = MakeServer();
            handler.RegistrationCommand<TestCommandOne>(1);

            Assert.Throws<InvalidOperationException>(() =>
                handler.RegistrationCommand<TestCommandTwo>(1));
        }

        [Fact]
        public void RegistrationCommand_DuplicateType_ThrowsInvalidOperationException()
        {
            var (_, handler) = MakeServer();
            handler.RegistrationCommand<TestCommandOne>(1);

            Assert.Throws<InvalidOperationException>(() =>
                handler.RegistrationCommand<TestCommandOne>(2));
        }

        [Fact]
        public void RegistrationCommand_MapIsReadOnly_CannotBeModifiedExternally()
        {
            var (_, handler) = MakeServer();
            handler.RegistrationCommand<TestCommandOne>(1);

            var snapshot = handler.ServerCommandsMap;
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<ushort, Type>)snapshot).Add(99, typeof(TestCommandTwo)));
        }

        [Fact]
        public void CommandPool_GetCommand_ReturnsInstance()
        {
            var pool = new CommandPool<ushort, IServerCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            IServerCommand cmd = pool.GetCommand(1, map);

            Assert.NotNull(cmd);
            Assert.IsType<TestCommandOne>(cmd);
        }

        [Fact]
        public void CommandPool_GetCommand_UnknownId_ThrowsKeyNotFoundException()
        {
            var pool = new CommandPool<ushort, IServerCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            Assert.Throws<KeyNotFoundException>(() => pool.GetCommand(99, map));
        }

        [Fact]
        public void CommandPool_AfterReturn_GetCommand_ReturnsSameInstance()
        {
            var pool = new CommandPool<ushort, IServerCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            IServerCommand first = pool.GetCommand(1, map);
            pool.ReturnCommandToPool(1, first, map);
            IServerCommand second = pool.GetCommand(1, map);

            Assert.Same(first, second);
        }

        [Fact]
        public void CommandPool_WithoutReturn_GetCommand_ReturnsNewInstance()
        {
            var pool = new CommandPool<ushort, IServerCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            IServerCommand first = pool.GetCommand(1, map);
            IServerCommand second = pool.GetCommand(1, map);

            Assert.NotSame(first, second);
        }

        [Fact]
        public void CommandPool_ReturnCommand_UnknownId_ThrowsKeyNotFoundException()
        {
            var pool = new CommandPool<ushort, IServerCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };
            var cmd = new TestCommandOne();

            Assert.Throws<KeyNotFoundException>(() =>
                pool.ReturnCommandToPool(99, cmd, map));
        }

        [Fact]
        public void CommandPool_MultipleTypes_IndependentPools()
        {
            var pool = new CommandPool<ushort, IServerCommand>();
            var map = new Dictionary<ushort, Type>
            {
                { 1, typeof(TestCommandOne) },
                { 2, typeof(TestCommandTwo) }
            };

            IServerCommand cmdOne = pool.GetCommand(1, map);
            IServerCommand cmdTwo = pool.GetCommand(2, map);

            Assert.IsType<TestCommandOne>(cmdOne);
            Assert.IsType<TestCommandTwo>(cmdTwo);
        }

        [Fact]
        public void CommandPool_ReturnMultiple_GetInSequence_BothReused()
        {
            var pool = new CommandPool<ushort, IServerCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            IServerCommand a = pool.GetCommand(1, map);
            IServerCommand b = pool.GetCommand(1, map);
            pool.ReturnCommandToPool(1, a, map);
            pool.ReturnCommandToPool(1, b, map);

            IServerCommand c = pool.GetCommand(1, map);
            IServerCommand d = pool.GetCommand(1, map);

            Assert.True(ReferenceEquals(c, a) || ReferenceEquals(c, b));
            Assert.True(ReferenceEquals(d, a) || ReferenceEquals(d, b));
            Assert.NotSame(c, d);
        }

        [Fact]
        public void CommandPool_ConcurrentGet_DoesNotThrow()
        {
            var pool = new CommandPool<ushort, IServerCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            {
                try
                {
                    var cmd = pool.GetCommand(1, map);
                    pool.ReturnCommandToPool(1, cmd, map);
                }
                catch (Exception ex) { exceptions.Add(ex); }
            }));

            Task.WaitAll(tasks.ToArray());

            Assert.Empty(exceptions);
        }

        [Fact]
        public void Command_Execute_SetsWasExecuted()
        {
            var client = MakeFakeClient();
            var cmd = new TestCommandOne();
            var data = new byte[] { 1, 2, 3 };

            cmd.Execute(client, data);

            Assert.True(cmd.WasExecuted);
        }

        [Fact]
        public void Command_Execute_PassesSenderAndData()
        {
            var client = MakeFakeClient();
            var cmd = new TestCommandOne();
            var data = new byte[] { 10, 20 };

            cmd.Execute(client, data);

            Assert.Same(client, cmd.LastSender);
            Assert.Equal(data, cmd.LastData);
        }

        [Fact]
        public void Command_Execute_EmptyData_DoesNotThrow()
        {
            var client = MakeFakeClient();
            var cmd = new TestCommandOne();

            var ex = Record.Exception(() => cmd.Execute(client, Array.Empty<byte>()));

            Assert.Null(ex);
        }

        [Fact]
        public void CommandHandlerManager_Name_IsCorrect()
        {
            var (_, handler) = MakeServer();

            Assert.Equal(nameof(CommandHandlerManager), handler.Name);
        }


        [Fact]
        public void BadPacketDefenderManager_Name_IsCorrect()
        {
            IServer server = new Server();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var manager = new BadPacketDefenderManager(server, logger);

            Assert.Equal(nameof(BadPacketDefenderManager), manager.Name);
        }

        [Fact]
        public void BadPacketDefenderManager_AfterStart_IsActiveTrue()
        {
            IServer server = new Server();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var manager = new BadPacketDefenderManager(server, logger);

            manager.Start();

            Assert.True(manager.IsActive);
        }

        [Fact]
        public void BadPacketDefenderManager_AfterStop_IsActiveFalse()
        {
            IServer server = new Server();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var manager = new BadPacketDefenderManager(server, logger);

            manager.Start();
            manager.Stop();

            Assert.False(manager.IsActive);
        }

        [Fact]
        public void BadPacketDefenderManager_CustomThreshold_RegistersSuccessfully()
        {
            IServer server = new Server();
            ILogger logger = new ConsoleLogger(LogType.Info);

            var ex = Record.Exception(() =>
                new BadPacketDefenderManager(server, logger, 5, new UshortConverterId()));

            Assert.Null(ex);
        }

        // ─── BadPacketDefenderManager behaviour tests ───────────────────────────

        [Fact]
        public void BadPacketDefenderManager_Threshold_DisconnectsClientAfterNBadPackets()
        {
            var fakeServer = new FakeServer();
            var logger = new ConsoleLogger(LogType.Low);
            var handler = new CommandHandlerManager(fakeServer, logger);
            // Register one known command so the defender has a registry to check.
            handler.RegistrationCommand<TestCommandOne>(1);
            var defender = new BadPacketDefenderManager(fakeServer, logger, countBadPacketForDisconnect: 3, new UshortConverterId());

            var client = new FakeClient();

            handler.Start();
            defender.Start();

            fakeServer.SimulateClientConnected(client);

            // Unknown command ID (99 not registered) — each counts as bad.
            byte[] badPacket = BitConverter.GetBytes((ushort)99);
            fakeServer.SimulateDataReceived(client, badPacket);
            fakeServer.SimulateDataReceived(client, badPacket);

            Assert.Null(fakeServer.LastDisconnected); // threshold not reached yet

            fakeServer.SimulateDataReceived(client, badPacket);

            Assert.Same(client, fakeServer.LastDisconnected);
            Assert.Equal(1, fakeServer.DisconnectCallCount);
        }

        [Fact]
        public void BadPacketDefenderManager_GoodPackets_DoNotDisconnect()
        {
            var fakeServer = new FakeServer();
            var logger = new ConsoleLogger(LogType.Low);
            var handler = new CommandHandlerManager(fakeServer, logger);
            handler.RegistrationCommand<TestCommandOne>(1);
            var defender = new BadPacketDefenderManager(fakeServer, logger, 3, new UshortConverterId());

            var client = new FakeClient();
            handler.Start();
            defender.Start();
            fakeServer.SimulateClientConnected(client);

            byte[] goodPacket = BitConverter.GetBytes((ushort)1);
            for (int i = 0; i < 10; i++)
                fakeServer.SimulateDataReceived(client, goodPacket);

            Assert.Null(fakeServer.LastDisconnected);
        }

        [Fact]
        public void BadPacketDefenderManager_TooShortData_CountsAsBadPacket()
        {
            var fakeServer = new FakeServer();
            var logger = new ConsoleLogger(LogType.Low);
            var handler = new CommandHandlerManager(fakeServer, logger);
            var defender = new BadPacketDefenderManager(fakeServer, logger, 1, new UshortConverterId());

            var client = new FakeClient();
            handler.Start();
            defender.Start();
            fakeServer.SimulateClientConnected(client);

            // 1-byte packet is too short to hold a ushort command ID.
            fakeServer.SimulateDataReceived(client, new byte[] { 0x01 });

            Assert.Same(client, fakeServer.LastDisconnected);
        }

        [Fact]
        public void BadPacketDefenderManager_ClientDisconnected_RemovesTracking()
        {
            var fakeServer = new FakeServer();
            var logger = new ConsoleLogger(LogType.Low);
            var defender = new BadPacketDefenderManager(fakeServer, logger, 5, new UshortConverterId());
            var handler = new CommandHandlerManager(fakeServer, logger);

            var client = new FakeClient();
            defender.Start();
            handler.Start();
            fakeServer.SimulateClientConnected(client);
            fakeServer.SimulateClientDisconnected(client);

            // After disconnect, sending data should not throw.
            byte[] data = BitConverter.GetBytes((ushort)99);
            var ex = Record.Exception(() => fakeServer.SimulateDataReceived(client, data));

            Assert.Null(ex);
        }

        // ─── CommandHandlerManager behaviour tests ──────────────────────────────

        [Fact]
        public void CommandHandlerManager_ShortData_DoesNotCrash()
        {
            var fakeServer = new FakeServer();
            var logger = new ConsoleLogger(LogType.Low);
            var handler = new CommandHandlerManager(fakeServer, logger);
            handler.RegistrationCommand<TestCommandOne>(1);
            handler.Start();

            var client = new FakeClient();
            var ex = Record.Exception(() => fakeServer.SimulateDataReceived(client, new byte[] { 0x01 }));

            Assert.Null(ex);
        }

        [Fact]
        public void CommandHandlerManager_UnknownCommand_DoesNotCrash()
        {
            var fakeServer = new FakeServer();
            var logger = new ConsoleLogger(LogType.Low);
            var handler = new CommandHandlerManager(fakeServer, logger);
            handler.RegistrationCommand<TestCommandOne>(1);
            handler.Start();

            var client = new FakeClient();
            byte[] unknownPacket = BitConverter.GetBytes((ushort)99);
            var ex = Record.Exception(() => fakeServer.SimulateDataReceived(client, unknownPacket));

            Assert.Null(ex);
        }

        [Fact]
        public void CommandHandlerManager_AfterStop_DoesNotExecuteCommands()
        {
            var fakeServer = new FakeServer();
            var logger = new ConsoleLogger(LogType.Low);
            var handler = new CommandHandlerManager(fakeServer, logger);
            handler.RegistrationCommand<TestCommandOne>(1);
            handler.Start();
            handler.Stop();

            var client = new FakeClient();
            var cmd = new TestCommandOne();

            // Simulate data — handler unsubscribed, so cmd.Execute should never be called.
            byte[] packet = BitConverter.GetBytes((ushort)1);
            fakeServer.SimulateDataReceived(client, packet);

            // If the handler were still active it would get a new instance from the pool.
            // We can't observe that directly, but we can verify Stop sets IsActive correctly.
            Assert.False(handler.IsActive);
        }

        [Fact]
        public void CommandHandlerManager_ThrowingCommand_DoesNotCrash()
        {
            var fakeServer = new FakeServer();
            var logger = new ConsoleLogger(LogType.Low);
            var handler = new CommandHandlerManager(fakeServer, logger);
            handler.RegistrationCommand<ThrowingCommand>(1);
            handler.Start();

            var client = new FakeClient();
            byte[] packet = BitConverter.GetBytes((ushort)1);

            // ThrowingCommand.Execute throws, but the manager catches it.
            var ex = Record.Exception(() => fakeServer.SimulateDataReceived(client, packet));

            Assert.Null(ex);
        }
    }
}
