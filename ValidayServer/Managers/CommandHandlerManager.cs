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
        /// <remarks>Set only by Start() and Stop().</remarks>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Registered command map exposed via ICommandRegistry.
        /// The returned wrapper is created once and reflects registrations made after construction
        /// because ReadOnlyDictionary wraps the underlying dictionary by reference.
        /// </summary>
        public IReadOnlyDictionary<ushort, Type> CommandsMap => _commandsMapReadOnly;

        /// <summary>
        /// Backwards-compatible alias.
        /// </summary>
        public IReadOnlyDictionary<ushort, Type> ServerCommandsMap => _commandsMapReadOnly;

        private readonly Dictionary<ushort, Type> _serverCommandsMap;
        private readonly IReadOnlyDictionary<ushort, Type> _commandsMapReadOnly;
        private readonly ICommandPool<ushort, IServerCommand> _commandServerPool;
        private readonly IConverterId<ushort> _converterId;
        private readonly IServer? _server;
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates the manager with default dependencies.
        /// Self-registers into the server so callers only need one line.
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
            _commandsMapReadOnly = new ReadOnlyDictionary<ushort, Type>(_serverCommandsMap);
            _converterId = converterId;
            _commandServerPool = new CommandPool<ushort, IServerCommand>();
            _server = server;
            _logger = logger;

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
            _server.OnReceivedData += OnDataReceived;

            _logger?.Log($"{nameof(CommandHandlerManager)} started!", LogType.Info);
        }

        /// <inheritdoc/>
        public virtual void Stop()
        {
            if (_server == null)
                return;

            IsActive = false;
            _server.OnReceivedData -= OnDataReceived;

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

            if (data == null || data.Length < sizeof(ushort))
            {
                _logger?.Log(
                    $"Received too-short packet ({data?.Length ?? 0} bytes) from [{sender?.Ip}:{sender?.Port}] — ignored.",
                    LogType.Warning);
                return;
            }

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
