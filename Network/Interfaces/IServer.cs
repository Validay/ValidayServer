using Validay.Managers.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Validay.Network.Interfaces
{
    /// <summary>
    /// Interface for server
    /// </summary>
    public interface IServer
    {
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
        /// <param name="socket">Client target</param>
        void DisconnectClient(IClient client);

        /// <summary>
        /// Get all active clients
        /// </summary>
        /// <returns>Active clients</returns>
        ReadOnlyCollection<IClient> GetAllConnections();
    }
}
