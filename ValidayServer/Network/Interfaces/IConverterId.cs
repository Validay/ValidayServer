namespace ValidayServer.Network.Interfaces
{
    /// <summary>
    /// Interface for convert bytes to TId type
    /// </summary>
    /// <typeparam name="TId">Id type</typeparam>
    public interface IConverterId<TId>
    {
        /// <summary>
        /// Convert bytes to TId
        /// </summary>
        /// <param name="bytes">Bytes data</param>
        /// <returns>TId instance</returns>
        TId Convert(byte[] bytes);
    }
}