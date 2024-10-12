using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ValidayServer.Network.Interfaces;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Logging;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace ValidayServer.Network
{
    /// <summary>
    /// Default implementation server
    /// </summary>
    public class Server : IServer
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsRun => _isRunning;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IReadOnlyCollection<IManager> Managers { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IReadOnlyCollection<IClient> ClientConnections { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<IClient, byte[]> OnRecivedData;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<IClient, byte[]> OnSendedData;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<IClient> OnClientConnected;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<IClient> OnClientDisconnected;

        private bool _isRunning;
        private bool _hideSocketError;
        private string _ip;
        private int _port;
        private int _connectingClientQueue;
        private int _bufferSize;
        private Socket _serverSocket;
        private IList<IClient> _clients;
        private IList<IManager> _managers;
        private ILogger _logger;
        private IClientFactory _clientFactory;

        /// <summary>
        /// Default server constructor
        /// </summary>
        public Server() : this(
            ServerSettings.Default,
            true)
        { }

        /// <summary>
        /// Constructor with explicit parameters
        /// </summary>
        /// <param name="serverSettings">Server parameters</param>
        /// <param name="hideSocketError">Hide no critical socket errors</param>
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
            _serverSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            Managers = new ReadOnlyCollection<IManager>(_managers);
            ClientConnections = new ReadOnlyCollection<IClient>(_clients);
            OnRecivedData = delegate {};
            OnSendedData = delegate {};
            OnClientConnected = delegate {};
            OnClientDisconnected = delegate {};           
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="manager">New manager for registration</param>
        /// <exception cref="InvalidOperationException">Already exist manager exception</exception>
        public virtual void RegistrationManager([NotNull] IManager manager)
        {
            bool hasExisting = _managers.FirstOrDefault(existManager => existManager.Name == manager.Name) != null;

            if (hasExisting)
            {
                _logger?.Log(
                    $"Registration manager failed! Manager [{manager.Name}] already registration!",
                    LogType.Warning);

                throw new InvalidOperationException($"Registration manager failed! Manager [{manager.Name}] already registration!");
            }

            _managers.Add(manager);

            Managers = new ReadOnlyCollection<IManager>(_managers);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void Start()
        {
            try
            {
                _logger?.Log(
                    "Server starting...", 
                    LogType.Info);

                _managers.ToList()
                    .ForEach(manager => 
                    {
                        manager.Start();
                    });

                IPAddress ipAddress = IPAddress.Parse(_ip);
                IPEndPoint ipEndPoint = new IPEndPoint(
                    ipAddress, 
                    _port);

                _serverSocket.Bind(ipEndPoint);
                _serverSocket.Listen(_connectingClientQueue);
                _serverSocket.BeginAccept(
                    new AsyncCallback(OnClientConnect), 
                    null);

                _isRunning = true;

                _logger?.Log(
                    "Server started!", 
                    LogType.Info);
            }
            catch (Exception exception)
            {
                _logger?.Log(
                    $"Server start failed! {exception.Message}", 
                    LogType.CriticalError);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void Stop()
        {
            try
            {
                _logger?.Log(
                    "Server stopping...", 
                    LogType.Info);

                _managers.ToList()
                    .ForEach(manager =>
                    {
                        manager.Stop();
                    });

                if (_serverSocket != null)
                {
                    _serverSocket.Close();

                    _isRunning = false;

                    _logger?.Log(
                        "Server stopped!", 
                        LogType.Info);
                }
                else
                {
                    _logger?.Log(
                        "Server already stopped!", 
                        LogType.Warning);
                }
            }
            catch (Exception exception)
            {
                _logger?.Log(
                    $"Server stop failed! {exception.Message}", 
                    LogType.CriticalError);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void SendToClient(
            IClient client,
            IClientCommand command)
        {
            try
            {
                byte[] rawData = command.GetRawData();

                client.Socket.BeginSend(
                    rawData,
                    0,
                    rawData.Length,
                    SocketFlags.None,
                    new AsyncCallback(OnDataSent),
                    client.Socket);

                OnSendedData?.Invoke(
                    client, 
                    rawData);

                _logger?.Log(
                    $"Send data [{rawData.Length} bytes] to [{client?.Ip}:{client?.Port}]",
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"{exception.Message}",
                        LogType.Warning);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void DisconnectClient([NotNull] IClient client)
        {
            OnClientDisconnect(client);
        }

        private void OnClientDisconnect(IClient client)
        {
            try
            {
                client.Socket.Close();
                client.Socket.Dispose();
                OnClientDisconnected?.Invoke(client);

                lock (_clients)
                {
                    if (client != null)
                        _clients.Remove(client);

                    for (int i = 0; i < _clients.Count; i++)
                        if (_clients[i] == null)
                            _clients.RemoveAt(i);
                }

                _logger?.Log(
                    $"Client [{client?.Ip}:{client?.Port}] disconnected!",
                    LogType.Info);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"{exception.Message}",
                        LogType.Error);

                lock (_clients)
                {
                    _clients.Remove(client);
                }
            }
            finally
            {
                ClientConnections = new ReadOnlyCollection<IClient>(_clients);
            }
        }

        private void OnClientConnect(IAsyncResult asyncResult)
        {
            IClient client;

            try
            {
                Socket clientSocket = _serverSocket.EndAccept(asyncResult);
                client = _clientFactory.CreateClient(clientSocket);

                lock (_clients)
                {
                    _clients.Add(client);

                    ClientConnections = new ReadOnlyCollection<IClient>(_clients);
                }

                _serverSocket.BeginAccept(
                    new AsyncCallback(OnClientConnect), 
                    null);

                clientSocket.BeginReceive(
                    new byte[] { }, 
                    0, 
                    0, 
                    SocketFlags.None,
                    new AsyncCallback(OnDataReceived), 
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
            Socket clientSocket = (Socket)asyncResult.AsyncState;
            IClient client;

            lock (_clients)
            {
                client = _clients.FirstOrDefault(client => client?.Socket == clientSocket);
            }

            try
            {
                clientSocket.EndReceive(asyncResult);

                byte[] buffer = new byte[_bufferSize];
                int receive = clientSocket.Receive(
                    buffer,
                    buffer.Length,
                    SocketFlags.None);

                if (receive < buffer.Length)
                    Array.Resize(
                        ref buffer,
                        receive);

                OnRecivedData.Invoke(
                    client,
                    buffer);
                clientSocket.BeginReceive(
                   new byte[] { },
                   0,
                   0,
                   SocketFlags.None,
                   new AsyncCallback(OnDataReceived),
                   clientSocket);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Data receive from [{client?.Ip}:{client?.Port}] failed! {exception.Message}",
                        LogType.Error);
            }
        }

        private void OnDataSent(IAsyncResult asyncResult)
        {
            Socket clientSocket = (Socket)asyncResult.AsyncState;
            IClient client;

            lock (_clients)
            {
                client = _clients.FirstOrDefault(client => client?.Socket == clientSocket);
            }

            if (client == null)
                return;

            try
            {
                clientSocket.EndSend(asyncResult);

                _logger?.Log(
                    $"Data sent to [{client?.Ip}:{client?.Port}] success!", 
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Data sent to [{client?.Ip}:{client?.Port}] failed! {exception.Message}", 
                        LogType.Error);
            }
        }
    }
}