namespace ValidayServer.Network.Commands.Interfaces
{
    /// <summary>
    /// Interface client command
    /// </summary>
    /// <typeparam name="TId">Type id server command</typeparam>
    public interface IClientCommand<TId>
    {
        /// <summary>
        /// Id client command
        /// </summary>
        TId Id { get; }

        /// <summary>
        /// Get raw data this client command
        /// </summary>
        /// <returns>bytes client command</returns>
        byte[] GetRawData();
    }
}