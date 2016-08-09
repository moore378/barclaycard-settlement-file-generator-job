using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rtcc.Database;
using Rtcc.Main;
using TransactionManagementCommon;

namespace Rtcc.Dummy
{
    public class DummyDatabase : RtccDatabase
    {
        private string _merchantID;
        private string _merchantPassword;
        private string _clearingPlatform;

        public DummyDatabase( string clearingPlatform, string merchantID, string merchantPassword)
        {
            _clearingPlatform = clearingPlatform;
            _merchantID = merchantID;
            _merchantPassword = merchantPassword;
        }

        public DummyDatabase():this("", "DummyMerchant", "DummyPassword")
        {
            // Nothing on purpose.
        }

        public override decimal InsertLiveTransactionRecord(
            string TerminalSerNo,
            string ElectronicSerNo,
            decimal TransactionType,
            DateTime StartDateTime,
            decimal TotalCreditCents,
            decimal TimePurchased,
            decimal TotalParkingTime,
            decimal AmountCents,
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
            // Do nothing
            return 0;
        }

        public override void UpdateTransactionStatus(
            int transactionRecordID, TransactionStatus oldStatus, TransactionStatus newStatus, string newStatusStr)
        {

        }

        public override CCProcessorInfo GetRtccProcessorInfo(string terminalSerialNumber)
        {
            CCProcessorInfo data = new CCProcessorInfo()
            {
                CompanyName = "DummyCompany",
                MerchantID = _merchantID,
                MerchantPassword = _merchantPassword,
                TerminalSerialNumber = "DummyTerminal",
                PoleSerialNumber = "DummyPole",
                ClearingPlatform = _clearingPlatform
            };

            if (data.ClearingPlatform.ToLower() == "fis-paydirect")
            {
                data.ProcessorSettings["SettleMerchantCode"] = "50BNA-PUBWK-PARKG-00";
            }

            return data;
        }

        public override void UpdateLiveTransactionRecord(decimal transactionRecordID, string tracks, string statusString, string authCode, string cardType, string obscuredPan, short batchNum, int ttid, short status, decimal ccFee)
        {
            // Do nothing
        }
    }
}
