using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;
using ValidayServer.Network.Commands;
using ValidayServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValidayServer.Managers
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

        /// <summary>
        /// Get all server commands
        /// </summary>
        public IReadOnlyDictionary<ushort, Type> ServerCommandsMap
        {
            get => _serverCommandsMap.ToDictionary(
                command => command.Key,
                command => command.Value);
        }
        
        private Dictionary<ushort, Type> _serverCommandsMap;
        private ICommandPool<ushort, IServerCommand> _commandServerPool;
        private IConverterId<ushort> _converterId;
        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="server">Instance server where register this manager</param>
        /// <param name="logger">Instance logger fot this manager</param>
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
        /// Constructor with explicit parameters
        /// </summary>
        /// <param name="server">Instance server when register this manager</param>
        /// <param name="logger">Instance logger fot this manager</param>
        /// <param name="serverCommandsMap">Server commands</param>
        /// <param name="converterId">Converter id from bytes</param>
        /// <exception cref="NullReferenceException">Exception null parameters</exception>
        public CommandHandlerManager(
            IServer server,
            ILogger logger,
            Dictionary<ushort, Type> serverCommandsMap,
            IConverterId<ushort> converterId)
        {
            _commandServerPool = new CommandPool<ushort, IServerCommand>();
            _serverCommandsMap = serverCommandsMap;
            _converterId = converterId;
            _server = server;
            _logger = logger;

            if (_server == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Server is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Logger is null!");

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
        public virtual void Stop()
        {
            if (_server == null)
                return;

            IsActive = false;

            _server.OnRecivedData -= OnDataReceived;

            _logger?.Log(
                 $"{nameof(CommandHandlerManager)} stopped!",
                LogType.Info);
        }

        /// <summary>
        /// Registration new command type
        /// </summary>
        /// <typeparam name="T">Type command</typeparam>
        /// <param name="id">Id command</param>
        /// <exception cref="InvalidOperationException">Already exist command exception</exception>
        public virtual void RegistrationCommand<T>(ushort id)
            where T : IServerCommand
        {
            if (_serverCommandsMap == null)
                _serverCommandsMap = new Dictionary<ushort, Type>();

            if (_serverCommandsMap.ContainsKey(id))
                throw new InvalidOperationException($"ServerCommandsMap already exists this id = {id}!");

            if (_serverCommandsMap.ContainsValue(typeof(T)))
                throw new InvalidOperationException($"ServerCommandsMap already exists this type {nameof(T)}!");

            _serverCommandsMap.Add(id, typeof(T));
        }

        private void OnDataReceived(
            IClient sender, 
            byte[] data)
        {
            if (_server == null)
                return;

            ushort commandId = _converterId.Convert(data);
            IServerCommand command = _commandServerPool.GetCommand(
                commandId, 
                _serverCommandsMap);

            command.Execute(
                sender,
                _server.Managers,
                data);

            _commandServerPool.ReturnCommandToPool(
                commandId, 
                command,
                _serverCommandsMap);
        }
    }
}