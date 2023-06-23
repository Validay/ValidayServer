using ValidayServer.Network;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;
using ValidayServerSample.Managers;
using ValidayServerSample.Network.Commands.ServerCommands;

namespace ValidayServerSample
{
    public class Program
    {
        static void Main(string[] args)
        {
            IServer server = new Server();
            CommandHandlerManager commandHandler = new CommandHandlerManager();

            commandHandler.RegistrationCommand<SimpleMessageServerCommand>(1);

            server.RegistrationManager(commandHandler);
            server.RegistrationManager<BadPacketDefenderManager>();
            server.RegistrationManager<ConsoleInfoManager>();

            server.Start();

            while (server.IsRun)
            { };
        }
    }
}