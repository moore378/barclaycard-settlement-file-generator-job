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
        // 2016-08-26 ES-79 For this hotfix, utilize a different table 
        // adapter to retrieve both the transaction record ID and
        // duplicate flag. NOTE: Newer mainline code utilizes AutoDatabase
        // so this cannot be merged over as-is.
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
            decimal transactionRecordId = 0; // ID as returned by the database from the insertion.

            DataSet1.INS_LIVE_TRANSACTIONRECORDRow row; // Store the result row returned back.

            //System.Threading.Thread.Sleep(40000); // Dev-level testing to verify ES-79.

            var adapter = new DataSet1TableAdapters.INS_LIVE_TRANSACTIONRECORDTableAdapter();

            var result = adapter.GetData(
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
                status,
                null, null, null, null, null, null);

            try
            {
                // Get the only row returned back by the stored procedure.
                // Should be one row in the normal case of insertion
                // and also in the duplicate record case.
                row = (DataSet1.INS_LIVE_TRANSACTIONRECORDRow)result.Rows[0];

                // If this is a duplicate transaction then stop processing.
                // Somehow Session Manager added the transaction as an offline
                // transaction and the transaction needs to be processed by
                // CCTM instead.
                if (1 == row.Dup)
                {
                    throw new Exception(String.Format("RTCC encounter duplicate transaction error for meter {0} and transaction record ID of {1}", TerminalSerNo, row.TransactionRecordID));
                }
                else
                {
                    // Return back the transaction record ID.
                    transactionRecordId = row.TransactionRecordID;
                }
            }
            catch (Exception e)
            {
                LogError("Database error in inserting the transaction record. \n" + e.ToString(), e);
                throw;
            }

            // Be consistent with throwing an error when not getting the transaction record ID.
            if (0 == transactionRecordId)
                throw new Exception("Error calling SEL_TRANSRECID_FROM_UNIQUEREC: returned null");
            else
                return transactionRecordId;
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
                var adapter = new DataSet1TableAdapters.QueriesTableAdapter();
                adapter.UPD_LIVE_TRANSACTIONRECORD(transactionRecordID, tracks, statusString, authCode, cardType, obscuredPan, batchNum, ttid, status, (int) ccFee);
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
            CCProcessorInfo info;

            var adapter = new DataSet1TableAdapters.SEL_RTCC_PROCESSORTableAdapter();
            var data = adapter.GetData(terminalSerialNumber);

            // There should be one row returned
            if (data.Rows.Count <= 0)
                throw new Exception("Error getting processor information from database: No data returned");

            // We get the data out of the first row
            info = new CCProcessorInfo(
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
                "",
                data[0].CCFee);//data[0].IP);

            // Normalize the extra processor settings.
            // TODO: Dynamically set these up via configuration.
            switch (info.ClearingPlatform.ToLower())
            {
                case "israel-premium":
                    info.ProcessorSettings["MerchantNumber"] = data[0].MerchantNumber;
                    info.ProcessorSettings["CashierNumber"] = data[0].CashierNumber;
                    break;
                case "fis-paydirect":
                    info.ProcessorSettings["SettleMerchantCode"] = data[0].MerchantNumber;
                    break;
                case "monetra":
                default:
                    break;
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
