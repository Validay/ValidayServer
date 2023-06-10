using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Interfaces;
using System;
using System.Threading;
using System.Net.Sockets;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Manager for checking connection all clients
    /// </summary>
    public class ConnectionCheckManager : IManager
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Name { get => nameof(ConnectionCheckManager); }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsActive { get; set; }

        private int _heartbeatIntervalSeconds = 10;
        private Timer? _heartbeatTimer;
        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ConnectionCheckManager()
            : this(10)
        { }

        /// <summary>
        /// Constructor with explicit parameters
        /// </summary>
        /// <param name="heartbeatIntervalSeconds">Connection check interval</param>
        public ConnectionCheckManager(int heartbeatIntervalSeconds)
        {
            _heartbeatIntervalSeconds = heartbeatIntervalSeconds;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Initialize(
            IServer server,
            ILogger logger)
        {
            _server = server;
            _logger = logger;

            if (_server == null)
                throw new NullReferenceException($"{nameof(ConnectionCheckManager)}: Server is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(ConnectionCheckManager)}: Logger is null!");
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Start()
        {
            if (_server == null)
            {
                _logger?.Log(
                    $"{nameof(ConnectionCheckManager)}: _server is null!",
                    LogType.Warning);

                return;
            }

            IsActive = true;

            _heartbeatTimer = new Timer(
                CheckSocketList, 
                null, 
                TimeSpan.Zero, 
                TimeSpan.FromSeconds(_heartbeatIntervalSeconds));

            _logger?.Log(
                $"{nameof(ConnectionCheckManager)} started!",
                LogType.Info);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Stop()
        {
            if (_server == null)
                return;

            IsActive = false;

            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;

            _logger?.Log(
                 $"{nameof(ConnectionCheckManager)} stopped!",
                LogType.Info);
        }

        private void CheckSocketList(object? state)
        {
            var clientList = _server?.GetAllConnections();

            if (clientList == null)
                return;

            for (int i = clientList.Count - 1; i >= 0; i--)
                if (!IsSocketConnected(clientList[i].Socket))
                    _server?.DisconnectClient(clientList[i]);
        }

        private bool IsSocketConnected(Socket socket)
        {
            if (socket != null 
                && socket.Connected)
            {
                try
                {
                    byte[] heartbeatData = new byte[2];

                    socket.Send(heartbeatData);

                    return true;
                }
                catch (Exception exception)
                {
                    _logger?.Log(
                        $"{socket} error! {exception.Message}",
                        LogType.Low);

                    return false;
                }
            }

            return false;
        }
    }
}