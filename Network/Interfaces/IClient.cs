using System.Net.Sockets;

namespace Validay.Network.Interfaces
{
    /// <summary>
    /// Interface for client
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// Client socket
        /// </summary>
        Socket Socket { get; }   

        /// <summary>
        /// Client IP address
        /// </summary>
        string Ip { get; }

        /// <summary>
        /// Client port
        /// </summary>
        int Port { get; }   
    }
}
