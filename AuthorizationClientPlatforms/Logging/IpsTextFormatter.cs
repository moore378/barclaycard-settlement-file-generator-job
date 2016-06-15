using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;

namespace AuthorizationClientPlatforms.Logging
{
    /// <summary>
    /// Text formatter 
    /// </summary>
    public class IpsTextFormatter : IEventTextFormatter
    {
        private string _dateTimeFormat;

        /// <summary>
        /// Constructor for IPS Text Formatter.
        /// </summary>
        /// <param name="dateTimeFormat">date/time format for writing the timestamp</param>
        public IpsTextFormatter(string dateTimeFormat)
        {
            _dateTimeFormat = dateTimeFormat;
        }

        /// <summary>
        /// Writes the event payload to the log sinks.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="writer"></param>
        public void WriteEvent(EventEntry entry, TextWriter writer)
        {
            string line = String.Format("{0}, {1}, {2}, {3}, {4}, ProcessId : {5}, ThreadId : {6}",
                entry.Timestamp.LocalDateTime.ToString(_dateTimeFormat),
                entry.EventId,
                entry.Schema.Level,
                entry.FormattedMessage,
                entry.Schema.EventName,
                entry.ProcessId,
                entry.ThreadId);

            writer.WriteLine(line);
        }
    }
}
