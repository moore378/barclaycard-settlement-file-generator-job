using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refunder
{
    public class PendingRefund
    {
        public Decimal TransactionRecordID; // decimal
        public Decimal Credit; // money
        public String CreditCallPAN; // varchar
        public String CreditCallCardEaseReference; // char
        public Decimal CCAmount; // money
        public DateTime StartDateTime; // datetime
        public String CCTerminalID; // char
        public String CCTransactionKey; // varchar
        public String CCClearingPlatform; // varchar
        public Decimal? TTID; // decimal
        public Int16? BatNum; // smallint
        public String CCTracks;
        public Decimal KeyVer;
        public String CCExpiryDate;
    }
}
