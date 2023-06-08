using System;
using System.Net;
using System.Net.Sockets;
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
        /// Logger for server
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

        /// <summary>
        /// Default constructor server settings
        /// </summary>
        /// <param name="ip">Ip address server</param>
        /// <param name="port">Port server</param>
        /// <param name="connectingClientQueue">Maximum number of expected connections</param>
        /// <param name="bufferSize">Buffer size</param>
        /// <param name="maxConnections">Maximum client connections</param>
        /// <param name="clientFactory">Factory for creating clients</param>
        /// <param name="managerFactory">Factory for creating managers</param>
        /// <param name="logger">Logger for server</param>
        /// <exception cref="FormatException">Invalid parameters</exception>
        public ServerSettings(
            string ip,
            int port,
            int connectingClientQueue,
            int bufferSize,
            int maxConnections,
            IClientFactory clientFactory,
            IManagerFactory managerFactory,
            ILogger logger)
        {
            if (bufferSize < 0
                || connectingClientQueue < 0
                || maxConnections < 0
                || clientFactory == null
                || managerFactory == null
                || logger == null
                || port < 0
                || port > 65535
                || !IsValidIpAddress(ip))
                throw new FormatException($"{nameof(ServerSettings)} create failed! Invalid parameters");

            Ip = ip;
            Port = port;
            ConnectingClientQueue = connectingClientQueue;
            BufferSize = bufferSize;
            MaxConnection = maxConnections;
            ClientFactory = clientFactory;
            ManagerFactory = managerFactory;
            Logger = logger;              
        }

        private static bool IsValidIpAddress(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out IPAddress parsedIpAddress))
            {
                if (parsedIpAddress.AddressFamily == AddressFamily.InterNetwork 
                    || parsedIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    return true;
            }

            return false;
        }
    }
}