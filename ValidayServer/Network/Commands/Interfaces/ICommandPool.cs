using System;
using System.Collections.Generic;

namespace ValidayServer.Network.Commands.Interfaces
{
    /// <summary>
    /// Interface for command pool
    /// </summary>
    /// <typeparam name="TId">Type id</typeparam>
    /// <typeparam name="TCommand">Type command</typeparam>
    public interface ICommandPool<TId, TCommand>
    {
        /// <summary>
        /// Get command from pool
        /// </summary>
        /// <param name="id"></param>
        /// <param name="commandsMap">Commands map</param>
        /// <returns>Command from pool</returns>
        public TCommand GetCommand(
            TId id,
            IDictionary<TId, Type> commandsMap);

        /// <summary>
        /// Return command to pool
        /// </summary>
        /// <param name="id">Id command</param>
        /// <param name="command">Inctance command</param>
        /// <param name="commandsMap">Commands map</param>
        void ReturnCommandToPool(
            TId id,
            TCommand command,
            IDictionary<TId, Type> commandsMap);
    }
}