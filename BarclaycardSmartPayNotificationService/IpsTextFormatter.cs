﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;

namespace BarclaycardSmartPayNotificationService
{
    /// <summary>
    /// Text formatter 
    /// </summary>
    public class IpsTextFormatter : IEventTextFormatter
    {
        //private string _dateTimeFormat;

        /// <summary>
        /// Constructor for IPS Text Formatter.
        /// </summary>
        /// <param name="dateTimeFormat">date/time format for writing the timestamp</param>
        //public IpsTextFormatter(string dateTimeFormat)
        //{
        //    _dateTimeFormat = dateTimeFormat;
        //}

        /// <summary>
        /// Writes the event payload to the log sinks.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="writer"></param>
        public void WriteEvent(EventEntry entry, TextWriter writer)
        {
            string payload = string.Join(";", entry.Payload.Select(x => x.ToString()));
            string line = String.Format("{0}, {1}, {2}, {3}, {4}, ProcessId : {5}, ThreadId : {6}",
                entry.Timestamp.LocalDateTime.ToString("MM - dd - yyyy HH:mm:ss:ffff"),
                //entry.Timestamp,
                entry.EventId,
                entry.Schema.Level,
                entry.FormattedMessage,
                payload,
                entry.Schema.EventName,
                entry.ProcessId,
                entry.ThreadId);

            writer.WriteLine(line);
        }

    }
}