using Xunit;
using ValidayServer.Network.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Managers;

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
            CommandHandlerManager commandHandler = new CommandHandlerManager();

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
                CommandHandlerManager commandHandler = new CommandHandlerManager();

                commandHandler.RegistrationCommand<TestServerCommandOne>(1);
                commandHandler.RegistrationCommand<TestServerCommandOne>(2);
            });
        }

        [Fact]
        public void RegistrationCommandInvalidOperationExceptionAlreadyExistId()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                CommandHandlerManager commandHandler = new CommandHandlerManager();

                commandHandler.RegistrationCommand<TestServerCommandOne>(1);
                commandHandler.RegistrationCommand<TestServerCommandTwo>(1);
            });
        }
    }
}