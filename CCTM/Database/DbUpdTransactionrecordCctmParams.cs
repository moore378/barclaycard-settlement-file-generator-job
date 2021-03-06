﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cctm.Database
{
    public class DbUpdTransactionrecordCctmParams
    {
        public Decimal TransactionRecordID; // Decimal
        public String CreditCallCardEaseReference; // Char
        public String CCTrackStatus; // VarChar
        public String CreditCallAuthCode; // VarChar
        public String CreditCallPAN; // VarChar
        public String CreditCallExpiryDate; // VarChar
        public String CreditCallCardScheme; // VarChar
        public String CCFirstSix; // Char
        public String CCLastFour; // Char
        public String CCTransactionStatus; // VarChar
        public Int16 BatNum; // SmallInt
        public Decimal TTID; // Decimal
        public Int16 Status; // SmallInt
        public Int16 OldStatus; // SmallInt

        /// <summary>
        /// If there are any additional credit card fees that were added on 
        /// by the processor which makes the authorization request value
        /// different than the authorized amount value.
        /// </summary>
        public Int16 CCFee; // SmallInt,

        public Int64 CCHash; // BigInt
    }
}
