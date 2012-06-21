using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rtcc.RtsaInterfacing
{
    public struct ClientAuthResponse
    {
        public int Accepted; // 0 = declined, 1 = accepted, 2 = system busy
        public string ReceiptReference;
        public string ResponseCode;
        public decimal AmountDollars;
    }
}
