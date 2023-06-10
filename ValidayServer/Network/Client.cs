using ValidayServer.Network.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace ValidayServer.Network
{
    /// <summary>
    /// Base class client
    /// </summary>
    public class Client : IClient
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Ip { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Base constructor for client
        /// </summary>
        /// <param name="socket">Socket for this client</param>
        public Client(Socket socket)
        {
            IPEndPoint endPoint = (IPEndPoint)socket.RemoteEndPoint;

            Socket = socket;
            Ip = endPoint.Address.ToString();
            Port = endPoint.Port;
        }
    }
}
