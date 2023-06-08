using Validay.Network.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace Validay.Network
{
    /// <summary>
    /// Base class client
    /// </summary>
    public class Client : IClient
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Socket Socket => _socket;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Ip => _ip;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int Port => _port;

        private Socket _socket;
        private string _ip;
        private int _port;

        /// <summary>
        /// Base constructor for client
        /// </summary>
        /// <param name="socket">Socket for this client</param>
        public Client(Socket socket)
        {
            IPEndPoint endPoint = (IPEndPoint)socket.RemoteEndPoint;

            _socket = socket;
            _ip = endPoint.Address.ToString();
            _port = endPoint.Port;
        }
    }
}
