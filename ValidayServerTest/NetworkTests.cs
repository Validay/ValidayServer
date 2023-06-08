using Xunit;
using Validay.Network;
using Validay.Managers;
using Validay.Network.Interfaces;
using Validay.Logging;

namespace ValidayServerTest
{
    public class NetworkTests
    {
        [Fact]
        public void CreateDefaultServerSuccess()
        {
            IServer server = new Server();

            Assert.Empty(server.Managers);

            server.RegistrationManager<CommandHandlerManager>();

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
                new ClientFactory(),
                new ManagerFactory(),
                new ConsoleLogger(LogType.Info));

            IServer server = new Server(
                serverSettings, 
                true);

            Assert.Empty(server.Managers);

            server.RegistrationManager<CommandHandlerManager>();

            Assert.NotNull(server);
            Assert.NotEmpty(server.Managers);
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
                    new ClientFactory(),
                    new ManagerFactory(),
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
                    new ClientFactory(),
                    new ManagerFactory(),
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
                    new ClientFactory(),
                    new ManagerFactory(),
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
                    new ClientFactory(),
                    new ManagerFactory(),
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
                    new ClientFactory(),
                    new ManagerFactory(),
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
                    new ClientFactory(),
                    new ManagerFactory(),
                    new ConsoleLogger(LogType.Info));
            });
        }
    }
}