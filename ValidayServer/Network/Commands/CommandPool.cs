using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ValidayServer.Network.Commands.Interfaces;

namespace ValidayServer.Network.Commands
{
    /// <summary>
    /// Base pool commands
    /// </summary>
    /// <typeparam name="TId">Type id command</typeparam>
    /// <typeparam name="TCommand">Type command</typeparam>
    public class CommandPool<TId, TCommand> : ICommandPool<TId, TCommand>
        where TCommand : class
    {
        private class CommandElement
        {
            public TId Id { get; set; }
            public TCommand? Command { get; set; }

            public CommandElement(
                TId id, 
                TCommand? command)
            {
                Id = id;
                Command = command;
            }
        }

        private readonly ConcurrentDictionary<Type, List<CommandElement>> _commandPool;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommandPool()
        {
            _commandPool = new ConcurrentDictionary<Type, List<CommandElement>>();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <exception cref="KeyNotFoundException">When id command dont exist</exception>
        public TCommand GetCommand(
            TId id,
            IDictionary<TId, Type> commandsMap)
        {
            if (!commandsMap.ContainsKey(id))
                throw new KeyNotFoundException($"Command with ID {id} not founded in server commands map.");

            if (_commandPool.ContainsKey(commandsMap[id]))
            {
                CommandElement commandElement = _commandPool[commandsMap[id]].FirstOrDefault(commandElement =>
                {
                    if (commandElement != null 
                        && commandElement.Id != null)
                        return commandElement.Id.Equals(id);

                    return false;
                });

                return commandElement?.Command 
                    ?? (TCommand)Activator.CreateInstance(commandsMap[id]);
            }
            else
            {
                _commandPool[commandsMap[id]] = new List<CommandElement>();

                return (TCommand)Activator.CreateInstance(commandsMap[id]);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ReturnCommandToPool(
            TId id,
            TCommand command,
            IDictionary<TId, Type> commandsMap)
        {
            if (!commandsMap.ContainsKey(id))
                throw new KeyNotFoundException($"Command with ID {id} not founded in server commands map.");

            CommandElement commandElement = new CommandElement(
                id, 
                command);

            if (!_commandPool.ContainsKey(commandsMap[id]))
                _commandPool[commandsMap[id]] = new List<CommandElement>();

            _commandPool[commandsMap[id]].Add(commandElement);
        }
    }
}