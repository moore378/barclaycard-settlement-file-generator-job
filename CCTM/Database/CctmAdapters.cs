using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using Cctm.Common;
using AuthorizationClientPlatforms;
using System.Threading;
using Common;
using TransactionManagementCommon.ControllerBase;

namespace Cctm.Database
{
    /// <summary>
    /// This class is just a group of server controller classes
    /// </summary>
    /// <remarks>
    /// Controllers are used here because they provide a safety and control layer between the
    /// server itself and the CCTM form which uses it. The layer automatically manages the
    /// server, controlling things like initialization, multi-threaded operations, and the
    /// behavior during times of failure (e.g. restating, retrying x number of times etc). In 
    /// order for the controller to police operations, all operations must be done through the
    /// controller class. To integrate this seamlessly and transparently into the CCTM, each 
    /// of these controllers is handled by a wrapper class which implements the original server
    /// interface. All the operations performed on the wrapper are actually performed on the 
    /// server object through the controller object.
    /// </remarks>
    internal class ServerControllers
    {
        /// <summary>
        /// Wraps an authorization controller as an Authorization Platform.
        /// </summary>
        internal class AuthorizerControllerWrapper : ControllerWrapper<IAuthorizationPlatform>, IAuthorizationPlatform
        {
            public AuthorizerControllerWrapper(ServerController<IAuthorizationPlatform> controller)
                : base(controller)
            {

            }

            public AuthorizationResponseFields Authorize(AuthorizationClientPlatforms.AuthorizationRequest request, AuthorizeMode mode)
            {
                return Controller.Perform<AuthorizationResponseFields>(
                    operation: (platform) => platform.Authorize(request, mode),
                    exceptionHandler: (exception, tried) => 
                        {
                            if (exception is AuthorizerProcessingException)
                                if (((AuthorizerProcessingException)exception).AllowRetry && (tried < 5))
                                {
                                    Thread.Sleep(1000);
                                    return OperationFailAction.RestartAndRetry;
                                }

                            return OperationFailAction.AbortAndRestart;
                        }
                    );
            }

            public IAuthorizationStatistics Statistics
            {
                get { return Controller.Get<IAuthorizationStatistics>((platform)=>platform.Statistics); }
            }
        }

        /// <summary>
        /// Wraps a database controller as a CctmDatabase
        /// </summary>
        internal class DatabaseControllerWrapper : ControllerWrapper<ICctmDatabase>, ICctmDatabase
        {
            public DatabaseControllerWrapper(ServerController<ICctmDatabase> controller):base(controller){}

            public IEnumerable<TransactionRecord> SelectNewTransactionRecords()
            {
                return Controller.Perform<IEnumerable<TransactionRecord>>(
                    operation: (database) => database.SelectNewTransactionRecords(),
                    exceptionHandler: (exception, tried) => { Thread.Sleep(1000); return OperationFailAction.AbortAndRestart; }
                    );
            }

            public void UpdateTransactionRecord(UpdatedTransactionRecord record)
            {
                Controller.Perform(
                    operation: (database) => database.UpdateTransactionRecord(record),
                    exceptionHandler: (exception, tried) => { if (tried > 2) return OperationFailAction.AbortAndRestart; Thread.Sleep(1000); return OperationFailAction.RestartAndRetry; }
                    );
            }

            public TransactionStatus? UpdateRecordStatus(int transactionRecordID, TransactionStatus oldStatus, TransactionStatus updatedStatus)
            {
                TransactionStatus? result = null;
                Controller.Perform(
                    operation: (database) => { result = database.UpdateRecordStatus(transactionRecordID, oldStatus, updatedStatus); },
                    exceptionHandler: (exception, tried) => { if (tried > 2) return OperationFailAction.AbortAndRestart; Thread.Sleep(1000); return OperationFailAction.RestartAndRetry; }
                    );
                return result;
            }


