using ValidayServer.Network;
using ValidayServer.Network.Commands.Interfaces;

namespace ValidayServerSample.Network.Commands.ClientCommands
{
    /// <summary>
    /// Sends a simple text message from the server to a client.
    /// Packet format (body after command ID): [string: message]
    /// </summary>
    public class SimpleMessageClientCommand : IClientCommand
    {
        private const ushort CommandId = 1;

        public string Message { get; set; } = string.Empty;

        public byte[] GetRawData()
        {
            return new PacketWriter(CommandId)
                .WriteString(Message)
                .BuildFramed();
        }
    }
}
