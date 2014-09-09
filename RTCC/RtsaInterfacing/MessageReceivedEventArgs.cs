using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rtcc.RtsaInterfacing
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public byte[] Message { get; private set; }

        public MessageReceivedEventArgs(byte[] message)
        {
            this.Message = message;
        }
    }
}
