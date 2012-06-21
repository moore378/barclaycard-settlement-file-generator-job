using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TransactionManagementCommon
{
    /// <summary>
    /// The base class for objects that can log messages. This is a base class for ease of use.
    /// </summary>
    public class LoggingObject
    {
        protected void LogImportant(string message)
        {
            var temp = Logged;
            if (temp != null)
                temp(this, new LogEventArgs(message, LogLevel.Important));
        }

        protected void LogError(string message, Exception exception = null)
        {
            var temp = Logged;
            if (temp != null)
                temp(this, new LogEventArgs(message, exception));
        }

        [Conditional("DetailedLoggingEnabled")]
        protected void LogDetail(string message)
        {
            var temp = Logged;
            if (temp != null)
                temp(this, new LogEventArgs(message, LogLevel.Detail));
        }

        /// <summary>
        /// Attach children to this log function
        /// </summary>
        protected virtual void ChildLogged(object sender, LogEventArgs args)
        {
            // Default action is just bubble the log event up the chain
            var temp = Logged;
            if (temp != null)
                temp(sender, args);
        }

        /// <summary>
        /// Subscribes ChildLogged to the childs Logged event and returns the child
        /// </summary>
        /// <remarks>This is a helper function to simplify code that creates children and subscribes them</remarks>
        protected virtual T SubscribeChild<T>(T child) 
            where T : LoggingObject
        {
            child.Logged += ChildLogged;
            return child;
        }

        public event EventHandler<LogEventArgs> Logged;
    }

    public enum LogLevel { Detail, Important, Error }

    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(string message, LogLevel logLevel)
        {
            this.Message = message;
            this.Level = logLevel;
        }

        public LogEventArgs(string message, Exception exception)
        {
            this.Level = LogLevel.Error;
            this.Message = message;
            this.Exception = exception;
        }

        public string Message { get; private set; }
        public Exception Exception { get; private set; }
        public LogLevel Level { get; private set; }
    }
}
