using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network.Commands.Interfaces
{
    /// <summary>
    /// Interface server command
    /// </summary>
    public interface IServerCommand
    {
        /// <summary>
        /// Executed this server command
        /// </summary>
        /// <param name="sender">Sender client executing server command</param>
        /// <param name="rawData">Raw data bytes</param>
        void Execute(
            IClient sender,
            byte[] rawData);
    }
}
