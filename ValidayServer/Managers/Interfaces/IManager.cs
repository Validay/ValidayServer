namespace ValidayServer.Managers.Interfaces
{
    /// <summary>
    /// Main interface for manager
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// Name manager
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Is this manager active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Starting this manager
        /// </summary>
        void Start();

        /// <summary>
        /// Stopping this manager
        /// </summary>
        void Stop();
    }
}