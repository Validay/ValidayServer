using System;
using System.Collections.Generic;

namespace ValidayServer.Managers.Interfaces
{
    /// <summary>
    /// Interface for accessing registered command map.
    /// Used to decouple managers that need to inspect known commands
    /// without depending on a concrete CommandHandlerManager type.
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// Read-only map of registered command IDs to their types
        /// </summary>
        IReadOnlyDictionary<ushort, Type> CommandsMap { get; }
    }
}