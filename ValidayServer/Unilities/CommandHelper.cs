using Validay.Network.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Validay.Unilities
{
    /// <summary>
    /// Helper for commands
    /// </summary>
    public static class CommandHelper
    {
        /// <summary>
        /// Get all server commands
        /// </summary>
        public static ReadOnlyDictionary<short, Type> ServerCommandsMap => new ReadOnlyDictionary<short, Type>(_serverCommandsMap);

        private static readonly Dictionary<short, Type> _serverCommandsMap = new Dictionary<short, Type>();

        /// <summary>
        /// Registration new server command
        /// </summary>
        /// <typeparam name="T">Type server command</typeparam>
        /// <param name="id">Id server command</param>
        /// <exception cref="InvalidOperationException">ServerCommandsMap already exists this id</exception>
        public static void RegistrationCommand<T>(short id)
            where T : IServerCommand
        {
            if (_serverCommandsMap.ContainsKey(id))
                throw new InvalidOperationException($"ServerCommandsMap already exists this id = {id}!");

            _serverCommandsMap.Add(id, typeof(T));
        }
    }
}
