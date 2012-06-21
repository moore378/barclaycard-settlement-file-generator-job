using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rtcc.Main;
using Rtcc.RtsaInterfacing;

namespace Rtcc.Dummy
{
    public class DummyRtsaConnection : RtsaConnection
    {
        public override void SendMessage(RawDataMessage msg)
        {
            // Do nothing
        }

        public override void Disconnect()
        {
            // Do nothing
        }

        public void SimulateMessageReceived(RawDataMessage msg)
        {
            DoMessageReceived(msg);
        }
    }
}
