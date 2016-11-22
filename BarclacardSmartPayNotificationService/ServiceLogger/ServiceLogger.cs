using System.Diagnostics.Tracing;

namespace ServiceLogger
{
    [EventSource(Name = "ServiceLogger")]
    internal class ServiceLogger : EventSource 
    {
        private static ServiceLogger _log = new ServiceLogger();
        private ServiceLogger() { }
        internal static ServiceLogger Log { get { return _log; } }

        [Event(1, Message = "{0}", Level = EventLevel.Critical)]
        internal void Critical(string message)
        {
            WriteEvent(1, message);
        }

        [Event(2, Message = "{0}", Level = EventLevel.Informational)]
        internal void Informational(string message)
        {
            WriteEvent(2, message);
        }
    }
}
