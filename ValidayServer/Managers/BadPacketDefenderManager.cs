using System;
using System.Collections.Generic;
using System.Linq;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Manager that tracks bad packets per client and disconnects clients that exceed the threshold.
    /// A "bad packet" is any packet whose command ID is not registered, or whose data is too short
    /// to even contain a command ID.
    /// Depends on ICommandRegistry (not on CommandHandlerManager directly) to check known command IDs.
    /// </summary>
    public class BadPacketDefenderManager : IManager
    {
        /// <inheritdoc/>
        public string Name => nameof(BadPacketDefenderManager);

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        private readonly Dictionary<IClient, int> _badPacketCounts;
        private readonly int _threshold;
        private readonly IConverterId<ushort> _converterId;
        private readonly IServer? _server;
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates the manager with default threshold (10 bad packets) and UshortConverterId.
        /// </summary>
        public BadPacketDefenderManager(
            IServer server,
            ILogger logger)
            : this(server, logger, countBadPacketForDisconnect: 10, new UshortConverterId()) { }

        /// <summary>
        /// Creates the manager with explicit parameters.
        /// </summary>
        /// <param name="server">Server instance to subscribe to.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="countBadPacketForDisconnect">How many bad packets before a client is disconnected.</param>
        /// <param name="converterId">Converts raw bytes to a command ID.</param>
        public BadPacketDefenderManager(
            IServer server,
            ILogger logger,
            int countBadPacketForDisconnect,
            IConverterId<ushort> converterId)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server),
                    $"{nameof(BadPacketDefenderManager)}: server is null!");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger),
                    $"{nameof(BadPacketDefenderManager)}: logger is null!");

            _badPacketCounts = new Dictionary<IClient, int>();
            _threshold = countBadPacketForDisconnect;
            _converterId = converterId;
            _server = server;
            _logger = logger;

            _server.RegistrationManager(this);
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (_server == null)
            {
                _logger?.Log(
                    $"{nameof(BadPacketDefenderManager)}: server is null, cannot start.",
                    LogType.Warning);
                return;
            }

            IsActive = true;

            _server.OnClientConnected += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;
            _server.OnReceivedData += OnDataReceived;

            _logger?.Log($"{nameof(BadPacketDefenderManager)} started!", LogType.Info);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (_server == null)
                return;

            IsActive = false;

            _server.OnClientConnected -= OnClientConnected;
            _server.OnClientDisconnected -= OnClientDisconnected;
            _server.OnReceivedData -= OnDataReceived;

            _logger?.Log($"{nameof(BadPacketDefenderManager)} stopped!", LogType.Info);
        }

        // ─── private ────────────────────────────────────────────────────────────

        private void OnClientConnected(IClient client)
        {
            lock (_badPacketCounts)
                _badPacketCounts[client] = 0;
        }

        private void OnClientDisconnected(IClient client)
        {
            lock (_badPacketCounts)
                _badPacketCounts.Remove(client);
        }

        private void OnDataReceived(IClient client, byte[] rawData)
        {
            // Resolve the command registry via ICommandRegistry — no hard dependency on CommandHandlerManager.
            ICommandRegistry? registry = _server?.Managers
                .OfType<ICommandRegistry>()
                .FirstOrDefault();

            if (registry == null)
                return;

            lock (_badPacketCounts)
            {
                if (!_badPacketCounts.ContainsKey(client))
                    return; // Client already disconnected.

                bool isBad;

                if (rawData == null || rawData.Length < sizeof(ushort))
                {
                    // Too short to contain a command ID — inherently bad.
                    isBad = true;
                }
                else
                {
                    ushort commandId = _converterId.Convert(rawData);
                    isBad = !registry.CommandsMap.ContainsKey(commandId);
                }

                if (isBad)
                {
                    _badPacketCounts[client]++;

                    _logger?.Log(
                        $"Client [{client.Ip}:{client.Port}] sent bad packet. " +
                        $"Count: {_badPacketCounts[client]}/{_threshold}",
                        LogType.Low);
                }

                if (_badPacketCounts[client] >= _threshold)
                {
                    _logger?.Log(
                        $"Client [{client.Ip}:{client.Port}] exceeded bad packet threshold, disconnecting.",
                        LogType.Warning);

                    _server?.DisconnectClient(client);
                }
            }
        }
    }
}
