using ValidayServer.Network;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;
using ValidayServerSample.Managers;

namespace ValidayServerSample
{
    public class Program
    {
        public static IServer? server;
        public static Timer? timer;

        static void Main(string[] args)
        {
            server = new Server();
            timer = new Timer(
                SendTestData, 
                null, 
                0, 
                1000);

            server.RegistrationManager<CommandHandlerManager>();
            server.RegistrationManager<ConnectionCheckManager>();
            server.RegistrationManager<ConsoleInfoManager>();

            server.Start();

            while (server.IsRun)
            { };
        }

        private static void SendTestData(object? state)
        {
            if (server == null)
                return;

            foreach (IClient client in server.GetAllConnections())
            {
                byte[] rawData = new byte[2]
                {
                    1,
                    6
                };

                server?.SendToClient(client, rawData);
            }
        }
    }
}