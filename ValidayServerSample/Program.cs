using Validay.Network;
using Validay.Managers;
using Validay.Network.Interfaces;
using ValidayServerSample.Managers;

namespace ValidayServerSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IServer server = new Server();

            server.RegistrationManager<CommandHandlerManager>();
            server.RegistrationManager<ConnectionCheckManager>();
            server.RegistrationManager<ConsoleInfoManager>();

            server.Start();

            while (server.IsRun)
            { };
        }
    }
}