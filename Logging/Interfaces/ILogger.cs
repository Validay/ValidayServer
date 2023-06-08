﻿namespace Validay.Logging.Interfaces
{
    /// <summary>
    /// Interface for logging on server room 
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logging message
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="logType">Log level type</param>
        void Log(
            string message, 
            LogType logType);
    }
}
