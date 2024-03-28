using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentDictionary<Type, ConcurrentBag<CommandElement>> _commandPool;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommandPool()
        {
            _commandPool = new ConcurrentDictionary<Type, ConcurrentBag<CommandElement>>();
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

            lock (_commandPool)
            {
                if (_commandPool.ContainsKey(commandsMap[id])
                    && _commandPool.TryGetValue(commandsMap[id], out ConcurrentBag<CommandElement> commandElements))
                {
                    commandElements.TryTake(out CommandElement commandElement);

                    return commandElement?.Command
                        ?? (TCommand)Activator.CreateInstance(commandsMap[id]);
                }
                else
                {
                    _commandPool.TryAdd(commandsMap[id], new ConcurrentBag<CommandElement>());

                    return (TCommand)Activator.CreateInstance(commandsMap[id]);
                }
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
                _commandPool.TryAdd(commandsMap[id], new ConcurrentBag<CommandElement>());

            _commandPool[commandsMap[id]].Add(commandElement);
        }
    }
}