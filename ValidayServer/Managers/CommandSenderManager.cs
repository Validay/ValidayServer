using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;
using System;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Manager for send to clients command
    /// </summary>
    public class CommandSenderManager : IManager
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Name { get => nameof(CommandSenderManager); }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsActive { get; set; }

        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public CommandSenderManager(
            IServer server,
            ILogger logger)
        {            
            _server = server;
            _logger = logger;

            if (_server == null)
                throw new NullReferenceException($"{nameof(CommandSenderManager)}: Server is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(CommandSenderManager)}: Logger is null!");

            _server.RegistrationManager(this);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void Start()
        {
            if (_server == null)
            {
                _logger?.Log(
                    $"{nameof(CommandSenderManager)}: _server is null!",
                    LogType.Warning);

                return;
            }

            IsActive = true;

            _logger?.Log(
                $"{nameof(CommandSenderManager)} started!",
                LogType.Info);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void Stop()
        {
            if (_server == null)
                return;

            IsActive = false;

            _logger?.Log(
                 $"{nameof(CommandSenderManager)} stopped!",
                LogType.Info);
        }

        /// <summary>
        /// Send data to client
        /// </summary>
        /// /// <typeparam name="T">Type command</typeparam>
        /// <param name="target">Target client</param>
        /// <param name="data">Data for send</param>
        public void SendData<T>(
            IClient target, 
            T data)
            where T : IClientCommand
        {
            if (_server == null)
                return;

            byte[] rawData = data.GetRawData();

            _server.SendToClient(
                target, 
                rawData);
        }
    }
}