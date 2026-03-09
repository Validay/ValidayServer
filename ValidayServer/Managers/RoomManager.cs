using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Manages the lifecycle of named rooms, including auto-removal of clients on disconnect
    /// and optional destruction of empty rooms.
    /// Self-registers into the server in its constructor.
    /// </summary>
    public class RoomManager : IManager, IRoomManager
    {
        /// <inheritdoc/>
        public string Name => nameof(RoomManager);

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyCollection<IRoom> Rooms
        {
            get
            {
                // Return a snapshot cast to IRoom.
                return _rooms.Values.Cast<IRoom>().ToList().AsReadOnly();
            }
        }

        private readonly ConcurrentDictionary<string, Room> _rooms =
            new ConcurrentDictionary<string, Room>();

        private readonly IServer _server;
        private readonly ILogger _logger;
        private readonly bool _destroyEmptyRooms;

        /// <summary>
        /// Creates the manager and self-registers it with the server.
        /// </summary>
        /// <param name="server">Server instance (must not be null).</param>
        /// <param name="logger">Logger (must not be null).</param>
        /// <param name="destroyEmptyRooms">When <c>true</c>, empty rooms are automatically destroyed when a client disconnects.</param>
        public RoomManager(
            IServer server,
            ILogger logger,
            bool destroyEmptyRooms = true)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server),
                    $"{nameof(RoomManager)}: server is null!");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger),
                    $"{nameof(RoomManager)}: logger is null!");

            _server = server;
            _logger = logger;
            _destroyEmptyRooms = destroyEmptyRooms;

            _server.RegistrationManager(this);
        }

        /// <inheritdoc/>
        public void Start()
        {
            IsActive = true;
            _server.OnClientDisconnected += OnClientDisconnected;
            _logger.Log($"{nameof(RoomManager)} started!", LogType.Info);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            IsActive = false;
            _server.OnClientDisconnected -= OnClientDisconnected;
            _logger.Log($"{nameof(RoomManager)} stopped!", LogType.Info);
        }

        /// <inheritdoc/>
        public IRoom Create(string id)
        {
            var room = new Room(id, _server);

            if (!_rooms.TryAdd(id, room))
                throw new InvalidOperationException(
                    $"{nameof(RoomManager)}: a room with id '{id}' already exists.");

            return room;
        }

        /// <inheritdoc/>
        public IRoom? Get(string id)
        {
            _rooms.TryGetValue(id, out Room? room);
            return room;
        }

        /// <inheritdoc/>
        public IRoom GetOrCreate(string id)
        {
            return _rooms.GetOrAdd(id, key => new Room(key, _server));
        }

        /// <inheritdoc/>
        public bool TryGet(string id, out IRoom? room)
        {
            bool found = _rooms.TryGetValue(id, out Room? concrete);
            room = concrete;
            return found;
        }

        /// <inheritdoc/>
        public bool Destroy(string id)
        {
            return _rooms.TryRemove(id, out _);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IRoom> GetRoomsOf(IClient client)
        {
            return _rooms.Values
                .Where(r => r.Contains(client))
                .Cast<IRoom>()
                .ToList()
                .AsReadOnly();
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void OnClientDisconnected(IClient client)
        {
            foreach (var kvp in _rooms.ToList())
            {
                kvp.Value.RemoveClient(client);

                if (_destroyEmptyRooms && kvp.Value.IsEmpty)
                    _rooms.TryRemove(kvp.Key, out _);
            }
        }
    }
}
