using ValidayServer.Managers.Interfaces;
using System;

namespace ValidayServer.Managers
{
    /// <summary>
    /// Default manager factory
    /// </summary>
    public class ManagerFactory : IManagerFactory
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IManager CreateManager<T>() 
            where T : IManager
        {
            return Activator.CreateInstance<T>();
        }
    }
}
