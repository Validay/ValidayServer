using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Manager for checking connection all clients
    /// </summary>
    public class BadPacketDefenderManager : IManager
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Name { get => nameof(BadPacketDefenderManager); }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsActive { get; set; }

        private Dictionary<IClient, int> _countBadPacketClients;
        private int _countBadPacketForDisconnect;
        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BadPacketDefenderManager() 
            : this(10)
        { }

        /// <summary>
        /// Constructor with explicit parameters
        /// </summary>
        /// <param name="countBadPacketForDisconnect">Count bad packet for disconnect client</param>
        public BadPacketDefenderManager(int countBadPacketForDisconnect)
        {
            _countBadPacketClients = new Dictionary<IClient, int>();
            _countBadPacketForDisconnect = countBadPacketForDisconnect;
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
                throw new NullReferenceException($"{nameof(BadPacketDefenderManager)}: Server is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(BadPacketDefenderManager)}: Logger is null!");
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Start()
        {
            if (_server == null)
            {
                _logger?.Log(
                    $"{nameof(BadPacketDefenderManager)}: _server is null!",
                    LogType.Warning);

                return;
            }

            IsActive = true;

            _server.OnClientConnected += AddNewClient;
            _server.OnClientDisconnected += RemoveClient;
            _server.OnRecivedData += OnReceivedData;

            _logger?.Log(
                $"{nameof(BadPacketDefenderManager)} started!",
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

            _server.OnClientConnected -= AddNewClient;
            _server.OnClientDisconnected -= RemoveClient;
            _server.OnRecivedData -= OnReceivedData;

            _logger?.Log(
                 $"{nameof(BadPacketDefenderManager)} stopped!",
                LogType.Info);
        }

        private void AddNewClient(IClient client)
        {
            lock (_countBadPacketClients)
            {
                _countBadPacketClients.Add(client, 0);
            }
        }

        private void RemoveClient(IClient client)
        {
            lock (_countBadPacketClients)
            {
                _countBadPacketClients.Remove(client);
            }
        }

        private void OnReceivedData(
            IClient client, 
            byte[] rawData)
        {
            lock (_countBadPacketClients)
            {
                CommandHandlerManager? commandHandler = _server?.Managers
                .ToList()
                .FirstOrDefault(manager => manager.GetType() == typeof(CommandHandlerManager))
                    as CommandHandlerManager;

                if (commandHandler != null)
                {
                    ushort commandId = BitConverter.ToUInt16(rawData, 0);

                    if (!commandHandler.ServerCommandsMap.ContainsKey(commandId))
                    {
                        _countBadPacketClients[client]++;

                        _logger?.Log(
                           $"Client [{client.Ip}:{client.Port}] sended bad packet!" +
                           $"\nCommand [id = {commandId}] not founded!",
                           LogType.Low);
                    }

                    if (_countBadPacketClients[client] >= _countBadPacketForDisconnect)
                    {
                        _server?.DisconnectClient(client);

                        _logger?.Log(
                            $"Сlient [{client.Ip}:{client.Port}] exceeded the number of bad packets and was disconnected!",
                            LogType.Warning);
                    }
                }
            }
        }
    }
}