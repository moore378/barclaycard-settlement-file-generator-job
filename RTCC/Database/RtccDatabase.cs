using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using Rtcc.Main;

using System.Threading.Tasks;

using AutoDatabase;

namespace Rtcc.Database
{
    /// <summary>
    /// Used to store the result from calling InsLiveTransactionrecord
    /// </summary>
    public class DbInsLiveResult
    {
        /// <summary>
        /// Transaction record ID.
        /// </summary>
        public decimal TransactionRecordID;

        /// <summary>
        /// If transaction record ID already existed before. Should be a boolean...
        /// </summary>
        public int Dup;
    }

    [DatabaseInterface(StoredProcNameConverter = typeof(PascalCaseToUnderscoreConverter))]
    public interface IRtccDatabase
    {
        Task<CCProcessorInfo> SelRtccProcessor(string TerminalSerNo);

        Task<DbInsLiveResult> InsLiveTransactionrecord
            (
            string TerminalSerNo,
            string ElectronicSerNo,
            decimal TransactionType,
            DateTime StartDateTime,
            decimal TotalCredit,
            decimal TimePurchased,
            decimal TotalParkingTime,
            decimal CCAmount,
            string CCTracks,
            string CCTransactionStatus,
            decimal CCTransactionIndex,
            string CoinCount,
            decimal EncryptionVer,
            decimal KeyVer,
            string UniqueRecordNumber,
            string CreditCallCardEaseReference,
            string CreditCallAuthCode,
            string CreditCallPAN,
            string CreditCallExpiryDate,
            string CreditCallCardScheme,
            string CCFirstSix,
            string CCLastFour,
            long UniqueNumber2,
            short Mode,
            short Status,
            Int64 CardHash);

        [return: DatabaseReturn(ColumnIndex = 0)]
        Task<decimal> SelTransrecidFromUniquerec(string UniqueRecordNumber, long UniqueNumber2);

        Task UpdLiveTransactionrecord(
            decimal TransactionRecordID,
            string CCTracks,
            string CCTransactionStatus,
            string CreditCallAuthCode,
            string CreditCallCardScheme,
            string CreditCallPAN,
            short BatNum,
            int TTID,
            short Status,
            decimal CCFee
            );

        Task UpdTransactionrecordStatus(int TransactionRecordID, string CCTransactionStatus, short Status, short OldStatus);

        Task UpdTransactionauthorization(
            int TransactionRecordID,
            DateTime SettlementDateTime,
            string CreditCallCardEaseReference,
            string CreditCallAuthCode,
            string CreditCallPAN,
            string CreditCallExpiryDate,
            string CreditCallCardScheme,
            string CCFirstSix,
            string CCLastFour,
            short BatNum,
            int TTID,
            short Status
            // NOTE: Although CCHash is a parameter, RTCC does not use it.
            );
    }

    class DatabaseDebugLog : IDatabaseTracker
    {
        class MethodLog : IDatabaseMethodTracker
        {
            private string name;

            public MethodLog(string name)
            {
                this.name = name;
            }

            public void Successful(object result)
            {
                System.Diagnostics.Debug.WriteLine(name + " was successful");
            }

            public void Failed()
            {
                System.Diagnostics.Debug.WriteLine(name + " failed");
            }
        }

        public IDatabaseMethodTracker StartingQuery(string name, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine("-> " + name);
            return new MethodLog(name);
        }
    }

    public class RtccDatabase : LoggingObject
    {
        private IRtccDatabase database;

        public RtccDatabase()
        {
            var connectionSource = new ConnectionSource(Properties.Settings.Default.ConnectionString);

            database = AutoDatabaseBuilder.CreateInstance<IRtccDatabase>(connectionSource, new DatabaseDebugLog());
        }

