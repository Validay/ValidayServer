namespace ValidayServer.Network.Interfaces
{
    /// <summary>
    /// Interface for client connected to server.
    /// Does not expose Socket — it is an implementation detail of the concrete Client class.
    /// </summary>
    public interface IClient
    {
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