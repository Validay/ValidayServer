using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network;
using ValidayServer.Network.Commands;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Manager that maps incoming command IDs to handler types and executes them via a pool.
    /// Also implements ICommandRegistry so other managers can inspect the command map
    /// without depending on this concrete type.
    /// </summary>
    public class CommandHandlerManager : IManager, ICommandRegistry
    {
        /// <inheritdoc/>
        public string Name => nameof(CommandHandlerManager);

        /// <inheritdoc/>
        /// <remarks>Set only by Start() and Stop() — private to prevent external bypass of those methods.</remarks>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Registered command map exposed via ICommandRegistry.
        /// Returns a true ReadOnlyDictionary — modification attempts throw NotSupportedException.
        /// </summary>
        public IReadOnlyDictionary<ushort, Type> CommandsMap
            => new ReadOnlyDictionary<ushort, Type>(_serverCommandsMap);

        /// <summary>
        /// Backwards-compatible alias so existing call sites keep working.
        /// </summary>
        public IReadOnlyDictionary<ushort, Type> ServerCommandsMap => CommandsMap;

        private Dictionary<ushort, Type> _serverCommandsMap;
        private ICommandPool<ushort, IServerCommand> _commandServerPool;
        private IConverterId<ushort> _converterId;
        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// Creates the manager.
        /// NOTE: the manager does NOT register itself into the server here.
        /// Call server.RegistrationManager(handler) explicitly after construction.
        /// </summary>
        public CommandHandlerManager(
            IServer server,
            ILogger logger)
                : this(
                      server, 
                      logger, 
                      new Dictionary<ushort, Type>(),
                      new UshortConverterId())
        { }

        /// <summary>
        /// Creates the manager with explicit dependencies.
        /// </summary>
        public CommandHandlerManager(
            IServer server,
            ILogger logger,
            Dictionary<ushort, Type> serverCommandsMap,
            IConverterId<ushort> converterId)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server),
                    $"{nameof(CommandHandlerManager)}: server is null!");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger),
                    $"{nameof(CommandHandlerManager)}: logger is null!");

            _serverCommandsMap = serverCommandsMap;
            _converterId = converterId;
            _commandServerPool = new CommandPool<ushort, IServerCommand>();
            _server = server;
            _logger = logger;

            //TODO: Self-registration so the old one-liner API still works:
            //   new CommandHandlerManager(server, logger);
            _server.RegistrationManager(this);
        }

        /// <inheritdoc/>
        public virtual void Start()
        {
            if (_server == null)
            {
                _logger?.Log(
                    $"{nameof(CommandHandlerManager)}: server is null, cannot start.",
                    LogType.Warning);
                return;
            }

            IsActive = true;
            _server.OnRecivedData += OnDataReceived;

            _logger?.Log($"{nameof(CommandHandlerManager)} started!", LogType.Info);
        }

        /// <inheritdoc/>
        public virtual void Stop()
        {
            if (_server == null)
                return;

            IsActive = false;
            _server.OnRecivedData -= OnDataReceived;

            _logger?.Log($"{nameof(CommandHandlerManager)} stopped!", LogType.Info);
        }

        /// <summary>
        /// Registers a command type for the given ID.
        /// </summary>
        /// <typeparam name="T">Command type that implements IServerCommand.</typeparam>
        /// <param name="id">Unique numeric ID for this command.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the ID or the type is already registered.
        /// </exception>
        public virtual void RegistrationCommand<T>(ushort id)
            where T : IServerCommand
        {
            if (_serverCommandsMap.ContainsKey(id))
                throw new InvalidOperationException(
                    $"ServerCommandsMap already contains id = {id}!");

            if (_serverCommandsMap.ContainsValue(typeof(T)))
                throw new InvalidOperationException(
                    $"ServerCommandsMap already contains type {typeof(T).Name}!");

            _serverCommandsMap.Add(id, typeof(T));
        }

        private void OnDataReceived(IClient sender, byte[] data)
        {
            if (_server == null)
                return;

            try
            {
                ushort commandId = _converterId.Convert(data);

                if (!_serverCommandsMap.ContainsKey(commandId))
                {
                    _logger?.Log(
                        $"Unknown command id={commandId} from [{sender?.Ip}:{sender?.Port}]",
                        LogType.Warning);
                    return;
                }

                IServerCommand command = _commandServerPool.GetCommand(commandId, _serverCommandsMap);

                command.Execute(sender, data);

                _commandServerPool.ReturnCommandToPool(commandId, command, _serverCommandsMap);
            }
            catch (Exception ex)
            {
                _logger?.Log(
                    $"Command handling failed for [{sender?.Ip}:{sender?.Port}]: {ex.Message}",
                    LogType.Error);
            }
        }
    }
}