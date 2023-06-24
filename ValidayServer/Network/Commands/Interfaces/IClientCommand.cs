namespace ValidayServer.Network.Commands.Interfaces
{
    /// <summary>
    /// Interface client command
    /// </summary>
    public interface IClientCommand
    {
        /// <summary>
        /// Get raw data this client command
        /// </summary>
        /// <returns>bytes client command</returns>
        byte[] GetRawData();
    }
}