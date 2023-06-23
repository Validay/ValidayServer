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
        public IReadOnlyCollection<IManager> Managers 
        { 
            get => _managers
                .ToList()
                .AsReadOnly(); 
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<IClient, byte[]>? OnRecivedData;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<IClient, byte[]>? OnSendedData;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<IClient>? OnClientConnected;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<IClient>? OnClientDisconnected;

        private bool _isRunning;
        private bool _hideSocketError;
        private int _maxDepthReadPacket;
        private string _ip;
        private int _port;
        private int _connectingClientQueue;
        private byte[] _buffer;
        private Socket _serverSocket;
        private IList<IClient> _clients;
        private IList<IManager> _managers;
        private ILogger _logger;
        private IClientFactory _clientFactory;
        private IManagerFactory _managerFactory;

        private readonly byte[] _markerStartPacket;

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
            _maxDepthReadPacket = serverSettings.MaxDepthReadPacket;
            _ip = serverSettings.Ip;
            _port = serverSettings.Port;
            _connectingClientQueue = serverSettings.ConnectingClientQueue;          
            _buffer = new byte[serverSettings.BufferSize];
            _logger = serverSettings.Logger;
            _clientFactory = serverSettings.ClientFactory;
            _managerFactory = serverSettings.ManagerFactory;
            _markerStartPacket = serverSettings.MarkerStartPacket;
            _clients = new List<IClient>();
            _managers = new List<IManager>();
            _serverSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void RegistrationManager<T>()
            where T : IManager
        {
            try
            {
                IManager newManager = _managerFactory.CreateManager<T>();
                bool hasExisting = _managers.FirstOrDefault(manager => manager.Name == newManager.Name) != null;

                if (hasExisting)
                {
                    _logger?.Log(
                        $"Registration manager failed! Manager [{newManager.Name}] already registration!",
                        LogType.Warning);

                    return;
                }

                newManager.Initialize(
                    this, 
                    _logger);

                _managers.Add(newManager);
            }
            catch (Exception exception)
            {
                _logger?.Log(
                    $"Registration manager failed! {exception.Message}",
                    LogType.Error);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <exception cref="InvalidOperationException">Already exist manager exception</exception>
        public virtual void RegistrationManager(IManager manager)
        {
            bool hasExisting = _managers.FirstOrDefault(existManager => existManager.Name == manager.Name) != null;

            if (hasExisting)
            {
                _logger?.Log(
                    $"Registration manager failed! Manager [{manager.Name}] already registration!",
                    LogType.Warning);

                throw new InvalidOperationException($"Registration manager failed! Manager [{manager.Name}] already registration!");
            }

            manager.Initialize(
                this,
                _logger);

            _managers.Add(manager);
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
            byte[] rawData)
        {
            if (client == null)
                return;

            try
            {
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
        public virtual void DisconnectClient(IClient? client)
        {
            OnClientDisconnect(client);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual ReadOnlyCollection<IClient> GetAllConnections()
        {
            lock (_clients)
            {
                return new ReadOnlyCollection<IClient>(_clients);
            }
        }

        private void OnClientDisconnect(IClient? client)
        {
            if (client == null)
                return;

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
        }

        private void OnClientConnect(IAsyncResult asyncResult)
        {
            IClient? client = null;

            try
            {
                Socket clientSocket = _serverSocket.EndAccept(asyncResult);
                client = _clientFactory.CreateClient(clientSocket);

                lock (_clients)
                {
                    _clients.Add(client);
                }

                _serverSocket.BeginAccept(
                    new AsyncCallback(OnClientConnect), 
                    null);

                clientSocket.BeginReceive(
                    _buffer, 
                    0, 
                    _buffer.Length, 
                    SocketFlags.None,
                    new AsyncCallback(OnDataReceived), 
                    clientSocket);

                OnClientConnected?.Invoke(client);

                _logger?.Log(
                    $"Client [{client?.Ip}:{client?.Port}] connected!", 
                    LogType.Info);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Client [{client?.Ip}:{client?.Port}] connect failed! {exception.Message}",
                        LogType.Error);
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            Socket clientSocket = (Socket)asyncResult.AsyncState;
            IClient? client = null;

            lock (_clients)
            {
                client = _clients.FirstOrDefault(client => client?.Socket == clientSocket);
            }

            if (client == null)
                return;

            try
            {
                int bytesRead = clientSocket.EndReceive(asyncResult);

                if (bytesRead > 0)
                {
                    byte[] receivedData = new byte[bytesRead];

                    Array.Copy(
                        _buffer, 
                        receivedData, 
                        bytesRead);

                    ProcessReceivedData(
                        client, 
                        receivedData);

                    if (asyncResult.CompletedSynchronously)
                    {
                        while (bytesRead > 0)
                        {
                            ProcessReceivedData(
                                client,
                                receivedData);

                            bytesRead = clientSocket.Receive(_buffer);
                        }
                    }
                    else
                    {
                        clientSocket.BeginReceive(
                            _buffer,
                            0,
                            _buffer.Length,
                            SocketFlags.None,
                            new AsyncCallback(OnDataReceived),
                            clientSocket);
                    }
                }
                else
                {
                    DisconnectClient(client);
                }
            }
            catch (Exception exception)
            {
                DisconnectClient(client);

                if (!_hideSocketError)
                    _logger?.Log(
                        $"Receive data failed! {exception.Message}",
                        LogType.Error);
            }
        }

        private void OnDataSent(IAsyncResult asyncResult)
        {
            Socket clientSocket = (Socket)asyncResult.AsyncState;
            IClient? client = null;

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

                OnClientDisconnect(client);
            }
        }

        private void ProcessReceivedData(
            IClient? client, 
            byte[] receivedData,
            int depth = 0)
        {
            if (client == null)
                return;

            int startMarkerIndex = FindSequence(
                receivedData, 
                _markerStartPacket);

            if (startMarkerIndex != -1)
            {
                if (receivedData.Length >= startMarkerIndex + _markerStartPacket.Length + sizeof(int))
                {
                    byte[] sizeIndicator = new byte[sizeof(int)];

                    Array.Copy(
                        receivedData, 
                        startMarkerIndex + _markerStartPacket.Length, 
                        sizeIndicator, 
                        0, 
                        sizeof(int));

                    int packetSize = BitConverter.ToInt32(
                        sizeIndicator, 
                        0);

                    if (receivedData.Length >= startMarkerIndex + _markerStartPacket.Length + sizeof(int) + packetSize)
                    {
                        byte[] packetData = new byte[packetSize];

                        Array.Copy(
                            receivedData, 
                            startMarkerIndex + _markerStartPacket.Length + sizeof(int), 
                            packetData, 
                            0, 
                            packetSize);

                        OnRecivedData?.Invoke(
                            client,
                            packetData);

                        _logger?.Log(
                            $"Received data [{packetSize} bytes] from [{client?.Ip}:{client?.Port}]",
                            LogType.Low);

                        byte[] remainingData = new byte[receivedData.Length - (startMarkerIndex + _markerStartPacket.Length + sizeof(int) + packetSize)];

                        Array.Copy(
                            receivedData, 
                            startMarkerIndex + _markerStartPacket.Length + sizeof(int) + packetSize, 
                            remainingData, 
                            0, 
                            remainingData.Length);

                        if (remainingData.Length > 0)
                        {
                            if (depth < _maxDepthReadPacket)
                            {
                                ProcessReceivedData(
                                    client,
                                    remainingData,
                                    depth++);
                            }
                            else
                            {
                                _logger?.Log(
                                    $"Client [{client?.Ip}:{client?.Port}]: Packet read depth exceeded!",
                                    LogType.Warning);

                                DisconnectClient(client);
                            }
                        }
                    }
                }
            }
        }

        private int FindSequence(
            byte[] data, 
            byte[] sequence)
        {
            for (int i = 0; i <= data.Length - sequence.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < sequence.Length; j++)
                {
                    if (data[i + j] != sequence[j])
                    {
                        found = false;

                        break;
                    }
                }

                if (found)
                    return i;
            }

            return -1;
        }
    }
}