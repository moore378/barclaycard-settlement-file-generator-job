using System;
using System.Linq;
using System.IO;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;


namespace ServiceLogger
{
    class LoggerTextFormatter : IEventTextFormatter
    {
        public void WriteEvent(EventEntry entry, TextWriter writer)
        {
            string payload = string.Join(";", entry.Payload.Select(x => x.ToString()));
            string line = String.Format("{0}, {1}, {2}, {3}, {4}, ProcessId : {5}, ThreadId : {6}",
                entry.Timestamp.LocalDateTime.ToString("MM - dd - yyyy HH:mm:ss:ffff"),
                //entry.Timestamp,
                entry.EventId,
                entry.Schema.Level,
                payload,
                entry.Schema.EventName,
                entry.ProcessId,
                entry.ThreadId);

            writer.WriteLine(line);
        }
    }
}
