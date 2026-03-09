using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ValidayServer.Network.Commands.Interfaces;

namespace ValidayServer.Network.Commands
{
    /// <summary>
    /// Thread-safe object pool for command instances, keyed by command type.
    /// Uses ConcurrentDictionary and ConcurrentBag — no external locking needed.
    /// </summary>
    /// <typeparam name="TId">Type of the command ID</typeparam>
    /// <typeparam name="TCommand">Type of the command</typeparam>
    public class CommandPool<TId, TCommand> : ICommandPool<TId, TCommand>
        where TCommand : class
    {
        private readonly ConcurrentDictionary<Type, ConcurrentBag<TCommand>> _pool;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommandPool()
        {
            _pool = new ConcurrentDictionary<Type, ConcurrentBag<TCommand>>();
        }

        /// <inheritdoc/>
        /// <exception cref="KeyNotFoundException">When the command ID is not in the map</exception>
        /// <exception cref="InvalidOperationException">When the mapped type cannot be instantiated</exception>
        public TCommand GetCommand(
            TId id,
            IDictionary<TId, Type> commandsMap)
        {
            if (!commandsMap.TryGetValue(id, out Type? commandType))
                throw new KeyNotFoundException($"Command with ID {id} not found in server commands map.");

            ConcurrentBag<TCommand> bag = _pool.GetOrAdd(commandType, _ => new ConcurrentBag<TCommand>());

            if (bag.TryTake(out TCommand? pooled))
                return pooled;

            return (TCommand)(Activator.CreateInstance(commandType)
                ?? throw new InvalidOperationException($"Failed to create instance of {commandType.Name}."));
        }

        /// <inheritdoc/>
        /// <exception cref="KeyNotFoundException">When the command ID is not in the map</exception>
        public void ReturnCommandToPool(
            TId id,
            TCommand command,
            IDictionary<TId, Type> commandsMap)
        {
            if (!commandsMap.TryGetValue(id, out Type? commandType))
                throw new KeyNotFoundException($"Command with ID {id} not found in server commands map.");

            ConcurrentBag<TCommand> bag = _pool.GetOrAdd(commandType, _ => new ConcurrentBag<TCommand>());
            bag.Add(command);
        }
    }
}
