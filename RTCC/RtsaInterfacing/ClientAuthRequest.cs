using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using Common;

namespace Rtcc.RtsaInterfacing
{
    public class ClientAuthRequest
    {
        public string TerminalSerialNumber;
        public DateTime StartDateTime;
        public int TransactionType;
        public int TransactionIndex;
        public EncryptionMethod EncryptionMethod;
        public int KeyVersion;
        public int RequestType; // pre-auth or auth
        public string UniqueRecordNumber;
        public decimal AmountDollars;
        public string TransactionDesc;
        public string Invoice;
        public EncryptedStripe EncryptedTrack;
        public int PurchasedTime;
        public long UniqueNumber2;
        public int Flags;

        public ClientAuthRequest(
            string TerminalSerialNumber,
            DateTime StartDateTime,
            int TransactionType,
            int TransactionIndex,
            EncryptionMethod EncryptionMethod,
            int KeyVersion,
            int RequestType, // pre-auth or auth
            string UniqueRecordNumber,
            decimal AmountDollars,
            string TransactionDesc,
            string Invoice,
            EncryptedStripe EncryptedTrack,
            int PurchasedTime,
            long uniqueNumber2,
            int flags)
        {
            this.TerminalSerialNumber = TerminalSerialNumber;
            this.StartDateTime = StartDateTime;
            this.TransactionType = TransactionType;
            this.TransactionIndex = TransactionIndex;
            this.EncryptionMethod = EncryptionMethod;
            this.KeyVersion = KeyVersion;
            this.RequestType = RequestType; // pre-auth or auth
            this.UniqueRecordNumber = UniqueRecordNumber;
            this.AmountDollars = AmountDollars;
            this.TransactionDesc = TransactionDesc;
            this.Invoice = Invoice;
            this.EncryptedTrack = EncryptedTrack;
            this.PurchasedTime = PurchasedTime;
            this.UniqueNumber2 = uniqueNumber2;
            this.Flags = flags;
        }

        public void Validate(string failStatus)
        {
            // The meter ID must be numerical
            try { Int32.Parse(TerminalSerialNumber); }
            catch { throw new ValidationException("Invalid meter ID: " + TerminalSerialNumber); }
        }
    }
}
