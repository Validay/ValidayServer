using System;
using System.Collections.Concurrent;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Tracks the number of packets received per client per second and optionally disconnects
    /// clients that exceed the configured limit.
    /// Self-registers into the server in its constructor.
    /// </summary>
    public class RateLimitManager : IManager
    {
        /// <inheritdoc/>
        public string Name => nameof(RateLimitManager);

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <summary>Overridable clock source for unit-testing purposes.</summary>
        protected virtual DateTime Now => DateTime.UtcNow;

        private readonly IServer _server;
        private readonly ILogger _logger;
        private readonly int _maxPacketsPerSecond;
        private readonly bool _disconnectOnExceed;

        private readonly ConcurrentDictionary<IClient, ClientRateInfo> _rates =
            new ConcurrentDictionary<IClient, ClientRateInfo>();

        /// <summary>
        /// Creates the manager and self-registers it with the server.
        /// </summary>
        /// <param name="server">Server instance (must not be null).</param>
        /// <param name="logger">Logger (must not be null).</param>
        /// <param name="maxPacketsPerSecond">Maximum packets allowed per client per second.</param>
        /// <param name="disconnectOnExceed">When <c>true</c>, exceeding clients are disconnected.</param>
        public RateLimitManager(
            IServer server,
            ILogger logger,
            int maxPacketsPerSecond,
            bool disconnectOnExceed = false)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server),
                    $"{nameof(RateLimitManager)}: server is null!");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger),
                    $"{nameof(RateLimitManager)}: logger is null!");

            if (maxPacketsPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPacketsPerSecond),
                    $"{nameof(RateLimitManager)}: maxPacketsPerSecond must be positive.");

            _server = server;
            _logger = logger;
            _maxPacketsPerSecond = maxPacketsPerSecond;
            _disconnectOnExceed = disconnectOnExceed;

            _server.RegistrationManager(this);
        }

        /// <inheritdoc/>
        public void Start()
        {
            IsActive = true;

            _server.OnClientConnected += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;
            _server.OnReceivedData += OnDataReceived;

            _logger.Log($"{nameof(RateLimitManager)} started!", LogType.Info);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            IsActive = false;

            _server.OnClientConnected -= OnClientConnected;
            _server.OnClientDisconnected -= OnClientDisconnected;
            _server.OnReceivedData -= OnDataReceived;

            _logger.Log($"{nameof(RateLimitManager)} stopped!", LogType.Info);
        }

        // ─── Event handlers ──────────────────────────────────────────────────────

        private void OnClientConnected(IClient client)
        {
            _rates[client] = new ClientRateInfo { Count = 0, WindowStart = Now };
        }

        private void OnClientDisconnected(IClient client)
        {
            _rates.TryRemove(client, out _);
        }

        private void OnDataReceived(IClient client, byte[] rawData)
        {
            if (!_rates.TryGetValue(client, out ClientRateInfo? info))
                return;

            lock (info)
            {
                if ((Now - info.WindowStart).TotalSeconds >= 1.0)
                {
                    info.Count = 0;
                    info.WindowStart = Now;
                }

                info.Count++;

                if (info.Count > _maxPacketsPerSecond)
                {
                    _logger.Log(
                        $"Rate limit exceeded by [{client.Ip}:{client.Port}]: " +
                        $"{info.Count}/{_maxPacketsPerSecond}",
                        LogType.Warning);

                    if (_disconnectOnExceed)
                        _server.DisconnectClient(client);
                }
            }
        }

        // ─── Inner type ──────────────────────────────────────────────────────────

        private sealed class ClientRateInfo
        {
            public int Count { get; set; }
            public DateTime WindowStart { get; set; }
        }
    }
}
