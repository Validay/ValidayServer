using Xunit;
using ValidayServer.Network;
using ValidayServer.Network.Interfaces;
using ValidayServer.Network.Commands;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Managers;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using System.Net.Sockets;

namespace ValidayServerTest
{
    public class ServerCommandTests
    {
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

        static IClient MakeFakeClient()
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            return new Client(socket);
        }

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
    }
}