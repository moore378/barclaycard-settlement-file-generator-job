using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using AutoDatabase;

namespace Cctm.DualAuth
{
    public class DbUpdTransactionrecordcctmFinalizationParams
    {
        // public Int32 RETURN_VALUE; // Int // (Return value)
        public Decimal TransactionRecordID; // Decimal
        public String CreditCallCardEaseReference; // Char
        public String CCTrackStatus; // VarChar
        public String CreditCallAuthCode; // VarChar
        public String CCTransactionStatus; // VarChar
        public Int16 BatNum; // SmallInt
        public Decimal TTID; // Decimal
        public Int16 Status; // SmallInt
        public Int16 OldStatus; // SmallInt
    }
}
