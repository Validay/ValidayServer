using System;
using System.Collections.Generic;
using System.Linq;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServerTest
{
    /// <summary>
    /// Minimal IServer fake for use across test classes.
    /// </summary>
    internal class FakeServer : IServer
    {
        public bool IsRun => false;
        public IReadOnlyCollection<IManager> Managers { get; private set; }

        private readonly List<IClient> _clients = new List<IClient>();

        public IReadOnlyCollection<IClient> ClientConnections => _clients.AsReadOnly();

        public event Action<IClient, byte[]> OnReceivedData = delegate { };
        public event Action<IClient, byte[]> OnSentData = delegate { };
        public event Action<IClient> OnClientConnected = delegate { };
        public event Action<IClient> OnClientDisconnected = delegate { };

        public IClient? LastDisconnected { get; private set; }
        public int DisconnectCallCount { get; private set; }

        public List<(IClient Client, IClientCommand Command)> SentCommands { get; } =
            new List<(IClient, IClientCommand)>();

        private readonly List<IManager> _managers = new List<IManager>();

        public FakeServer()
        {
            Managers = _managers.AsReadOnly();
        }

        public void RegistrationManager(IManager manager)
        {
            foreach (var m in _managers)
                if (m.Name == manager.Name)
                    throw new InvalidOperationException($"Manager [{manager.Name}] already registered.");
            _managers.Add(manager);
        }

        public void Start() { }
        public void Stop() { }

        public void SendToClient(IClient client, IClientCommand command)
        {
            SentCommands.Add((client, command));
        }

        public void Broadcast(IClientCommand command)
        {
            foreach (var c in _clients.ToList())
                SendToClient(c, command);
        }

        public void BroadcastExcept(IClientCommand command, IClient exclude)
        {
            foreach (var c in _clients.Where(c => c != exclude).ToList())
                SendToClient(c, command);
        }

        public void BroadcastTo(IClientCommand command, IEnumerable<IClient> targets)
        {
            foreach (var c in targets)
                SendToClient(c, command);
        }

        public void DisconnectClient(IClient client)
        {
            LastDisconnected = client;
            DisconnectCallCount++;
        }

        // ─── Client management helpers ────────────────────────────────────────────

        public void AddClient(IClient client) => _clients.Add(client);
        public void RemoveClient(IClient client) => _clients.Remove(client);

        // ─── Event simulation helpers ─────────────────────────────────────────────

        public void SimulateClientConnected(IClient client) => OnClientConnected(client);
        public void SimulateDataReceived(IClient client, byte[] data) => OnReceivedData(client, data);
        public void SimulateClientDisconnected(IClient client) => OnClientDisconnected(client);
    }

    /// <summary>
    /// Minimal IClient fake for use in isolation tests.
    /// </summary>
    internal class FakeClient : IClient
    {
        public string Ip { get; }
        public int Port { get; }
        public IClientSession Session { get; } = new ClientSession();

        public FakeClient(string ip = "127.0.0.1", int port = 9000)
        {
            Ip = ip;
            Port = port;
        }
    }
}