        public virtual decimal InsertLiveTransactionRecord(
            string TerminalSerNo,
            string ElectronicSerNo,
            decimal TransactionType,
            DateTime StartDateTime,
            decimal TotalCredit,
            decimal TimePurchased,
            decimal TotalParkingTime,
            decimal CCAmount,
            string CCTracks,
            string CCTransactionStatus,
            decimal CCTransactionIndex,
            string CoinCount,
            decimal EncryptionVer,
            decimal KeyVer,
            string UniqueRecordNumber,
            long UniqueRecordNumber2,
            string CreditCallCardEaseReference,
            string CreditCallAuthCode,
            string CreditCallPAN,
            string CreditCallExpiryDate,
            string CreditCallCardScheme,
            string FirstSixDigits,
            string LastFourDigits,
            short mode,
            short status,
            Int64 CardHash)
        {
            decimal transactionRecordID = 0;

            DbInsLiveResult row = null;

            //System.Threading.Thread.Sleep(40000); // Dev-level testing to verify ES-79.

            var insertTask = database.InsLiveTransactionrecord(
                TerminalSerNo,
                ElectronicSerNo ?? "",
                TransactionType,
                StartDateTime,
                TotalCredit,
                TimePurchased,
                TotalParkingTime,
                CCAmount,
                CCTracks,
                CCTransactionStatus,
                CCTransactionIndex,
                CoinCount,
                EncryptionVer,
                KeyVer,
                UniqueRecordNumber,
                CreditCallCardEaseReference,
                CreditCallAuthCode,
                CreditCallPAN,
                CreditCallExpiryDate,
                CreditCallCardScheme,
                FirstSixDigits,
                LastFourDigits,
                UniqueRecordNumber2,
                mode,
                status,
                CardHash);

            insertTask.ContinueWith( (t) => { row = t.Result; });

            Task.WaitAll(insertTask);

            try
            {
                // Get the only row returned back by the stored procedure.
                // Should be one row in the normal case of insertion
                // and also in the duplicate record case.
                if (null == row)
                {
                    throw new Exception("Unknown processing error with live transaction insertion");
                }
                // If this is a duplicate transaction then stop processing.
                // Somehow Session Manager added the transaction as an offline
                // transaction and the transaction needs to be processed by
                // CCTM instead.
                else if (1 == row.Dup)
                {
                    throw new Exception(String.Format("RTCC encounter duplicate transaction error for meter {0} and transaction record ID of {1}", TerminalSerNo, row.TransactionRecordID));
                }
                else
                {
                    // Return back the transaction record ID.
                    transactionRecordID = row.TransactionRecordID;
                }
            }
            catch (Exception e)
            {
                LogError("Database error in inserting the transaction record. \n" + e.ToString(), e);
                throw;
            }

            // Be consistent with throwing an error when not getting the transaction record ID.
            if (0 == transactionRecordID)
                throw new Exception("Error calling SEL_TRANSRECID_FROM_UNIQUEREC: returned null");
            else
                return transactionRecordID;
        }

        public virtual void UpdateLiveTransactionRecord(
            decimal transactionRecordID,
            string tracks,
            string statusString,
            string authCode,
            string cardType,
            string obscuredPan,
            short batchNum,
            int ttid,
            short status,
            decimal ccFee)
        {
            try
            {
                LogDetail("Calling database UPD_LIVE_TRANSACTIONRECORD. (TransactionRecordID=" + transactionRecordID.ToString()
                      + ";CCTracks=" + tracks
                      + ";CCTransactionStatus=" + status
                      + ";CreditCallAuthCode=" + authCode
                      + ";CreditCallCardScheme=" + cardType
                      + ";CreditCallPAN=" + obscuredPan
                      + ";CCFee=" + ccFee.ToString()
                      + ")");

                var task = database.UpdLiveTransactionrecord(transactionRecordID, tracks, statusString, authCode, cardType, obscuredPan, batchNum, ttid, status, (int) ccFee);

                task.Wait();
            }
            catch (Exception e)
            {
                LogError("Database UPD_LIVE_TRANSACTIONRECORD error. \n" + e.Message, e);
                throw;
            }
        }

        public virtual void UpdateTransactionStatus(
            int transactionRecordID, TransactionStatus oldStatus, TransactionStatus newStatus, string newStatusStr)
        {
            var task = database.UpdTransactionrecordStatus(transactionRecordID, newStatusStr, (short)newStatus, (short)oldStatus);
            task.Wait();
        }

        public virtual CCProcessorInfo GetRtccProcessorInfo(string terminalSerialNumber)
        {
            CCProcessorInfo info = null;

            try
            {
                var task = database.SelRtccProcessor(terminalSerialNumber)
                    .ContinueWith((t) => info = t.Result);

                task.Wait();

            }
            catch (AggregateException e)
            {
                var flattened = e.Flatten();
                var inner = flattened.InnerExceptions;

                if (inner[0] is System.InvalidOperationException)
                {
                    throw new Exception("Error getting processor information from database: No data returned");
                }
                else
                {
                    throw new Exception("Error getting processor information from database", inner[0]);
                }
            }
            catch (Exception)
            {
                throw new Exception("Error getting processor information from database: unknown error");
            }

            return info;
        }

        public virtual void UpdatePreauth(
            int transactionRecordID, 
            DateTime settlementDateTime, 
            string reference, 
            string authCode, 
            string pan, 
            string expiryDate, 
            string cardScheme, 
            string firstSix, 
            string lastFour, 
            short batchNum, 
            int ttid, 
            short status)
        {
            var task = database.UpdTransactionauthorization(
                transactionRecordID, 
                settlementDateTime, 
                reference, 
                authCode, 
                pan, 
                expiryDate, 
                cardScheme, 
                firstSix, 
                lastFour, 
                batchNum, 
                ttid, 
                status);

            task.Wait();
        }

        private T readVal<T>(Func<T> get, T defaultVal)
        {
            try
            {
                return get();
            }
            catch (Exception e)
            {
                LogError(e.Message, e);
                return defaultVal;
            }
        }
    }
}
