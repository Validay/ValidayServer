using Validay.Logging;
using Validay.Logging.Interfaces;
using Validay.Managers;
using Validay.Managers.Interfaces;
using Validay.Network.Interfaces;

namespace Validay.Network
{
    /// <summary>
    /// Server settings
    /// </summary>
    public struct ServerSettings
    {
        /// <summary>
        /// Ip address server
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// Port server
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Maximum number of expected connections
        /// </summary>
        public int ConnectingClientQueue { get; set; }
        
        /// <summary>
        /// Buffer size
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Maximum client connections
        /// </summary>
        public int MaxConnection { get; set; }

        /// <summary>
        /// Factory for creating clients
        /// </summary>
        public IClientFactory ClientFactory { get; set; }

        /// <summary>
        /// Factory for creating managers
        /// </summary>
        public IManagerFactory ManagerFactory { get; set; }

        /// <summary>
        /// Factory for creating clients
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Default server settings
        /// </summary>
        public static ServerSettings Default => new ServerSettings
        {
            Ip = "127.0.0.1",
            BufferSize = 1024,
            MaxConnection = 100,
            Port = 8888,
            ConnectingClientQueue = 10,
            ClientFactory = new ClientFactory(),
            ManagerFactory = new ManagerFactory(),
            Logger = new ConsoleLogger(LogType.Info),
        };
    }
}