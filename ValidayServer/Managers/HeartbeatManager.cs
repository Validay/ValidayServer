using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network;
using ValidayServer.Network.Commands;
using ValidayServer.Network.Framing.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Periodically sends ping packets to all connected clients and disconnects any client
    /// that does not respond within the configured timeout.
    /// Self-registers into the server in its constructor.
    /// </summary>
    public class HeartbeatManager : IManager
    {
        /// <summary>Default command ID used for the outgoing ping packet.</summary>
        public const ushort DefaultPingCommandId = 0xFFFE;

        /// <summary>Default command ID expected from the client as a pong response.</summary>
        public const ushort DefaultPongCommandId = 0xFFFF;

        /// <inheritdoc/>
        public string Name => nameof(HeartbeatManager);

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <summary>Overridable clock source for unit-testing purposes.</summary>
        protected virtual DateTime Now => DateTime.UtcNow;

        private readonly IServer _server;
        private readonly ILogger _logger;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _timeout;
        private readonly ushort _pingCommandId;
        private readonly ushort _pongCommandId;
        private readonly IPacketFramer? _framer;

        private readonly ConcurrentDictionary<IClient, DateTime> _lastPong =
            new ConcurrentDictionary<IClient, DateTime>();

        private Timer? _timer;

        /// <summary>
        /// Creates the manager and self-registers it with the server.
        /// </summary>
        /// <param name="server">Server instance (must not be null).</param>
        /// <param name="logger">Logger (must not be null).</param>
        /// <param name="interval">How often to send pings / check for timeouts.</param>
        /// <param name="timeout">How long without a pong before a client is disconnected.</param>
        /// <param name="pingCommandId">Command ID written into outgoing ping packets.</param>
        /// <param name="pongCommandId">Command ID that identifies an incoming pong packet.</param>
        /// <param name="framer">Optional framer applied to outgoing ping packets.</param>
        public HeartbeatManager(
            IServer server,
            ILogger logger,
            TimeSpan interval,
            TimeSpan timeout,
            ushort pingCommandId = DefaultPingCommandId,
            ushort pongCommandId = DefaultPongCommandId,
            IPacketFramer? framer = null)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server),
                    $"{nameof(HeartbeatManager)}: server is null!");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger),
                    $"{nameof(HeartbeatManager)}: logger is null!");

            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval),
                    $"{nameof(HeartbeatManager)}: interval must be positive.");

            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout),
                    $"{nameof(HeartbeatManager)}: timeout must be positive.");

            _server = server;
            _logger = logger;
            _interval = interval;
            _timeout = timeout;
            _pingCommandId = pingCommandId;
            _pongCommandId = pongCommandId;
            _framer = framer;

            _server.RegistrationManager(this);
        }

        /// <inheritdoc/>
        public void Start()
        {
            IsActive = true;

            _server.OnClientConnected += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;
            _server.OnReceivedData += OnDataReceived;

            _timer = new Timer(
                _ => CheckHeartbeats(),
                null,
                _interval,
                _interval);

            _logger.Log($"{nameof(HeartbeatManager)} started!", LogType.Info);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            IsActive = false;

            _timer?.Dispose();
            _timer = null;

            _server.OnClientConnected -= OnClientConnected;
            _server.OnClientDisconnected -= OnClientDisconnected;
            _server.OnReceivedData -= OnDataReceived;

            _logger.Log($"{nameof(HeartbeatManager)} stopped!", LogType.Info);
        }

        // ─── Event handlers ──────────────────────────────────────────────────────

        private void OnClientConnected(IClient client)
        {
            _lastPong[client] = Now;
        }

        private void OnClientDisconnected(IClient client)
        {
            _lastPong.TryRemove(client, out _);
        }

        private void OnDataReceived(IClient client, byte[] rawData)
        {
            if (rawData != null
                && rawData.Length >= 2
                && BitConverter.ToUInt16(rawData, 0) == _pongCommandId)
            {
                _lastPong[client] = Now;
            }
        }

        // ─── Heartbeat tick ──────────────────────────────────────────────────────

        /// <summary>
        /// Checks all tracked clients: pings those within the timeout window and
        /// disconnects those that have exceeded it.
        /// <c>protected</c> so that test subclasses can invoke it directly without waiting for the timer.
        /// </summary>
        protected void CheckHeartbeats()
        {
            DateTime now = Now;
            var ping = new HeartbeatPingClientCommand(_pingCommandId, _framer);

            foreach (KeyValuePair<IClient, DateTime> kvp in _lastPong.ToArray())
            {
                if (now - kvp.Value > _timeout)
                {
                    _logger.Log(
                        $"[{kvp.Key.Ip}:{kvp.Key.Port}] heartbeat timeout, disconnecting.",
                        LogType.Warning);

                    _server.DisconnectClient(kvp.Key);
                }
                else
                {
                    _server.SendToClient(kvp.Key, ping);
                }
            }
        }
    }
}
