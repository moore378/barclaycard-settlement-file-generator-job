using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics.Tracing;

namespace AuthorizationClientPlatforms.Logging
{
    [EventSource(Name = "Ips-TransactionManagement")]
    public sealed class IpsTmsEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords Diagnostic = (EventKeywords)1;
        }

        public class Tasks
        {
            public const EventTask Processor = (EventTask)1;
        }

        private static IpsTmsEventSource _log = new IpsTmsEventSource();
        private IpsTmsEventSource() { }
        public static IpsTmsEventSource Log { get { return _log; } }


        [Event(1, Message = "{0}",
            Level = EventLevel.Informational, Keywords = Keywords.Diagnostic)]
        public void LogInformational(string message)
        {
            if (this.IsEnabled()) this.WriteEvent(1, message);
        }

        [Event(2, Message = "{0}", Keywords = Keywords.Diagnostic,
            Level = EventLevel.Warning)]
        public void LogWarning(string message)
        {
            if (this.IsEnabled()) this.WriteEvent(2, message);
        }

        [Event(3, Message = "{0}", Keywords = Keywords.Diagnostic,
            Level = EventLevel.Error)]
        public void LogError(string message)
        {
            if (this.IsEnabled()) this.WriteEvent(3, message);
        }

        /*
        [Event(4, Message = "{0}: {1}", Keywords = Keywords.Diagnostic,
            Level = EventLevel.Verbose)]
        public void MessageDump( string context, object message )
        {
            if (this.IsEnabled())
            {
                System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(message.GetType());

                using (System.IO.StringWriter textWriter = new System.IO.StringWriter())
                {
                    xmlSerializer.Serialize(textWriter, message);

                    string data = textWriter.ToString();

                    this.WriteEvent(4, context, data);
                }
            };
        }
         */
    }
}
