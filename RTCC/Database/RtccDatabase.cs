using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using Rtcc.Main;

namespace Rtcc.Database
{
    public class RtccDatabase : LoggingObject
    {
        public virtual decimal InsertLiveTransactionRecord(
            string TerminalSerNo,
            string ElectronicSerNo,
            decimal? TransactionType,
            DateTime? StartDateTime,
            decimal? TotalCredit,
            decimal? TimePurchased,
            decimal? TotalParkingTime,
            decimal? CCAmount,
            string CCTracks,
            string CCTransactionStatus,
            decimal? CCTransactionIndex,
            string CoinCount,
            decimal? EncryptionVer,
            decimal? KeyVer,
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
            var adapter = new DataSet1TableAdapters.QueriesTableAdapter();
            adapter.INS_LIVE_TRANSACTIONRECORD(
                TerminalSerNo,
                ElectronicSerNo,
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
            object result = null;

            try
            {
                LogDetail("Calling database SEL_TRANSRECID_FROM_UNIQUEREC with UniqueRecordNumber=" + UniqueRecordNumber.ToString());
                result = adapter.SEL_TRANSRECID_FROM_UNIQUEREC(UniqueRecordNumber, UniqueRecordNumber2);
                if (result != null)
                    LogDetail("Database SEL_TRANSRECID_FROM_UNIQUEREC returned (" + result.ToString() + ")");

            }
            catch (Exception e)
            {
                LogError("Database SEL_TRANSRECID_FROM_UNIQUEREC error. \n" + e.ToString(), e);
                throw;
            }

            if (result == null)
                throw new Exception("Error calling SEL_TRANSRECID_FROM_UNIQUEREC: returned null");
            else
                return (decimal)result;
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
            short status
            )
        {
            try
            {
                LogDetail("Calling database UPD_LIVE_TRANSACTIONRECORD. (TransactionRecordID=" + transactionRecordID.ToString()
                      + ";CCTracks=" + tracks
                      + ";CCTransactionStatus=" + status
                      + ";CreditCallAuthCode=" + authCode
                      + ";CreditCallCardScheme=" + cardType
                      + ";CreditCallPAN=" + obscuredPan
                      + ")");
                var adapter = new DataSet1TableAdapters.QueriesTableAdapter();
                adapter.UPD_LIVE_TRANSACTIONRECORD(transactionRecordID, tracks, statusString, authCode, cardType, obscuredPan, batchNum, ttid, status);
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
            var adapter = new Database.DataSet1TableAdapters.QueriesTableAdapter();
            adapter.UPD_TRANSACTIONRECORD_STATUS(transactionRecordID, newStatusStr, (short)newStatus, (short)oldStatus);
        }

        public virtual CCProcessorInfo GetRtccProcessorInfo(string terminalSerialNumber)
        {
            var adapter = new DataSet1TableAdapters.SEL_RTCC_PROCESSORTableAdapter();
            var data = adapter.GetData(terminalSerialNumber);

            // There should be one row returned
            if (data.Rows.Count <= 0)
                throw new Exception("Error getting processor information from database: No data returned");

            // We get the data out of the first row
            return new CCProcessorInfo(
                decimal.ToInt32(data[0].TerminalID),
                data[0].TerminalSerNo,
                data[0].CompanyName,
                data[0].CCTerminalID,
                data[0].CCTransactionKey,
                data[0].CCClearingPlatform,
                readVal(() => data[0].PoleSerNo, ""),
                readVal(() => decimal.ToInt32(data[0].PoleID), 0),
                data[0].TimeZoneOffset,
                data[0].DST_Adjust,
                "",//data[0].PhoneNumber,
                "");//data[0].IP);
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
            var adapter = new Database.DataSet1TableAdapters.QueriesTableAdapter();
            adapter.UPD_TRANSACTIONAUTHORIZATION(
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
