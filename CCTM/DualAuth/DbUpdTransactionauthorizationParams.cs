using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cctm.DualAuth
{
    public class DbUpdTransactionauthorizationParams
    {
        public Decimal TransactionRecordID; // Decimal
        public DateTime SettlementDateTime; // DateTime
        public String CreditCallCardEaseReference; // Char
        public String CreditCallAuthCode; // VarChar
        public String CreditCallPAN; // VarChar
        public String CreditCallExpiryDate; // VarChar
        public String CreditCallCardScheme; // VarChar
        public String CCFirstSix; // Char
        public String CCLastFour; // Char
        public Int16 BatNum; // SmallInt
        public Decimal TTID; // Decimal
        public Int16 Status; // SmallInt
        public Int64 CCHash; // BigInt
    }
}
