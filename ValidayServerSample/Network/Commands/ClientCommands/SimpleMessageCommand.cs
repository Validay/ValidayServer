using System.Text;
using ValidayServer.Network.Commands.Interfaces;

namespace ValidayServerSample.Network.Commands.ClientCommands
{
    public class SimpleMessageClientCommand : IClientCommand
    {
        public string Message { get; set; }

        private readonly ushort _idClientHandlerCommand;
        private readonly byte[] _markerStartPacket;

        public SimpleMessageClientCommand(
            ushort idClientHandlerCommand,
            byte[] markerStartPacket)
        {
            Message = string.Empty;
            _idClientHandlerCommand = idClientHandlerCommand;
            _markerStartPacket = markerStartPacket;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public byte[] GetRawData()
        {
            List<byte> data = new List<byte>();
            byte[] dataId = BitConverter.GetBytes(_idClientHandlerCommand);
            byte[] dataMessage = Encoding.UTF8.GetBytes(Message);

            data.AddRange(_markerStartPacket);
            data.AddRange(dataId);
            data.AddRange(dataMessage);

            return data.ToArray();
        }
    }
}