using System.Net.Sockets;

namespace ValidayServer.Network.Interfaces
{
    /// <summary>
    /// Interface for client factory
    /// </summary>
    public interface IClientFactory
    {
        /// <summary>
        /// Create new client
        /// </summary>
        /// <param name="socket">Socket client</param>
        /// <returns>New client</returns>
        IClient CreateClient(Socket socket);
    }
}
