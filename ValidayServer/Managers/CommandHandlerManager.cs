using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommandHandlerManager() 
            : this(new Dictionary<ushort, Type>())
        { }

        /// <summary>
        /// Constructor with explicit parameters
        /// </summary>
        /// <param name="serverCommandsMap">Server commands</param>
        public CommandHandlerManager(Dictionary<ushort, Type> serverCommandsMap)
        {
            _serverCommandsMap = serverCommandsMap;
        }

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
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Server is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Logger is null!");

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    if (typeof(IServerCommand).IsAssignableFrom(type) 
                        && !type.IsInterface)
                    {
                        object? serverCommand = Activator.CreateInstance(type);
                        ushort id = ((IServerCommand)serverCommand).Id;

                        if (serverCommand != null)
                            _serverCommandsMap.Add(
                                id,
                                type);                    
                    }
                }
            }
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

        private void OnDataReceived(
            IClient sender, 
            byte[] data)
        {
            if (_server == null)
                return;

            ushort commandId = BitConverter.ToUInt16(data, 0);

            if (_serverCommandsMap.TryGetValue(
                commandId, 
                out Type commandType))
            {
                if (commandType == null)
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