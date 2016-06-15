using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cctm.Database
{
    public class DbTransactionRecord
    {
        public Decimal TransactionRecordID; // decimal
        public Decimal PoleID; // decimal
        public Decimal TerminalID; // decimal
        public DateTime StartDateTime; // datetime
        public Decimal TotalCredit; // money
        public Decimal TimePurchased; // decimal
        public Decimal TotalParkingTime; // decimal
        public Decimal CCAmount; // money
        public String CCTracks; // varchar
        public String CCTransactionStatus; // varchar
        public Decimal CCTransactionIndex; // decimal
        public Decimal EncryptionVer; // decimal
        public Decimal KeyVer; // decimal
        public String UniqueRecordNumber; // char
        public DateTime DateTimeCreated; // datetime
        public DateTime DateTimeModified; // datetime
        public String TerminalSerNo; // varchar
        public String CCTerminalID; // char
        public String CCTransactionKey; // varchar
        public String CCClearingPlatform; // varchar
        public String MerchantNumber; // Processor-specific setting that also needs to be sent out for every authorization.
        public String CashierNumber; // Processor-specific setting that also needs to be sent out for every authorization.
        public Int32 PreAuth; // int
        public Int16 Status; // smallint
        public Int16 Mode; // smallint
        public Int16? AuthStatus; // smallint
        public Decimal? AuthTTID; // decimal
        public DateTime? ReferenceDateTime; // datetime
        public Decimal? AuthCCTransactionIndex; // decimal
        public Decimal? AuthCCAmount; // money
    }
}
