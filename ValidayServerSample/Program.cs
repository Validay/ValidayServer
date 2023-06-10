using ValidayServer.Network;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;
using ValidayServerSample.Managers;
using ValidayServerSample.Network.Commands.ServerCommand;

namespace ValidayServerSample
{
    public class Program
    {
        static void Main(string[] args)
        {
            IServer server = new Server();
            CommandHandlerManager commandHandler = new CommandHandlerManager();

            commandHandler.RegistrationCommand<SimpleMessageCommand>(1);

            server.RegistrationManager(commandHandler);
            server.RegistrationManager<ConnectionCheckManager>();
            server.RegistrationManager<ConsoleInfoManager>();

            server.Start();

            while (server.IsRun)
            { };
        }
    }
}