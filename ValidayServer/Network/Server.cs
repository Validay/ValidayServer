using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Framing.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network
{
    /// <summary>
    /// Default TCP server implementation.
    /// Implements IDisposable because it owns a Socket.
    /// </summary>
    public class Server : IServer, IDisposable
    {
        /// <inheritdoc/>
        public bool IsRun => _isRunning;

        /// <inheritdoc/>
        public IReadOnlyCollection<IManager> Managers { get; }

        /// <inheritdoc/>
        public IReadOnlyCollection<IClient> ClientConnections { get; }

        /// <inheritdoc/>
        public event Action<IClient, byte[]> OnReceivedData = delegate { };

        /// <inheritdoc/>
        public event Action<IClient, byte[]> OnSentData = delegate { };

        /// <inheritdoc/>
        public event Action<IClient> OnClientConnected = delegate { };

        /// <inheritdoc/>
        public event Action<IClient> OnClientDisconnected = delegate { };

        private bool _isRunning;
        private bool _disposed;
        private bool _suppressSocketErrors;
        private string _ip;
        private int _port;
        private int _connectingClientQueue;
        private int _bufferSize;
        private Socket? _serverSocket;
        private readonly List<IClient> _clients;
        private readonly List<IManager> _managers;
        private ILogger _logger;
        private IClientFactory _clientFactory;
        private readonly IPacketFramer _framer;

        // Carries socket + pre-allocated buffer across async receive calls.
        private sealed class ReceiveState
        {
            public Socket Socket { get; }
            public byte[] Buffer { get; }

            public ReceiveState(Socket socket, byte[] buffer)
            {
                Socket = socket;
                Buffer = buffer;
            }
        }

        // Carries socket + client + data across async send calls.
        private sealed class SendState
        {
            public Socket Socket { get; }
            public IClient Client { get; }
            public byte[] Data { get; }

            public SendState(Socket socket, IClient client, byte[] data)
            {
                Socket = socket;
                Client = client;
                Data = data;
            }
        }

        /// <summary>
        /// Creates a server with default settings.
        /// </summary>
        public Server()
            : this(
                  ServerSettings.Default,
                  suppressSocketErrors: true)
        { }

        /// <summary>
        /// Creates a server with explicit settings.
        /// </summary>
        public Server(
            ServerSettings serverSettings,
            bool suppressSocketErrors)
        {
            _suppressSocketErrors = suppressSocketErrors;
            _ip = serverSettings.Ip;
            _port = serverSettings.Port;
            _connectingClientQueue = serverSettings.ConnectingClientQueue;
            _bufferSize = serverSettings.BufferSize;
            _logger = serverSettings.Logger;
            _clientFactory = serverSettings.ClientFactory;
            _clients = new List<IClient>();
            _managers = new List<IManager>();
            _framer = serverSettings.Framer;
            // ReadOnlyCollection wraps the list by reference — create once and reuse.
            Managers = new ReadOnlyCollection<IManager>(_managers);
            ClientConnections = new ReadOnlyCollection<IClient>(_clients);
        }

        /// <inheritdoc/>
        public virtual void RegistrationManager([NotNull] IManager manager)
        {
            if (_isRunning)
                throw new InvalidOperationException(
                    $"Cannot register manager [{manager.Name}] after the server has started.");

            bool alreadyExists = _managers.Any(m => m.Name == manager.Name);

            if (alreadyExists)
            {
                _logger?.Log(
                    $"Registration manager failed! Manager [{manager.Name}] already registered!",
                    LogType.Warning);

                throw new InvalidOperationException(
                    $"Registration manager failed! Manager [{manager.Name}] already registered!");
            }

            _managers.Add(manager);
        }

        /// <inheritdoc/>
        public virtual void Start()
        {
            try
            {
                _logger?.Log("Server starting...", LogType.Info);

                AddressFamily addressFamily = IPAddress.Parse(_ip).AddressFamily;

                _serverSocket = new Socket(
                    addressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                foreach (IManager manager in _managers)
                    manager.Start();

                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(_ip), _port);

                _serverSocket.Bind(endpoint);
                _serverSocket.Listen(_connectingClientQueue);
                _serverSocket.BeginAccept(OnClientConnect, null);

                _isRunning = true;

                _logger?.Log("Server started!", LogType.Info);
            }
            catch (Exception exception)
            {
                _logger?.Log($"Server start failed! {exception.Message}", LogType.CriticalError);
            }
        }

        /// <inheritdoc/>
        public virtual void Stop()
        {
            try
            {
                _logger?.Log("Server stopping...", LogType.Info);

                foreach (IManager manager in _managers)
                    manager.Stop();

                if (_serverSocket != null)
                {
                    _serverSocket.Close();
                    _serverSocket = null;
                    _isRunning = false;

                    _logger?.Log("Server stopped!", LogType.Info);
                }
                else
                {
                    _logger?.Log("Server already stopped!", LogType.Warning);
                }
            }
            catch (Exception exception)
            {
                _logger?.Log($"Server stop failed! {exception.Message}", LogType.CriticalError);
            }
        }

        /// <inheritdoc/>
        public virtual void SendToClient(
            IClient client,
            IClientCommand command)
        {
            Socket? socket = GetSocket(client);

            if (socket == null)
            {
                _logger?.Log(
                    $"SendToClient: cannot resolve socket for client [{client?.Ip}:{client?.Port}]. " +
                    "Custom IClient implementations must be of type Client.",
                    LogType.Warning);
                return;
            }

            try
            {
                byte[] rawData = command.GetRawData();

                socket.BeginSend(
                    rawData, 0, rawData.Length,
                    SocketFlags.None,
                    OnDataSent,
                    new SendState(socket, client, rawData));

                _logger?.Log(
                    $"Sending [{rawData.Length} bytes] to [{client.Ip}:{client.Port}]",
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_suppressSocketErrors)
                    _logger?.Log(exception.Message, LogType.Warning);
            }
        }

        /// <inheritdoc/>
        public virtual void Broadcast(IClientCommand command)
        {
            List<IClient> snapshot;
            lock (_clients)
                snapshot = new List<IClient>(_clients);

            foreach (IClient client in snapshot)
                SendToClient(client, command);
        }

        /// <inheritdoc/>
        public virtual void BroadcastExcept(IClientCommand command, IClient exclude)
        {
            List<IClient> snapshot;
            lock (_clients)
                snapshot = new List<IClient>(_clients);

            foreach (IClient client in snapshot)
                if (!ReferenceEquals(client, exclude))
                    SendToClient(client, command);
        }

        /// <inheritdoc/>
        public virtual void BroadcastTo(IClientCommand command, IEnumerable<IClient> targets)
        {
            foreach (IClient client in targets)
                SendToClient(client, command);
        }

        /// <inheritdoc/>
        public virtual void DisconnectClient([NotNull] IClient client)
        {
            OnClientDisconnect(client);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _serverSocket?.Dispose();
                _serverSocket = null;
            }
            catch { }
        }

        /// <summary>
        /// Returns the Socket from a concrete Client instance, or null for any other IClient.
        /// This is the single place in Server that knows about the concrete Client type.
        /// </summary>
        private static Socket? GetSocket(IClient client)
        {
            return (client as Client)?.Socket;
        }

        private void OnClientDisconnect(IClient client)
        {
            try
            {
                lock (_clients)
                {
                    if (!_clients.Contains(client))
                        return;

                    _clients.Remove(client);
                }

                _framer.RemoveClient(client);

                Socket? socket = GetSocket(client);
                socket?.Close();
                socket?.Dispose();

                OnClientDisconnected.Invoke(client);

                _logger?.Log(
                    $"Client [{client?.Ip}:{client?.Port}] disconnected!",
                    LogType.Info);
            }
            catch (Exception exception)
            {
                if (!_suppressSocketErrors)
                    _logger?.Log(exception.Message, LogType.Error);
            }
        }

        private void OnClientConnect(IAsyncResult asyncResult)
        {
            if (_serverSocket == null)
                return;

            try
            {
                Socket clientSocket = _serverSocket.EndAccept(asyncResult);
                IClient client = _clientFactory.CreateClient(clientSocket);

                lock (_clients)
                {
                    _clients.Add(client);
                }

                // Re-arm the accept loop before processing the new client.
                _serverSocket.BeginAccept(OnClientConnect, null);

                // Start receiving with a dedicated buffer per connection.
                byte[] buffer = new byte[Math.Max(1, _bufferSize)];
                clientSocket.BeginReceive(
                    buffer, 0, buffer.Length,
                    SocketFlags.None,
                    OnDataReceived,
                    new ReceiveState(clientSocket, buffer));

                OnClientConnected.Invoke(client);

                _logger?.Log(
                    $"Client [{client.Ip}:{client.Port}] connected!",
                    LogType.Info);
            }
            catch (Exception exception)
            {
                if (!_suppressSocketErrors)
                    _logger?.Log(
                        $"OnClientConnect: {exception.Message}\n{exception.StackTrace}",
                        LogType.Error);
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            var state = (ReceiveState)asyncResult.AsyncState!;
            Socket clientSocket = state.Socket;
            IClient? client;

            lock (_clients)
            {
                client = _clients.FirstOrDefault(c => GetSocket(c) == clientSocket);
            }

            try
            {
                int received = clientSocket.EndReceive(asyncResult);

                if (received == 0)
                {
                    // Zero bytes means the client closed the connection gracefully.
                    if (client != null)
                        OnClientDisconnect(client);
                    return;
                }

                byte[] chunk = new byte[received];
                Array.Copy(state.Buffer, chunk, received);

                if (client != null)
                {
                    foreach (byte[] packet in _framer.ProcessIncoming(client, chunk))
                        OnReceivedData.Invoke(client, packet);
                }

                // Re-arm the receive loop, reusing the same buffer.
                clientSocket.BeginReceive(
                    state.Buffer, 0, state.Buffer.Length,
                    SocketFlags.None,
                    OnDataReceived,
                    state);
            }
            catch (Exception exception)
            {
                if (!_suppressSocketErrors)
                    _logger?.Log(
                        $"Data receive from [{client?.Ip}:{client?.Port}] failed! {exception.Message}",
                        LogType.Error);

                if (client != null)
                    OnClientDisconnect(client);
            }
        }

        private void OnDataSent(IAsyncResult asyncResult)
        {
            var state = (SendState)asyncResult.AsyncState!;

            try
            {
                state.Socket.EndSend(asyncResult);

                OnSentData.Invoke(state.Client, state.Data);

                _logger?.Log(
                    $"Data sent to [{state.Client.Ip}:{state.Client.Port}] success!",
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_suppressSocketErrors)
                    _logger?.Log(
                        $"Data sent to [{state.Client.Ip}:{state.Client.Port}] failed! {exception.Message}",
                        LogType.Error);
            }
        }
    }
}
