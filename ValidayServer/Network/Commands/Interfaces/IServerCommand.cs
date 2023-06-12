using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Interfaces;
using System.Collections.Generic;

namespace ValidayServer.Network.Commands.Interfaces
{
    /// <summary>
    /// Interface server command
    /// </summary>
    public interface IServerCommand
    {
        /// <summary>
        /// Id server command
        /// </summary>
        short Id { get; }

        /// <summary>
        /// Executed this server command
        /// </summary>
        /// <param name="sender">Sender client executing server command</param>
        /// <param name="managers">Collection manager</param>
        /// <param name="rawData">Raw data bytes</param>
        void Execute(
            IClient sender, 
            IReadOnlyCollection<IManager> managers,
            byte[] rawData);
    }
}
