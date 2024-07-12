using ValidayServer.Managers.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValidayServer.Network.Commands.Interfaces;

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
        /// Managers colection 
        /// </summary>
        IReadOnlyCollection<IManager> Managers { get; }

        /// <summary>
        /// Event for recived data from client
        /// </summary>
        event Action<IClient, byte[]> OnRecivedData;

        /// <summary>
        /// Event for sended data to client
        /// </summary>
        event Action<IClient, byte[]> OnSendedData;

        /// <summary>
        /// Event for connect client
        /// </summary>
        event Action<IClient> OnClientConnected;

        /// <summary>
        /// Event for disconnect client
        /// </summary>
        event Action<IClient> OnClientDisconnected;

        /// <summary>
        /// Registration new instance manager
        /// </summary>
        /// <param name="manager">Instance manager</param>
        void RegistrationManager(IManager manager);

        /// <summary>
        /// Starting this server
        /// </summary>
        void Start();

        /// <summary>
        /// Stopping this server
        /// </summary>
        void Stop();

        /// <summary>
        /// Send data to client
        /// </summary>
        /// <param name="client">Client target</param>
        /// <param name="clientCommand">Command for send to client</param>
        void SendToClient(
            IClient client,
            IClientCommand clientCommand);

        /// <summary>
        /// Disconnect client
        /// </summary>
        /// <param name="client">Client target</param>
        void DisconnectClient(IClient client);

        /// <summary>
        /// Get all active clients
        /// </summary>
        /// <returns>Active clients</returns>
        ReadOnlyCollection<IClient> GetAllConnections();
    }
}