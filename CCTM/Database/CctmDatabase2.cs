using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cctm.Common;
using TransactionManagementCommon;
using TransactionManagementCommon.ControllerBase;
using System.Threading.Tasks;
using Common;

namespace Cctm.Database
{
    //TODO: This is a prospective replacement for ICctmDatabase.
    /// <summary>
    /// Access to the database is through this class
    /// </summary>
    class CctmDatabase2 : LoggingObject
    {
        /// <summary>
        /// Gets transactions that are classified as new (waiting to be captured by CCTM)
        /// </summary>
        /// <returns>Returns new transactions</returns>
        public virtual IEnumerable<TransactionRecord> GetNewTransactionRecords()
        {
            CCTransactionsDataSet.SEL_NEW_TRANSACTIONRECORDSDataTable data;

            var adapter = new Database.CCTransactionsDataSetTableAdapters.SEL_NEW_TRANSACTIONRECORDSTableAdapter();
            data = adapter.GetData();
            
            List<TransactionRecord> result = new List<TransactionRecord>(data.Count);

            for (int i = 0; i < data.Count; i++)
            {
                try
                {
                    EncryptedStripe encryptedDecodedStripe;
                    if (data[i].AuthStatus == 3)
                        encryptedDecodedStripe = DatabaseFormats.DecodeDatabaseStripe(data[i].CCTracks);
                    else
                        encryptedDecodedStripe = new EncryptedStripe(new byte[0]);

                    result.Add(new TransactionRecord()
                    {
                        AmountDollars = data[i].CCAmount,
                        ClearingPlatform = data[i].CCClearingPlatform,
                        DateTimeCreated = data[i].DateTimeCreated,
                        EncryptedStripe = encryptedDecodedStripe,
                        EncryptionMethod = DatabaseFormats.decodeDatabaseEncryptionMethod(data[i].EncryptionVer),
                        ID = decimal.ToInt32(data[i].TransactionRecordID),
                        KeyVersion = decimal.ToInt32(data[i].KeyVer),
                        MerchantID = data[i].CCTerminalID,
                        MerchantPassword = data[i].CCTransactionKey,
                        PoleID = decimal.ToInt32(data[i].PoleID),
                        StartDateTime = data[i].StartDateTime,
                        StatusString = data[i].CCTransactionStatus,
                        TerminalID = decimal.ToInt32(data[i].TerminalID),
                        TerminalSerialNumber = data[i].TerminalSerNo,
                        TimePurchased = data[i].TimePurchased,
                        TotalCredit = data[i].TotalCredit,
                        TotalParkingTime = data[i].TotalParkingTime,
                        TransactionIndex = decimal.ToInt32(data[i].CCTransactionIndex),
                        UniqueRecordNumber = data[i].UniqueRecordNumber,
                        Status = (TransactionStatus)data[i].Status,
                        Mode = (TransactionMode)data[i].Mode,
                        PreauthStatus = (data[i].IsNull("AuthStatus")) ? (TransactionStatus?)null : ((TransactionStatus)data[i].AuthStatus),
                        PreauthTtid = (data[i].IsNull("AuthTTID")) ? (int?)null : ((int)data[i].AuthTTID),
                        PreauthTransactionIndex = (data[i].IsNull("AuthCCTransactionIndex")) ? (int?)null : ((int)data[i].AuthCCTransactionIndex),
                        MerchantNumber = data[i].MerchantNumber, // Processor-specific setting that also needs to be sent out for every authorization.
                        CashierNumber = data[i].CashierNumber // Processor-specific setting that also needs to be sent out for every authorization.
                    });
                }
                catch (Exception error)
                {
                    LogError("Error reading new transaction from database. ID " + data[i].TransactionRecordID, error);
                }
            }

            return result;
        }

        /// <summary>
        /// Updates chosen transaction fields in the database
        /// </summary>
        /// <param name="record"></param>
        public virtual void UpdateTransactionRecord(TransactionRecord from)
        {
            var adapter = new Database.CCTransactionsDataSetTableAdapters.QueriesTableAdapter();

            throw new NotImplementedException();
            
        }

        /// <summary>
        /// Updates the status for a transaction in the database, and then updates the transaction object to match.
        /// </summary>
        /// <param name="transaction">The transaction to update.</param>
        /// <param name="newStatus">The new status for the transaction</param>
        /// <returns>Returns false if the original transaction status (in "transaction") doesnt match whats in the database</returns>
        public virtual bool UpdateRecordStatus(TransactionRecord transaction, TransactionStatus newStatus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update the preauth status of a transaction in the database (and update the transaction object to match).
        /// </summary>
        /// <param name="transaction">Transaction to update</param>
        /// <param name="newStatus">New status</param>
        /// <returns>Return false if the original preauth status doesnt match</returns>
        public virtual bool UpdatePreauthStatus(TransactionRecord transaction, TransactionStatus newStatus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the track string in the database
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="track"></param>
        public virtual void UpdateTrack(TransactionRecord transaction, string track)
        {
            throw new NotImplementedException();
        }
    }
}
