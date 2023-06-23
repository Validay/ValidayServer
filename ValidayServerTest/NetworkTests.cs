using Xunit;
using ValidayServer.Network;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;

namespace ValidayServerTest
{
    public class NetworkTests
    {
        [Fact]
        public void CreateDefaultServerSuccess()
        {
            IServer server = new Server();
            ILogger logger = new ConsoleLogger(LogType.Info);

            CommandHandlerManager commandHandler = new CommandHandlerManager(
                server,
                logger);

            Assert.NotNull(server);
            Assert.NotEmpty(server.Managers);
        }

        [Fact]
        public void CreateCustomServerSuccess()
        {
            ServerSettings serverSettings = new ServerSettings(
                "127.0.0.1",
                8888,
                10,
                1024,
                100,
                64,
                new byte[1],
                new ClientFactory(),
                new ConsoleLogger(LogType.Info));

            ILogger logger = new ConsoleLogger(LogType.Info);
            IServer server = new Server(
                serverSettings, 
                true);

            Assert.Empty(server.Managers);

            CommandHandlerManager commandHandler = new CommandHandlerManager(
                server,
                logger);

            Assert.NotNull(server);
            Assert.NotEmpty(server.Managers);
        }

        [Fact]
        public void RegistrationManagerInvalidOperationExceptionAlreadyExistType()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                IServer server = new Server();
                ILogger logger = new ConsoleLogger(LogType.Info);

                CommandHandlerManager commandHandlerOne = new CommandHandlerManager(
                    server,
                    logger);

                CommandHandlerManager commandHandlerTwo = new CommandHandlerManager(
                    server,
                    logger);
            });
        }

        [Fact]
        public void CreateServerSettingsInvalidParameters()
        {
            Assert.Throws<FormatException>(() =>
            {
                ServerSettings serverSettings = new ServerSettings(
                    "invalid ip",
                    8888,
                    10,
                    1024,
                    100,
                    64,
                    new byte[1],
                    new ClientFactory(),
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ServerSettings serverSettings = new ServerSettings(
                    "127.0.0.1",
                    100000,
                    10,
                    1024,
                    100,
                    64,
                    new byte[1],
                    new ClientFactory(),
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ServerSettings serverSettings = new ServerSettings(
                    "127.0.0.1",
                    -1,
                    10,
                    1024,
                    100,
                    64,
                    new byte[1],
                    new ClientFactory(),
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ServerSettings serverSettings = new ServerSettings(
                    "127.0.0.1",
                    8888,
                    -1,
                    1024,
                    100,
                    64,
                    new byte[1],
                    new ClientFactory(),
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ServerSettings serverSettings = new ServerSettings(
                    "127.0.0.1",
                    8888,
                    10,
                    -1,
                    100,
                    64,
                    new byte[1],
                    new ClientFactory(),
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ServerSettings serverSettings = new ServerSettings(
                    "127.0.0.1",
                    8888,
                    10,
                    1024,
                    -1,
                    64,
                    new byte[1],
                    new ClientFactory(),
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ServerSettings serverSettings = new ServerSettings(
                    "127.0.0.1",
                    8888,
                    10,
                    1024,
                    100,
                    -1,
                    new byte[1],
                    new ClientFactory(),
                    new ConsoleLogger(LogType.Info));
            });
        }
    }
}