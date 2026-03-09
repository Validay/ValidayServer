using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using System;
using System.Collections.Generic;

namespace ValidayServer.Network.Interfaces
{
    /// <summary>
    /// Interface for server
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// Is the server running
        /// </summary>
        bool IsRun { get; }

        /// <summary>
        /// Registered managers collection
        /// </summary>
        IReadOnlyCollection<IManager> Managers { get; }

        /// <summary>
        /// Connected clients collection
        /// </summary>
        IReadOnlyCollection<IClient> ClientConnections { get; }

        /// <summary>
        /// Fires when data is received from a client
        /// </summary>
        event Action<IClient, byte[]> OnReceivedData;

        /// <summary>
        /// Fires when data is successfully sent to a client
        /// </summary>
        event Action<IClient, byte[]> OnSentData;

        /// <summary>
        /// Fires when a client connects
        /// </summary>
        event Action<IClient> OnClientConnected;

        /// <summary>
        /// Fires when a client disconnects
        /// </summary>
        event Action<IClient> OnClientDisconnected;

        /// <summary>
        /// Register a manager. Must be called before Start().
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a manager with the same name is already registered,
        /// or if the server is already running.
        /// </exception>
        void RegistrationManager(IManager manager);

        /// <summary>
        /// Start the server and all registered managers
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the server and all registered managers
        /// </summary>
        void Stop();

        /// <summary>
        /// Send a command to a specific client
        /// </summary>
        void SendToClient(
            IClient client,
            IClientCommand clientCommand);

        /// <summary>
        /// Disconnect a specific client
        /// </summary>
        void DisconnectClient(IClient client);
    }
}