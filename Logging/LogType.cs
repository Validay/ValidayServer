using System.ComponentModel;

namespace Validay.Logging
{
    /// <summary>
    /// Log level type enum
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Low level for little operations
        /// </summary>
        [Description("Low")]
        Low,
        /// <summary>
        /// Main level info for usally information
        /// </summary>
        [Description("Info")]
        Info,
        /// <summary>
        /// Warning level for non critical error or info
        /// </summary>
        [Description("Warning")]
        Warning,
        /// <summary>
        /// Error level for main error in server
        /// </summary>
        [Description("Error")]
        Error,
        /// <summary>
        /// Critical error level using for dangerous messages 
        /// </summary>
        [Description("Critical Error")]
        CriticalError
    }
}
