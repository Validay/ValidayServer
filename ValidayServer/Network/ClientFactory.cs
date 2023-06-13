using ValidayServer.Network.Interfaces;
using System.Net.Sockets;

namespace ValidayServer.Network
{
    /// <summary>
    /// Base class client factory
    /// </summary>
    public class ClientFactory : IClientFactory
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IClient CreateClient(Socket socket)
        {
            return new Client(socket);
        }
    }
}