using ValidayServer.Network;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;
using ValidayServerSample.Managers;

namespace ValidayServerSample
{
    public class Program
    {
        static void Main(string[] args)
        {
            IServer server = new Server();

            server.RegistrationManager<CommandHandlerManager>();
            server.RegistrationManager<ConnectionCheckManager>();
            server.RegistrationManager<BadPacketDefenderManager>();
            server.RegistrationManager<ConsoleInfoManager>();

            server.Start();

            while (server.IsRun)
            { };
        }
    }
}