            public void UpdateTrack(int transactionRecordID, string track)
            {
                Controller.Perform(
                    operation: (database) => database.UpdateTrack(transactionRecordID, track),
                    exceptionHandler: (exception, tried) => { if (tried > 2) return OperationFailAction.AbortAndRestart; Thread.Sleep(1000); return OperationFailAction.RestartAndRetry; }
                    );
            }


            public TransactionStatus? UpdatePreauthStatus(int transactionRecordID, TransactionStatus? oldStatus, TransactionStatus updatedStatus)
            {
                TransactionStatus? result = null;
                Controller.Perform(
                    operation: (database) => { result = database.UpdatePreauthStatus(transactionRecordID, oldStatus, updatedStatus); },
                    exceptionHandler: (exception, tried) => { if (tried > 2) return OperationFailAction.AbortAndRestart; Thread.Sleep(1000); return OperationFailAction.RestartAndRetry; }
                    );
                return result;
            }


            public void UpdatePreauthRecord(TransactionRecord record, UpdatedTransactionRecord updates)
            {
                Controller.Perform(
                    operation: (database) => database.UpdatePreauthRecord(record, updates),
                    exceptionHandler: (exception, tried) => { if (tried > 2) return OperationFailAction.AbortAndRestart; Thread.Sleep(1000); return OperationFailAction.RestartAndRetry; }
                    );
            }

            public void UpdateFinalizeRecord(TransactionRecord record, UpdatedTransactionRecord updates)
            {
                Controller.Perform(
                    operation: (database) => database.UpdateFinalizeRecord(record, updates),
                    exceptionHandler: (exception, tried) => { if (tried > 2) return OperationFailAction.AbortAndRestart; Thread.Sleep(1000); return OperationFailAction.RestartAndRetry; }
                    );
            }


            public void UpdateTransactionRecordCctm(TransactionRecord record, UpdatedTransactionRecord updates)
            {
                Controller.Perform(
                    operation: (database) => database.UpdateTransactionRecordCctm(record, updates),
                    exceptionHandler: (exception, tried) => { if (tried > 2) return OperationFailAction.AbortAndRestart; Thread.Sleep(1000); return OperationFailAction.RestartAndRetry; }
                    );
            }
        }
    }

    /// <summary>
    /// Group of adapter classes for use in the CCTM
    /// </summary>
    /// <remarks>
    /// These adapter classes implement predefined abstractions using objects defined outside the 
    /// scope of manually coded CCTM. Using these abstractions aids testing (for example the unit
    /// tests), and automation (for example the ServerControllers), as well optimally defining 
    /// methods and structures best of the native language.
    /// </remarks>
    internal class Adapters
    {
        public static ICctmDatabase NewDatabaseAdapter()
        {
            return new CctmDatabaseAdapter(
                            () => new UpdateTransactionRecordCC(),
                            () => new CCTransactionsDataSetTableAdapters.QueriesTableAdapter(),
                            () => new UpdateTransactionCCTracks(),
                            () => new CCTransactionsDataSetTableAdapters.SEL_NEW_TRANSACTIONRECORDSTableAdapter());
        }

        /// <summary>
        /// Represents a database. Uses the code generated objects provided by Visual Studio from importing
        /// the database stored procedures.
        /// </summary>
        private class CctmDatabaseAdapter : ICctmDatabase
        {
            ThreadLocal<UpdateTransactionRecordCC> updateTransactionRecord;
            ThreadLocal<CCTransactionsDataSetTableAdapters.QueriesTableAdapter> updateTransactionStatus;
            ThreadLocal<UpdateTransactionCCTracks> updateTransactionStripe;
            ThreadLocal<CCTransactionsDataSetTableAdapters.SEL_NEW_TRANSACTIONRECORDSTableAdapter> selectNewTransactionRecords;

