using ValidayServer.Network;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;
using ValidayServerSample.Network.Commands.ServerCommands;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Logging;

namespace ValidayServerSample
{
    public class Program
    {
        static void Main(string[] args)
        {         
            IServer server = new Server();
            ILogger logger = new ConsoleLogger(LogType.Info);

            CommandHandlerManager commandHandler = new CommandHandlerManager(
                server, 
                logger);

            commandHandler.RegistrationCommand<SimpleMessageServerCommand>(1);

            server.Start();

            while (server.IsRun)
            { };
        }
    }
}