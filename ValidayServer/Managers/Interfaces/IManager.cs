using ValidayServer.Logging.Interfaces;
using ValidayServer.Network.Interfaces;

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
        /// Get active this manager
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
