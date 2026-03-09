using System;
using ValidayServer.Network;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServerSample.Network.Commands.ServerCommands
{
    /// <summary>
    /// Handles a simple text message sent by a client.
    /// Packet format (body after command ID): [string: message]
    /// </summary>
    public class SimpleMessageServerCommand : IServerCommand
    {
        public void Execute(IClient sender, byte[] rawData)
        {
            var reader = new PacketReader(rawData).SkipCommandId();
            string message = reader.ReadString();

            Console.WriteLine($"[{sender.Ip}:{sender.Port}] → \"{message}\"");
        }
    }
}
