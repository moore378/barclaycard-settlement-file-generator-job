using System;

namespace TransactionManagement
{
    /// <summary>
    /// This class holds information about the transaction being processed, 
    /// and can be passed to functions which need it.
    /// </summary>
    public struct TransactionInfo
    {
        public DateTime startDateTime;
        public int transactionIndex;
        public string meterSerialNumber;
        public string ccAmount;
        public int ccBaseAmount;
        public string idString;
        public string clearingPltfrm;
        public string CCTransactionKey;
    }
}