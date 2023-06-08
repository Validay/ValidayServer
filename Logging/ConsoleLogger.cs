﻿using Validay.Logging.Interfaces;
using Validay.Extensions;
using System;

namespace Validay.Logging
{
    /// <summary>
    /// Main logger for server room
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Log level for messages
        /// </summary>
        public LogType LogLevel { get; set; }

        /// <summary>
        /// Main constructor for ConsoleLogger
        /// </summary>
        /// <param name="logLevel">Log level for message</param>
        public ConsoleLogger(LogType logLevel)
        {
            LogLevel = logLevel;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Log(
            string message, 
            LogType logType)
        {
            if ((int)LogLevel <= (int)logType)
                Console.WriteLine($"{logType.GetDisplayName()}: {message}");
        }
    }
}
