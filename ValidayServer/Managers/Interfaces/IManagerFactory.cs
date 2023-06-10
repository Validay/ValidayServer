namespace ValidayServer.Managers.Interfaces
{
    /// <summary>
    /// Main interface for server factory
    /// </summary>
    public interface IManagerFactory
    {
        /// <summary>
        /// Create new object manager
        /// </summary>
        /// <typeparam name="T">Type manager</typeparam>
        /// <returns>Server manager object type of T</returns>
        IManager CreateManager<T>() 
            where T : IManager;
    }
}
