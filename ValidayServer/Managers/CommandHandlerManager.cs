using Validay.Logging;
using Validay.Logging.Interfaces;
using Validay.Managers.Interfaces;
using Validay.Network.Commands.Interfaces;
using Validay.Network.Interfaces;
using Validay.Unilities;
using System;

namespace Validay.Managers
{
    /// <summary>
    /// Manager for handle and execute commands
    /// </summary>
    public class CommandHandlerManager : IManager
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Name { get => nameof(CommandHandlerManager); }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsActive { get; set; }

        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Initialize(
            IServer server,
            ILogger logger)
        {
            _server = server;
            _logger = logger;

            if (_server == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Server is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Logger is null!");
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Start()
        {
            if (_server == null)
            {
                _logger?.Log(
                    $"{nameof(CommandHandlerManager)}: _server is null!",
                    LogType.Warning);

                return;
            }

            IsActive = true;

            _server.OnRecivedData += OnDataReceived;

            _logger?.Log(
                $"{nameof(CommandHandlerManager)} started!",
                LogType.Info);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Stop()
        {
            if (_server == null)
                return;

            IsActive = false;

            _server.OnRecivedData -= OnDataReceived;

            _logger?.Log(
                 $"{nameof(CommandHandlerManager)} stopped!",
                LogType.Info);
        }

        private void OnDataReceived(
            IClient sender, 
            byte[] data)
        {
            short commandId = BitConverter.ToInt16(data, 0);

            if (CommandHelper.ServerCommandsMap.TryGetValue(
                commandId, 
                out Type commandType))
            {
                if (commandType == null
                    || _server == null)
                    return;

                var command = Activator.CreateInstance(commandType) 
                    as IServerCommand;

                command?.Execute(
                    sender, 
                    _server.Managers,
                    data);
            }
        }
    }
}