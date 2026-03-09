using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Thread-safe implementation of <see cref="IRoom"/>.
    /// </summary>
    public sealed class Room : IRoom
    {
        private readonly List<IClient> _members = new List<IClient>();
        private readonly IServer _server;
        private readonly object _lock = new object();

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public IReadOnlyCollection<IClient> Members { get; }

        /// <inheritdoc/>
        public int Count
        {
            get { lock (_lock) return _members.Count; }
        }

        /// <inheritdoc/>
        public bool IsEmpty
        {
            get { lock (_lock) return _members.Count == 0; }
        }

        internal Room(string id, IServer server)
        {
            Id = id;
            _server = server;
            Members = new ReadOnlyCollection<IClient>(_members);
        }

        /// <inheritdoc/>
        public bool Join(IClient client)
        {
            lock (_lock)
            {
                if (_members.Contains(client))
                    return false;

                _members.Add(client);
                return true;
            }
        }

        /// <inheritdoc/>
        public bool Leave(IClient client)
        {
            lock (_lock)
                return _members.Remove(client);
        }

        /// <inheritdoc/>
        public bool Contains(IClient client)
        {
            lock (_lock)
                return _members.Contains(client);
        }

        /// <inheritdoc/>
        public void Broadcast(IClientCommand command)
        {
            IClient[] snapshot;
            lock (_lock)
                snapshot = _members.ToArray();

            _server.BroadcastTo(command, snapshot);
        }

        /// <inheritdoc/>
        public void BroadcastExcept(IClientCommand command, IClient exclude)
        {
            IClient[] snapshot;
            lock (_lock)
                snapshot = _members.Where(c => c != exclude).ToArray();

            _server.BroadcastTo(command, snapshot);
        }

        /// <summary>
        /// Called by <see cref="RoomManager"/> when a client disconnects.
        /// </summary>
        internal void RemoveClient(IClient client) => Leave(client);
    }
}
