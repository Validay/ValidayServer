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
        public IReadOnlyCollection<IManager> Managers { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyCollection<IClient> ClientConnections { get; private set; }

        /// <inheritdoc/>
        public event Action<IClient, byte[]> OnRecivedData = delegate { };

        /// <inheritdoc/>
        public event Action<IClient, byte[]> OnSendedData = delegate { };

        /// <inheritdoc/>
        public event Action<IClient> OnClientConnected = delegate { };

        /// <inheritdoc/>
        public event Action<IClient> OnClientDisconnected = delegate { };

        private bool _isRunning;
        private bool _disposed;
        private bool _hideSocketError;
        private string _ip;
        private int _port;
        private int _connectingClientQueue;
        private int _bufferSize;
        private Socket? _serverSocket;
        private IList<IClient> _clients;
        private IList<IManager> _managers;
        private ILogger _logger;
        private IClientFactory _clientFactory;

        /// <summary>
        /// Creates a server with default settings.
        /// </summary>
        public Server() 
            : this(
                  ServerSettings.Default,
                  hideSocketError: true) 
        { }

        /// <summary>
        /// Creates a server with explicit settings.
        /// </summary>
        public Server(
            ServerSettings serverSettings,
            bool hideSocketError)
        {
            _hideSocketError = hideSocketError;
            _ip = serverSettings.Ip;
            _port = serverSettings.Port;
            _connectingClientQueue = serverSettings.ConnectingClientQueue;
            _bufferSize = serverSettings.BufferSize;
            _logger = serverSettings.Logger;
            _clientFactory = serverSettings.ClientFactory;
            _clients = new List<IClient>();
            _managers = new List<IManager>();
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
            Managers = new ReadOnlyCollection<IManager>(_managers);
        }

        /// <inheritdoc/>
        public virtual void Start()
        {
            try
            {
                _logger?.Log("Server starting...", LogType.Info);

                _serverSocket = new Socket(
                    AddressFamily.InterNetwork,
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
                return;

            try
            {
                byte[] rawData = command.GetRawData();

                socket.BeginSend(
                    rawData, 0, rawData.Length,
                    SocketFlags.None,
                    OnDataSent,
                    socket);

                OnSendedData.Invoke(client, rawData);

                _logger?.Log(
                    $"Send data [{rawData.Length} bytes] to [{client.Ip}:{client.Port}]",
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(exception.Message, LogType.Warning);
            }
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
            catch {}
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
                    ClientConnections = new ReadOnlyCollection<IClient>(_clients);
                }

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
                if (!_hideSocketError)
                    _logger?.Log(exception.Message, LogType.Error);
            }
        }

        private void OnClientConnect(IAsyncResult asyncResult)
        {
            try
            {
                Socket clientSocket = _serverSocket!.EndAccept(asyncResult);
                IClient client = _clientFactory.CreateClient(clientSocket);

                lock (_clients)
                {
                    _clients.Add(client);
                    ClientConnections = new ReadOnlyCollection<IClient>(_clients);
                }

                _serverSocket.BeginAccept(OnClientConnect, null);

                clientSocket.BeginReceive(
                    Array.Empty<byte>(), 0, 0,
                    SocketFlags.None,
                    OnDataReceived,
                    clientSocket);

                OnClientConnected.Invoke(client);

                _logger?.Log(
                    $"Client [{client.Ip}:{client.Port}] connected!",
                    LogType.Info);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"OnClientConnect: {exception.Message}\n{exception.StackTrace}",
                        LogType.Error);
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            Socket clientSocket = (Socket)asyncResult.AsyncState!;
            IClient? client;

            lock (_clients)
            {
                client = _clients.FirstOrDefault(c => GetSocket(c) == clientSocket);
            }

            try
            {
                clientSocket.EndReceive(asyncResult);

                byte[] buffer = new byte[_bufferSize];
                int received = clientSocket.Receive(buffer, buffer.Length, SocketFlags.None);

                if (received < buffer.Length)
                    Array.Resize(ref buffer, received);

                if (client != null)
                    OnRecivedData.Invoke(client, buffer);

                clientSocket.BeginReceive(
                    Array.Empty<byte>(), 0, 0,
                    SocketFlags.None,
                    OnDataReceived,
                    clientSocket);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Data receive from [{client?.Ip}:{client?.Port}] failed! {exception.Message}",
                        LogType.Error);

                if (client != null)
                    OnClientDisconnect(client);
            }
        }

        private void OnDataSent(IAsyncResult asyncResult)
        {
            Socket clientSocket = (Socket)asyncResult.AsyncState!;
            IClient? client;

            lock (_clients)
            {
                client = _clients.FirstOrDefault(c => GetSocket(c) == clientSocket);
            }

            if (client == null)
                return;

            try
            {
                clientSocket.EndSend(asyncResult);

                _logger?.Log(
                    $"Data sent to [{client.Ip}:{client.Port}] success!",
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Data sent to [{client.Ip}:{client.Port}] failed! {exception.Message}",
                        LogType.Error);
            }
        }
    }
}