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
    [DatabaseInterface(StoredProcNameConverter = typeof(PascalCaseToUnderscoreConverter))]
    public interface IRtccDatabase
    {
        Task<CCProcessorInfo> SelRtccProcessor(string TerminalSerNo);

        Task InsLiveTransactionrecord
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
            short Status);

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
            decimal CCFee,
            Int64 CCHash
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
            short status)
        {
            decimal transactionRecordID = 0;

            //var adapter = new DataSet1TableAdapters.QueriesTableAdapter();
            //adapter.INS_LIVE_TRANSACTIONRECORD(
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
                status);

            Task.WaitAll(insertTask);

            try
            {
                LogDetail("Calling database SEL_TRANSRECID_FROM_UNIQUEREC with UniqueRecordNumber=" + UniqueRecordNumber.ToString());
                //result = adapter.SEL_TRANSRECID_FROM_UNIQUEREC(UniqueRecordNumber, UniqueRecordNumber2);

                var task = database.SelTransrecidFromUniquerec(UniqueRecordNumber, UniqueRecordNumber2)
                    .ContinueWith((t) => transactionRecordID = t.Result);

                task.Wait();

                LogDetail("Database SEL_TRANSRECID_FROM_UNIQUEREC returned (" + transactionRecordID.ToString() + ")");
            }
            catch (Exception e)
            {
                LogError("Database SEL_TRANSRECID_FROM_UNIQUEREC error. \n" + e.ToString(), e);
                throw;
            }

            if (0 == transactionRecordID)
            {
                throw new Exception("Error calling SEL_TRANSRECID_FROM_UNIQUEREC: returned null");
            }

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
            decimal ccFee,
            Int64 ccHash )
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

                var task = database.UpdLiveTransactionrecord(transactionRecordID, tracks, statusString, authCode, cardType, obscuredPan, batchNum, ttid, status, (int) ccFee, ccHash);

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
