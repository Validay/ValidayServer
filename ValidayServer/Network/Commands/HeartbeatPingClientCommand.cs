using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Framing.Interfaces;

namespace ValidayServer.Network.Commands
{
    /// <summary>
    /// A ping packet sent by the server to detect dead connections.
    /// Clients should respond with a pong packet (commandId = <see cref="Managers.HeartbeatManager.DefaultPongCommandId"/>).
    /// </summary>
    internal sealed class HeartbeatPingClientCommand : IClientCommand
    {
        private readonly ushort _commandId;
        private readonly IPacketFramer? _framer;

        internal HeartbeatPingClientCommand(ushort commandId, IPacketFramer? framer)
        {
            _commandId = commandId;
            _framer = framer;
        }

        /// <inheritdoc/>
        public byte[] GetRawData()
        {
            byte[] body = new PacketWriter(_commandId).Build();
            return _framer?.Frame(body) ?? body;
        }
    }
}
