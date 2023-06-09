using Xunit;
using Validay.Network.Interfaces;
using Validay.Network.Commands.Interfaces;
using Validay.Managers.Interfaces;
using Validay.Network;

namespace ValidayServerTest
{
    public class ServerCommandTests
    {
        class TestServerCommandOne : IServerCommand
        {
            public void Execute(
                IClient sender,
                IReadOnlyCollection<IManager> managers,
                byte[] rawData)
            {
            }
        }

        class TestServerCommandTwo : IServerCommand
        {
            public void Execute(
                IClient sender,
                IReadOnlyCollection<IManager> managers,
                byte[] rawData)
            {
            }
        }

        [Fact]
        public void RegistrationCommandSuccess()
        {
            IServer server = new Server();

            server.RegistrationCommand<TestServerCommandOne>(1);

            Type type = server.ServerCommandsMap[1];

            Assert.NotNull(type);
            Assert.NotNull(server.ServerCommandsMap);
            Assert.NotEmpty(server.ServerCommandsMap);
        }

        [Fact]
        public void RegistrationCommandInvalidOperationExceptionAlreadyExistType()
        {
            Assert.Throws<InvalidOperationException>(() => 
            {
                IServer server = new Server();

                server.RegistrationCommand<TestServerCommandOne>(1);
                server.RegistrationCommand<TestServerCommandOne>(2);
            });
        }

        [Fact]
        public void RegistrationCommandInvalidOperationExceptionAlreadyExistId()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                IServer server = new Server();

                server.RegistrationCommand<TestServerCommandOne>(1);
                server.RegistrationCommand<TestServerCommandTwo>(1);
            });
        }
    }
}