using System;
using System.Net;
using System.Net.Sockets;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Network.Framing;
using ValidayServer.Network.Framing.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network
{
    /// <summary>
    /// Server configuration.
    /// Declared as a class (not struct) because it contains reference-type fields
    /// (ILogger, IClientFactory, byte[]) — copying a struct would share those references silently.
    /// </summary>
    public class ServerSettings
    {
        /// <summary>
        /// IP address the server will bind to
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// Port the server will listen on
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Maximum number of pending connections in the accept queue
        /// </summary>
        public int ConnectingClientQueue { get; set; }

        /// <summary>
        /// Receive buffer size in bytes
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Maximum number of simultaneous client connections
        /// </summary>
        public int MaxConnection { get; set; }

        /// <summary>
        /// Maximum read depth for a single packet (framing guard)
        /// </summary>
        public int MaxDepthReadPacket { get; set; }

        /// <summary>
        /// Byte sequence that marks the beginning of a new packet
        /// </summary>
        public byte[] MarkerStartPacket { get; set; }

        /// <summary>
        /// Factory used to wrap accepted sockets into IClient instances
        /// </summary>
        public IClientFactory ClientFactory { get; set; }

        /// <summary>
        /// Logger used by the server
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Packet framing strategy used by the server.
        /// Defaults to <see cref="PassthroughFramer"/> (no framing — backward-compatible).
        /// Set to <see cref="LengthPrefixFramer"/> to enable length-prefix framing,
        /// which correctly handles TCP fragmentation and coalescing.
        /// </summary>
        public IPacketFramer Framer { get; set; } = new PassthroughFramer();

        /// <summary>
        /// Ready-to-use default settings (localhost:8888)
        /// </summary>
        public static ServerSettings Default => new ServerSettings(
            ip: "127.0.0.1",
            port: 8888,
            connectingClientQueue: 10,
            bufferSize: 1024,
            maxConnections: 100,
            maxDepthReadPacket: 64,
            markerStartPacket: new byte[] { 1, 2, 3 },
            clientFactory: new ClientFactory(),
            logger: new ConsoleLogger(LogType.Info));

        /// <summary>
        /// Creates and validates server settings.
        /// </summary>
        /// <exception cref="FormatException">Thrown when any parameter is out of range or invalid.</exception>
        public ServerSettings(
            string ip,
            int port,
            int connectingClientQueue,
            int bufferSize,
            int maxConnections,
            int maxDepthReadPacket,
            byte[] markerStartPacket,
            IClientFactory clientFactory,
            ILogger logger)
        {
            if (!IsValidIpAddress(ip))
                throw new FormatException($"{nameof(ServerSettings)}: invalid IP address '{ip}'.");

            if (port < 0 || port > 65535)
                throw new FormatException($"{nameof(ServerSettings)}: port must be 0–65535, got {port}.");

            if (connectingClientQueue < 0)
                throw new FormatException($"{nameof(ServerSettings)}: connectingClientQueue must be >= 0.");

            if (bufferSize <= 0)
                throw new FormatException($"{nameof(ServerSettings)}: bufferSize must be > 0.");

            if (maxConnections < 0)
                throw new FormatException($"{nameof(ServerSettings)}: maxConnections must be >= 0.");

            if (maxDepthReadPacket < 0)
                throw new FormatException($"{nameof(ServerSettings)}: maxDepthReadPacket must be >= 0.");

            if (markerStartPacket == null || markerStartPacket.Length == 0)
                throw new FormatException($"{nameof(ServerSettings)}: markerStartPacket must not be empty.");

            if (clientFactory == null)
                throw new ArgumentNullException(nameof(clientFactory));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            Ip = ip;
            Port = port;
            ConnectingClientQueue = connectingClientQueue;
            BufferSize = bufferSize;
            MaxConnection = maxConnections;
            MaxDepthReadPacket = maxDepthReadPacket;
            MarkerStartPacket = markerStartPacket;
            ClientFactory = clientFactory;
            Logger = logger;
        }

        private static bool IsValidIpAddress(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress parsed))
                return false;

            return parsed.AddressFamily == AddressFamily.InterNetwork
                || parsed.AddressFamily == AddressFamily.InterNetworkV6;
        }
    }
}