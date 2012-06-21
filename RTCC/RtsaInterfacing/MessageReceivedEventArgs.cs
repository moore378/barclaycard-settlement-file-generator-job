using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rtcc.RtsaInterfacing
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public RawDataMessage Message { get; private set; }

        public MessageReceivedEventArgs(RawDataMessage message)
        {
            this.Message = message;
        }
    }
}
