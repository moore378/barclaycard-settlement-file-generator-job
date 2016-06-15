using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using Common;

namespace Cctm.Common
{
    public class TransactionRecord
    {
        // Identity
        public int ID;
        public int PoleID;
        public int TerminalID;
        public string UniqueRecordNumber;
        public DateTime DateTimeCreated;
        public string TerminalSerialNumber;
        public TransactionMode Mode;

        // Transaction
        public DateTime StartDateTime;
        public decimal TotalCredit;
        public decimal TimePurchased;
        public decimal TotalParkingTime;
        public decimal AmountDollars;

        // Card info
        public EncryptedStripe EncryptedStripe;
        public EncryptionMethod EncryptionMethod;
        public int TransactionIndex;
        public int KeyVersion;

        // State
        public string StatusString;
        public TransactionStatus Status;
        public TransactionStatus? PreauthStatus;

        // Auth in
        public string MerchantID;
        public string MerchantPassword;
        public string ClearingPlatform;

        // Extra processor settings.
        // NOTE: Every different processor will have different meanings
        // These are fields that are overloaded for generic processing.
        public string MerchantNumber;
        public string CashierNumber;
        
        public int? PreauthTtid;
        //public string AuthCode;
        //public string CardSchema;

        public DateTime? RefDateTime;

        public int? PreauthTransactionIndex;

        public decimal? PreauthAmountDollars;
               
        public void Validate(string failStatus)
        {
            // A few sanity checks to see that we have a reasonable transaction record
            if (
                (ID <= 0)
                || (PoleID <= 0)
                || (TerminalID <= 0))
                throw new ValidationException("Invalid transaction record IDs", failStatus);
            if (
                (StartDateTime.AddYears(-1).CompareTo(DateTime.Now) > 0) // In the future
                || (StartDateTime.AddYears(5).CompareTo(DateTime.Now) < 0) // Further than 5 years ago
                || (StartDateTime.AddYears(5).CompareTo(DateTime.Now) < 0))
                throw new ValidationException("Invalid transaction record date-times", failStatus);

            if (EncryptedStripe.Data.Length <= 0)
                throw new ValidationException("Invalid transaction record track", failStatus);
        }
    }
}
