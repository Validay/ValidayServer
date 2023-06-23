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
    /// <typeparam name="TId">Type id client command</typeparam>
    public class CommandSenderManager<TId> : IManager
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Name { get => nameof(CommandSenderManager<TId>); }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsActive { get; set; }

        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void Initialize(
            IServer server,
            ILogger logger)
        {            
            _server = server;
            _logger = logger;

            if (_server == null)
                throw new NullReferenceException($"{nameof(CommandSenderManager<TId>)}: Server is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(CommandSenderManager<TId>)}: Logger is null!");
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void Start()
        {
            if (_server == null)
            {
                _logger?.Log(
                    $"{nameof(CommandSenderManager<TId>)}: _server is null!",
                    LogType.Warning);

                return;
            }

            IsActive = true;

            _logger?.Log(
                $"{nameof(CommandSenderManager<TId>)} started!",
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
                 $"{nameof(CommandSenderManager<TId>)} stopped!",
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
            where T : IClientCommand<TId>
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