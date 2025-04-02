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
            CommandHandlerManager _ = new CommandHandlerManager(
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

            CommandHandlerManager _ = new CommandHandlerManager(
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

        [Theory]
        [MemberData(nameof(InvalidParametersData))]
        public void CreateServerSettingsInvalidParameters(
            string ip,
            int port,
            int connectingClientQueue,
            int bufferSize,
            int maxConnections,
            int maxDepthReadPackage,
            byte[] markerStartPackage)
        {
            Assert.Throws<FormatException>(() =>
            {
                var serverSettings = new ServerSettings(
                    ip,
                    port,
                    connectingClientQueue,
                    bufferSize,
                    maxConnections,
                    maxDepthReadPackage,
                    markerStartPackage,
                    new ClientFactory(),
                    new ConsoleLogger(LogType.Info));
            });
        }

        public static IEnumerable<object[]> InvalidParametersData()
        {
            var validIp = "127.0.0.1";
            var validPort = 8888;
            var validConnectingClientQueue = 10;
            var validBufferSize = 1024;
            var validMaxConnections = 100;
            var validMaxDepthReadPackage = 64;
            var validMarkerStartPackage = new byte[1];

            yield return new object[] 
            { 
                "invalid ip",
                validPort, 
                validConnectingClientQueue,
                validBufferSize, 
                validMaxConnections, 
                validMaxDepthReadPackage,
                validMarkerStartPackage 
            };

            yield return new object[] 
            {
                validIp,
                100000,
                validConnectingClientQueue,
                validBufferSize,
                validMaxConnections,
                validMaxDepthReadPackage,
                validMarkerStartPackage
            };

            yield return new object[] 
            { 
                validIp,
                -1,
                validConnectingClientQueue,
                validBufferSize, 
                validMaxConnections,
                validMaxDepthReadPackage,
                validMarkerStartPackage 
            };

            yield return new object[] 
            { 
                validIp, 
                validPort, 
                -1,
                validBufferSize, 
                validMaxConnections,
                validMaxDepthReadPackage, 
                validMarkerStartPackage 
            };

            yield return new object[] 
            { 
                validIp,
                validPort,
                validConnectingClientQueue,
                -1,
                validMaxConnections, 
                validMaxDepthReadPackage, 
                validMarkerStartPackage };

            yield return new object[]
            { 
                validIp, 
                validPort,
                validConnectingClientQueue, 
                validBufferSize,             
                -1,
                validMaxDepthReadPackage,
                validMarkerStartPackage
            };

            yield return new object[] 
            { 
                validIp,
                validPort,
                validConnectingClientQueue,
                validBufferSize, 
                validMaxConnections, 
                -1,
                validMarkerStartPackage 
            };

            yield return new object[] 
            { 
                validIp,
                validPort,
                validConnectingClientQueue,
                validBufferSize, 
                validMaxConnections,
                validMaxDepthReadPackage,
                new byte[0] 
            };
        }
    }
}