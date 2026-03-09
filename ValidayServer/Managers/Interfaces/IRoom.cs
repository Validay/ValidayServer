using System.Collections.Generic;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Managers.Interfaces
{
    /// <summary>
    /// Represents a named group of connected clients.
    /// </summary>
    public interface IRoom
    {
        /// <summary>Unique identifier for this room.</summary>
        string Id { get; }

        /// <summary>Read-only snapshot of current room members.</summary>
        IReadOnlyCollection<IClient> Members { get; }

        /// <summary>Current member count.</summary>
        int Count { get; }

        /// <summary><c>true</c> when the room has no members.</summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Adds <paramref name="client"/> to the room.
        /// Returns <c>false</c> if the client is already a member.
        /// </summary>
        bool Join(IClient client);

        /// <summary>
        /// Removes <paramref name="client"/> from the room.
        /// Returns <c>false</c> if the client is not a member.
        /// </summary>
        bool Leave(IClient client);

        /// <summary>Returns <c>true</c> if <paramref name="client"/> is a member of this room.</summary>
        bool Contains(IClient client);

        /// <summary>Sends <paramref name="command"/> to all current members.</summary>
        void Broadcast(IClientCommand command);

        /// <summary>Sends <paramref name="command"/> to all current members except <paramref name="exclude"/>.</summary>
        void BroadcastExcept(IClientCommand command, IClient exclude);
    }
}