            public CctmDatabaseAdapter(Func<UpdateTransactionRecordCC> updateTransactionRecordFactory,
                Func<CCTransactionsDataSetTableAdapters.QueriesTableAdapter> updateTransactionStatusFactory,
                Func<UpdateTransactionCCTracks> updateTransactionStripeFactory,
                Func<CCTransactionsDataSetTableAdapters.SEL_NEW_TRANSACTIONRECORDSTableAdapter> selectNewTransactionRecordsFactory)
            {
                this.updateTransactionRecord = new ThreadLocal<UpdateTransactionRecordCC>(updateTransactionRecordFactory);
                this.updateTransactionStatus = new ThreadLocal<CCTransactionsDataSetTableAdapters.QueriesTableAdapter>(updateTransactionStatusFactory);
                this.updateTransactionStripe = new ThreadLocal<UpdateTransactionCCTracks>(updateTransactionStripeFactory);
                this.selectNewTransactionRecords = new ThreadLocal<CCTransactionsDataSetTableAdapters.SEL_NEW_TRANSACTIONRECORDSTableAdapter>(selectNewTransactionRecordsFactory);
            }

            public IEnumerable<TransactionRecord> SelectNewTransactionRecords()
            {
                try
                {
                    CCTransactionsDataSet.SEL_NEW_TRANSACTIONRECORDSDataTable data = selectNewTransactionRecords.Value.GetData();

                    List<TransactionRecord> result = new List<TransactionRecord>();
                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        EncryptedStripe encryptedDecodedStripe;
                        try
                        {
                            string s = data[i].CCTracks;
                            if (s.Contains("Processing") | s.Contains("CCTM"))
                                encryptedDecodedStripe = new EncryptedStripe(new byte[0]);
                            else
                                encryptedDecodedStripe = DatabaseFormats.DecodeDatabaseStripe(s);
                        }
                        catch
                        {
                            encryptedDecodedStripe = new EncryptedStripe(new byte[0]);
                        }

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
                            RefDateTime = (data[i].IsNull("ReferenceDateTime")) ? (DateTime?)null : ((DateTime)data[i].ReferenceDateTime),
                            PreauthTransactionIndex = (data[i].IsNull("AuthCCTransactionIndex")) ? (int?)null : ((int)data[i].AuthCCTransactionIndex),
                            PreauthAmountDollars = (data[i].IsNull("AuthCCAmount")) ? (int?)null : ((int)data[i].AuthCCAmount),
                        });
                    }
                    return result;
                }
                catch
                {
                    throw;
                }
            }

            public void UpdateTransactionRecord(UpdatedTransactionRecord record)
            {
                updateTransactionRecord.Value.UPD_TRANSACTIONRECORD_CREDITCALL(
                    TransactionRecordID: record.TransactionRecordID,
                    CreditCallCardEaseReference: record.CardEaseReference,
                    CreditCallAuthCode: record.AuthorizationCode, 
                    CreditCallPAN: record.PAN,
                    CreditCallExpiryDate: record.ExpiryDate,
                    CreditCallCardScheme: record.CardScheme,
                    FirstSix: record.FirstSix,
                    LastFour: record.LastFour,
                    batNum: record.BatchNum,
                    ttid: record.Ttid,
                    status: (short)record.Status
                    );
            }

            public TransactionStatus? UpdateRecordStatus(int transactionRecordID, TransactionStatus oldStatus, TransactionStatus updatedStatus)
            {
                short? result = (short?)updateTransactionStatus.Value.UPD_TRANSACTIONRECORD_STATUS(
                    TransactionRecordID: transactionRecordID, 
                    CCTransactionStatus: updatedStatus.ToText(),
                    Status: (short)updatedStatus,
                    OldStatus: (short)oldStatus);

                return (result == null) ? (TransactionStatus?)null : (TransactionStatus?)result;
            }


            public void UpdateTrack(int transactionRecordID, string track)
            {
                updateTransactionStatus.Value.UPD_TRANSACTIONRECORD_CCTRACKS(transactionRecordID, track);
            }


            public TransactionStatus? UpdatePreauthStatus(int transactionRecordID, TransactionStatus? oldStatus, TransactionStatus updatedStatus)
            {
                short? result = (short?)updateTransactionStatus.Value.UPD_TRANSACTIONAUTHORIZATION_STATUS(
                    TransactionRecordID: transactionRecordID,
                    Status: (short)updatedStatus,
                    OldStatus: (short?)oldStatus);

                return (result == null) ? (TransactionStatus?)null : (TransactionStatus?)result;
            }


            public void UpdatePreauthRecord(TransactionRecord record, UpdatedTransactionRecord updates)
            {
                var adapter = new CCTransactionsDataSetTableAdapters.QueriesTableAdapter();
                var table = adapter.UPD_TRANSACTIONAUTHORIZATION(
                    record.ID,
                    DateTime.Now,
                    updates.CardEaseReference,
                    updates.AuthorizationCode,
                    updates.PAN,
                    updates.ExpiryDate,
                    updates.CardScheme,
                    updates.FirstSix,
                    updates.LastFour,
                    updates.BatchNum,
                    updates.Ttid,
                    (short)updates.Status);
            }


            public void UpdateFinalizeRecord(TransactionRecord record, UpdatedTransactionRecord updates)
            {
                var adapter = new CCTransactionsDataSetTableAdapters.QueriesTableAdapter();
                var table = adapter.UPD_TRANSACTIONRECORD_FINALIZE(
                    record.ID,
                    DateTime.Now,
                    updates.CardEaseReference,
                    updates.AuthorizationCode,
                    updates.BatchNum,
                    updates.Ttid,
                    (short)updates.Status);
            }


            public void UpdateTransactionRecordCctm(TransactionRecord record, UpdatedTransactionRecord updates)
            {
                var adapter = new CCTransactionsDataSetTableAdapters.QueriesTableAdapter();
                var table = adapter.UPD_TRANSACTIONRECORD_CCTM(
                    record.ID,
                    updates.CardEaseReference, 
                    updates.TrackText,
                    updates.AuthorizationCode,
                    updates.PAN,
                    updates.ExpiryDate,
                    updates.CardScheme,
                    updates.FirstSix,
                    updates.LastFour,
                    updates.Status.ToText(),
                    updates.BatchNum,
                    updates.Ttid,
                    (short)updates.Status,
                    (short)record.Status);
            }

        }

        class UpdateTransactionCCTracks : CCTransactionsDataSetTableAdapters.QueriesTableAdapter
        {
            public new virtual int UPD_TRANSACTIONRECORD_CCTRACKS(
              System.Nullable<decimal> TransactionRecordID, string CCTracksString)
            {
                return base.UPD_TRANSACTIONRECORD_CCTRACKS(TransactionRecordID, CCTracksString);
            }
        }
        
        class UpdateTransactionRecordCC : CCTransactionsDataSetTableAdapters.QueriesTableAdapter
        {
            public virtual object UPD_TRANSACTIONRECORD_CREDITCALL(
                System.Nullable<decimal> TransactionRecordID,
                string CreditCallCardEaseReference,
                string CreditCallAuthCode,
                string CreditCallPAN,
                string CreditCallExpiryDate,
                string CreditCallCardScheme,
                string FirstSix,
                string LastFour,
                short batNum,
                int ttid,
                short status
                )
            {
                return base.UPD_TRANSACTIONRECORD_CREDITCALL(
                    TransactionRecordID: TransactionRecordID, 
                    CreditCallCardEaseReference: CreditCallCardEaseReference,
                    CreditCallAuthCode: CreditCallAuthCode,
                    CreditCallPAN: CreditCallPAN,
                    CreditCallExpiryDate: CreditCallExpiryDate,
                    CreditCallCardScheme: CreditCallCardScheme, 
                    CCFirstSix: FirstSix, 
                    CCLastFour: LastFour,
                    BatNum: batNum,
                    TTID: ttid,
                    Status: status
                );
            }
        }
    }
}
