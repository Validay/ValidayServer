using ValidayServer.Network;
using ValidayServer.Network.Framing;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Logging;
using ValidayServerSample.Managers;
using ValidayServerSample.Network.Commands.ServerCommands;
using ValidayServerSample.Network.Commands.ClientCommands;

namespace ValidayServerSample
{
    public class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger(LogType.Info);

            // Enable length-prefix framing so packets are correctly reassembled
            // even when TCP fragments or coalesces them.
            ServerSettings settings = ServerSettings.Default;
            settings.Framer = new LengthPrefixFramer();

            IServer server = new Server(settings, suppressSocketErrors: false);

            new ConsoleInfoManager(server, logger);

            CommandHandlerManager commandHandler = new CommandHandlerManager(server, logger);
            commandHandler.RegistrationCommand<SimpleMessageServerCommand>(1);

            // Echo every received message back to ALL connected clients.
            server.OnReceivedData += (sender, rawData) =>
            {
                var echo = new SimpleMessageClientCommand
                {
                    Message = $"[Echo from {sender.Ip}:{sender.Port}] " +
                              new ValidayServer.Network.PacketReader(rawData)
                                  .SkipCommandId()
                                  .ReadString()
                };

                server.BroadcastExcept(echo, sender);
            };

            server.Start();

            Console.WriteLine("Server running. Press Enter to stop.");
            Console.ReadLine();

            server.Stop();
        }
    }
}
