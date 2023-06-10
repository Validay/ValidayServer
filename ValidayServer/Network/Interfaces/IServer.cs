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
        /// Get all server commands
        /// </summary>
        IReadOnlyDictionary<short, Type> ServerCommandsMap { get; }

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
        /// Registration new server command
        /// </summary>
        /// <typeparam name="T">Type server command</typeparam>
        /// <param name="id">Id server command</param>
        void RegistrationCommand<T>(short id)
            where T : IServerCommand;

        /// <summary>
        /// Registration new manager
        /// </summary>
        /// <typeparam name="T">Manager type</typeparam>
        void RegistrationManager<T>()
            where T : IManager;

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
        /// <param name="rawData">Raw data</param>
        void SendToClient(
            IClient client,
            byte[] rawData);

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
