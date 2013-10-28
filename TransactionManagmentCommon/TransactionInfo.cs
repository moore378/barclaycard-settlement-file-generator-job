using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    /// <summary>
    /// This class holds information about the transaction being processed, 
    /// and can be passed to functions which need it.
    /// </summary>
    public class TransactionInfo
    {
        public DateTime StartDateTime;
        public decimal TransactionIndex;
        public string MeterSerialNumber;
        public decimal AmountDollars;
        public DateTime? RefDateTime;

        public TransactionInfo(DateTime startDateTime,
            int transactionIndex,
            string meterSerialNumber,
            decimal amountDollars,
            DateTime? refDateTime)
        {
            this.StartDateTime = startDateTime;
            this.TransactionIndex = transactionIndex;
            this.MeterSerialNumber = meterSerialNumber;
            this.AmountDollars = amountDollars;
            this.RefDateTime = refDateTime;
        }
    }
}
