using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuthorizationClientPlatforms
{
    public class IsraelPremium : IAuthorizationPlatform
    {
        IsraelProcessor.IsraelProcessorServiceClient client;

        public IsraelPremium()
        {
            client = new IsraelProcessor.IsraelProcessorServiceClient();
        }
                   
            
        public AuthorizationResponseFields Authorize(AuthorizationRequest request, AuthorizeMode mode)
        {
            string transactionRecordResult;
            string resultRecord;

            string premiumUserName = request.MerchantID.Trim();
            string premiumPassword = request.MerchantPassword;
            string premiumCashierNum = "01";
            string merchantNumber = "0963185013";
            string transactionDate_yyyyMMdd = request.StartDateTime.ToString("yyyyMMdd");
            string transactionTime_HHmm = request.StartDateTime.ToString("HHmm");
            string track2 = "";// request.TrackTwoData;
            string cardNum = request.Pan;
            string expDate_YYMM = request.ExpiryDateMMYY.Substring(2, 2) + request.ExpiryDateMMYY.Substring(0, 2);
            string amount = ((int)request.AmountDolars * 100).ToString();
            string transactionType = "01";
            string creditTerms = "1";
            string currency = "1";
            string code = "51";
            string paramJ = "4";
            string last4Digits = request.Pan.Substring(request.Pan.Length - 4, 4);

            int res = client.AuthCreditCardFull(
                out transactionRecordResult, // out TransactionRecord
                out resultRecord,  // out ResultRecord
                premiumUserName, //"user1", // premiumUserName
                premiumPassword, //"11111-22222-33333-44444-55555", // premiumPassword
                premiumCashierNum, // premiumCashierNum
                merchantNumber, // merchantNumber
                transactionDate_yyyyMMdd, // transactionDate_yyyyMMdd
                transactionTime_HHmm, // transactionTime_HHmm
                "", // uniqueTransactionNumber_SixDigits
                track2, // track2
                cardNum, // cardNum
                expDate_YYMM, // expDate_YYMM
                amount, // amount
                "", // cochavAmount
                transactionType, // transactionType
                creditTerms, // creditTerms
                currency, // currency
                "", // authNum
                code, // code
                "", // firstAmount
                "", // nonFirstAmount
                "", // numOfPayment
                "", // sapakMutav
                "", // sapakMutavNo
                "", // uniqNum
                "", // clubCode
                paramJ, // paramJ
                "", // addData
                "", // eci
                "", // cvv2
                "", // id
                "", // cavvUcaf
                last4Digits, // last4Digits
                "", // transactionCurrency
                "" // transactionAmount
                );

            return new AuthorizationResponseFields(
                (res == 0) ? AuthorizationResultCode.Approved : AuthorizationResultCode.Declined,
                transactionRecordResult, "", "", transactionRecordResult, 0, 0);
        }

        public IAuthorizationStatistics Statistics
        {
            get { throw new NotImplementedException(); }
        }
    }
}
