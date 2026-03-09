using ValidayServer.Network.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace ValidayServer.Network
{
    /// <summary>
    /// Represents a client connected to the server.
    /// Socket is intentionally not part of IClient — it is an implementation detail
    /// accessed only by Server internals via the concrete type.
    /// </summary>
    public class Client : IClient
    {
        /// <summary>
        /// The underlying socket.
        /// Public so the class compiles, but intentionally absent from IClient —
        /// treat it as an implementation detail and avoid using it outside of Server internals.
        /// </summary>
        public Socket Socket { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Ip { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IClientSession Session { get; } = new ClientSession();

        /// <summary>
        /// Creates a Client wrapping an accepted socket.
        /// </summary>
        public Client(Socket socket)
        {
            Socket = socket;

            IPEndPoint? endPoint = socket.RemoteEndPoint as IPEndPoint;

            if (endPoint != null)
            {
                Ip = endPoint.Address.ToString();
                Port = endPoint.Port;
            }
            else
            {
                Ip = "Unknown";
                Port = 0;
            }
        }
    }
}