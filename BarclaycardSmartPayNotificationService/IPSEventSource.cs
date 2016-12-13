using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarclaycardSmartPayNotificationService
{
    [EventSource(Name = "IPSEventSource")]
    internal class IpsEventSource : EventSource
    {
        
        private static IpsEventSource _log = new IpsEventSource();
        private IpsEventSource() { }
        internal static IpsEventSource Log { get { return _log; } }

   

        [Event(1, Message = "{0}", Level = EventLevel.Critical)]
        internal void Critical(string message)
        {
            WriteEvent(1, message);
        }

        [Event(2, Message = "{0}", Level = EventLevel.Warning)]
        internal void Warning(string message)
        {
            WriteEvent(2, message);
        }

        [Event(3, Message = "{0}", Level = EventLevel.Informational)]
        internal void Informational(string message)
        {
            WriteEvent(3, message);
        }

    }
}
