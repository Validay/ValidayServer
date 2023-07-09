namespace ValidayServer.Logging.Interfaces
{
    /// <summary>
    /// Interface for logging on server 
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logging message
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="logType">Log level type</param>
        public void Log(
            string message, 
            LogType logType);
    }
}
