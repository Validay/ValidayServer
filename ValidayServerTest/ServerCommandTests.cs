using Xunit;
using ValidayServer.Network.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Managers;
using ValidayServer.Network.Commands;
using ValidayServer.Network;
using System.Net.Sockets;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Logging;

namespace ValidayServerTest
{
    public class ServerCommandTests
    {
        class TestServerCommandOne : IServerCommand
        {
            public void Execute(
                IClient sender,
                byte[] rawData)
            {
            }
        }

        class TestServerCommandTwo : IServerCommand
        {
            public void Execute(
                IClient sender,
                byte[] rawData)
            {
            }
        }

        [Fact]
        public void RegistrationCommandSuccess()
        {
            IServer server = new Server();
            ILogger logger = new ConsoleLogger(LogType.Info);

            CommandHandlerManager commandHandler = new CommandHandlerManager(
                server,
                logger);

            commandHandler.RegistrationCommand<TestServerCommandOne>(1);

            Type type = commandHandler.ServerCommandsMap[1];

            Assert.NotNull(type);
            Assert.NotNull(commandHandler.ServerCommandsMap);
            Assert.NotEmpty(commandHandler.ServerCommandsMap);
        }

        [Fact]
        public void RegistrationCommandInvalidOperationExceptionAlreadyExistType()
        {
            Assert.Throws<InvalidOperationException>(() => 
            {
                IServer server = new Server();
                ILogger logger = new ConsoleLogger(LogType.Info);

                CommandHandlerManager commandHandler = new CommandHandlerManager(
                    server,
                    logger);

                commandHandler.RegistrationCommand<TestServerCommandOne>(1);
                commandHandler.RegistrationCommand<TestServerCommandOne>(2);
            });
        }

        [Fact]
        public void RegistrationCommandInvalidOperationExceptionAlreadyExistId()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                IServer server = new Server();
                ILogger logger = new ConsoleLogger(LogType.Info);

                CommandHandlerManager commandHandler = new CommandHandlerManager(
                    server,
                    logger);

                commandHandler.RegistrationCommand<TestServerCommandOne>(1);
                commandHandler.RegistrationCommand<TestServerCommandTwo>(1);
            });
        }

        [Fact]
        public void CheckCommandExistInPool()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IClient client = new Client(socket);
            ICommandPool<ushort, IServerCommand> commandServerPool = new CommandPool<ushort, IServerCommand>();
            IReadOnlyCollection<IManager> managers = new List<IManager>();
            IDictionary<ushort, Type> commandMap = new Dictionary<ushort, Type>()
            {
                { 
                    1, 
                    typeof(TestServerCommandOne) 
                }
            };

            IServerCommand commandFirst = commandServerPool.GetCommand(
                1, 
                commandMap);

            commandFirst.Execute(
                client,
                new byte[1]);

            commandServerPool.ReturnCommandToPool(
                1,
                commandFirst,
                commandMap);

            IServerCommand commandSecond = commandServerPool.GetCommand(
                1,
                commandMap);

            Assert.Equal(
                commandFirst, 
                commandSecond);
        }
    }
}