using System.Collections.Generic;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Managers.Interfaces
{
    /// <summary>
    /// Manages the lifecycle of named rooms and their memberships.
    /// </summary>
    public interface IRoomManager : IManager
    {
        /// <summary>All currently existing rooms.</summary>
        IReadOnlyCollection<IRoom> Rooms { get; }

        /// <summary>
        /// Creates a new room with the given <paramref name="id"/>.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if a room with the same ID already exists.</exception>
        IRoom Create(string id);

        /// <summary>Returns the room with <paramref name="id"/>, or <c>null</c> if it does not exist.</summary>
        IRoom? Get(string id);

        /// <summary>Returns the existing room with <paramref name="id"/>, or creates it atomically if absent.</summary>
        IRoom GetOrCreate(string id);

        /// <summary>Returns <c>true</c> and the room if it exists; otherwise <c>false</c>.</summary>
        bool TryGet(string id, out IRoom? room);

        /// <summary>
        /// Removes the room with <paramref name="id"/>.
        /// Returns <c>false</c> if the room does not exist.
        /// </summary>
        bool Destroy(string id);

        /// <summary>Returns all rooms that <paramref name="client"/> is currently a member of.</summary>
        IReadOnlyCollection<IRoom> GetRoomsOf(IClient client);
    }
}
