using System;
using System.Collections.Generic;
using ValidayServer.Network.Commands.Interfaces;

namespace ValidayServer.Network.Commands
{
    /// <summary>
    /// Base pool commands
    /// </summary>
    /// <typeparam name="TId">Type id command</typeparam>
    /// <typeparam name="TCommand">Type command</typeparam>
    public class CommandPool<TId, TCommand> : ICommandPool<TId, TCommand>
    {
        private readonly IDictionary<TId, TCommand> _commandPool;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommandPool()
        {
            _commandPool = new Dictionary<TId, TCommand>();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <exception cref="KeyNotFoundException">When id command dont exist</exception>
        public TCommand GetCommand(
            TId id,
            IDictionary<TId, Type> serverCommandsMap)
        {
            if (_commandPool.ContainsKey(id))
            {
                TCommand command = _commandPool[id];
                _commandPool.Remove(id);

                return command;
            }
            else if (serverCommandsMap.ContainsKey(id))
            {
                Type commandType = serverCommandsMap[id];
                TCommand command = (TCommand)Activator.CreateInstance(commandType);

                return command;
            }
            else
            {
                throw new KeyNotFoundException($"Command with ID {id} not found.");
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ReturnCommandToPool(
            TId id,
            TCommand command)
        {
            _commandPool[id] = command;
        }
    }
}