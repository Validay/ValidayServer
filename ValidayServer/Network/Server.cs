using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Validay.Network.Interfaces;
using Validay.Logging.Interfaces;
using Validay.Logging;
using Validay.Managers.Interfaces;

namespace Validay.Network
{
    /// <summary>
    /// Default implementation server
    /// </summary>
    public class Server : IServer
    {
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

        private bool _hideSocketError;
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

        /// <summary>
        /// Default server constructor
        /// </summary>
        public Server() : this(
            ServerSettings.Default,
            true)
        { }

        /// <summary>
        /// Main server constructor
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
            _buffer = new byte[serverSettings.BufferSize];
            _logger = serverSettings.Logger;
            _clientFactory = serverSettings.ClientFactory;
            _managerFactory = serverSettings.ManagerFactory;
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

            IPEndPoint endPoint = (IPEndPoint)client.Socket.RemoteEndPoint;

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
                    $"Send data [{rawData.Length} bytes] to [{endPoint.Address}:{endPoint.Port}]",
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
        public virtual void DisconnectClient(IClient client)
        {
            OnClientDisconnect(client);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual ReadOnlyCollection<IClient> GetAllConnections()
        {
            return _clients
                .ToList()
                .AsReadOnly();
        }

        private void OnClientDisconnect(IClient client)
        {
            if (client == null)
                return;

            IPEndPoint? endPoint = null;

            try
            {
                endPoint = (IPEndPoint)client.Socket.RemoteEndPoint;
                client.Socket.Close();
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"{exception.Message}",
                        LogType.Error);

                _clients.Remove(client);
            }
            finally
            {
                OnClientDisconnected?.Invoke(client);

                _logger?.Log(
                    $"Client [{endPoint?.Address}] disconnected!",
                    LogType.Info);

                _clients.Remove(client);
            }
        }

        private void OnClientConnect(IAsyncResult asyncResult)
        {
            try
            {
                Socket clientSocket = _serverSocket.EndAccept(asyncResult);
                IClient client = _clientFactory.CreateClient(clientSocket);

                _clients.Add(client);

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
                    "Client connected!", 
                    LogType.Info);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Client connect failed! {exception.Message}", 
                        LogType.Error);
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            Socket clientSocket = (Socket)asyncResult.AsyncState;
            IPEndPoint endPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            IClient client = _clients.FirstOrDefault(client => client.Socket == clientSocket);

            if (client == null)
                return;

            try
            {               
                int bytesRead = clientSocket.EndReceive(asyncResult);

                if (bytesRead > 0)
                {
                    byte[] rawData = new byte[bytesRead];

                    Array.Copy(
                        _buffer, 
                        rawData, 
                        bytesRead);

                    OnRecivedData?.Invoke(
                        client, 
                        rawData);

                    _logger?.Log(
                        $"Received data [{rawData.Length} bytes] from [{endPoint.Address}:{endPoint.Port}]", 
                        LogType.Low);

                    clientSocket.BeginReceive(
                        _buffer, 
                        0, 
                        _buffer.Length, 
                        SocketFlags.None, 
                        new AsyncCallback(OnDataReceived), 
                        clientSocket);
                }
                else
                {
                    clientSocket.Close();
                }
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Receive data failed! {exception.Message}",
                        LogType.Error);

                OnClientDisconnect(client);
            }
        }

        private void OnDataSent(IAsyncResult asyncResult)
        {
            Socket clientSocket = (Socket)asyncResult.AsyncState;
            IPEndPoint endPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            IClient client = _clients.FirstOrDefault(client => client.Socket == clientSocket);

            if (client == null)
                return;

            try
            {
                clientSocket.EndSend(asyncResult);

                _logger?.Log(
                    $"Data sent to [{endPoint.Address}:{endPoint.Port}] success!", 
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Data sent failed! {exception.Message}", 
                        LogType.Error);

                OnClientDisconnect(client);
            }
        }
    }
}