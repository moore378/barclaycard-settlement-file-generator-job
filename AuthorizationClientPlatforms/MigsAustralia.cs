using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PCCOMLib;

namespace AuthorizationClientPlatforms
{
    /*
    /// <summary>
    /// Mastercard Internet Gateway Service for Australia
    /// </summary>
    [Obsolete("MIGS Australia is no longer maintained - everything goes through Monetra")]
    public class MigsAustralia : IAuthorizationPlatform
    {
        private bool onlyForTesting = true;
        private Action<string> log;
        private AuthorizationStatistics statistics = new AuthorizationStatistics();

        public MigsAustralia(Action<string> log, bool onlyForTesting)
        {
            this.onlyForTesting = onlyForTesting;
            this.log = log;
        }

        public AuthorizationResponseFields Authorize(AuthorizationRequest request, bool preAuth)
        {
            if (preAuth)
                throw new NotSupportedException();

            try
            {
                // Check access to PCCOMLib.dll
                PaymentClientClass PCC = new PaymentClientClass();
                string result = PCC.echo("Testing PCCOMLib");
                if (result != "echo:Testing PCCOMLib")
                {
                    return new AuthorizationResponseFields(
                        AuthorizationResultCode.ConnectionError, "ConnectionErr", "", "", "Record " + request.IDString +
                        " - Access to PCCOMLib failed " + result, "");
                }
                else
                {
                    // Build Digital Order
                    PCC.addDigitalOrderField("MercTxnRef", request.IDString);
                    PCC.addDigitalOrderField("CardNum", request.Pan.ToString());
                    PCC.addDigitalOrderField("CardExp", request.ExpiryDateMMYY);

                    string MerchantId = request.MerchantPassword;  // As issued to City of Perth by MIGS

                    //ClearingPltfrm = "TEST";         // For Testing only. Comment out when done 
                    //TransactionKey = "CITOFPCOM54";  // For Testing only. Comment out when done

                    if (onlyForTesting)
                        MerchantId = "TEST" + request.MerchantPassword;

                    // Submit Digital Order
                    int MOTOResult = PCC.sendMOTODigitalOrder(request.MeterSerialNumber, MerchantId, (int)(request.AmountDolars*100), "en", "");

                    // Test for valid result
                    int nextResult = PCC.nextResult();  // test whether this command is really required...

                    // Get Digital Receipt Details
                    string receiptRef = PCC.getResultField("DigitalReceipt.ReceiptNo");
                    string qsiCode = PCC.getResultField("DigitalReceipt.QSIResponseCode");
                    string authCode = "QSI:" + qsiCode;
                    AuthorizationResultCode resultCode = AuthorizationResultCode.UnknownError;

                    switch (qsiCode)
                    {
                        case "0":
                            statistics.TotalApproved++;
                            resultCode = AuthorizationResultCode.Approved;
                            break;

                        case "2":  // Bank Declined Transaction.
                        case "4":  // Expired Card
                        case "5":  // Insufficient funds
                        case "8":  // Transaction type not supported
                        case "9":  // Bank Declined Transaction. Don not contact bank.
                        case "E":  // 3D secure Authentication failed
                        case "F":  // 3D secure Authentication failed
                            statistics.TotalDeclined++;
                            resultCode = AuthorizationResultCode.Declined;
                            break;

                        case "1":
                            statistics.TotalErrors++;
                            resultCode = AuthorizationResultCode.UnknownError;
                            authCode = "MIGS Error 1";
                            break;

                        case "3":  // No reply from bank
                            statistics.TotalErrors++;
                            resultCode = AuthorizationResultCode.UnknownError;
                            authCode = "MIGS No Bank Reply";
                            break;

                        case "6":  // Comms Error with bank
                            statistics.TotalErrors++;
                            resultCode = AuthorizationResultCode.ConnectionError;
                            authCode = "MIGS Bank Comms Err";
                            break;

                        case "7":  // Payment Server System error
                            statistics.TotalErrors++;
                            resultCode = AuthorizationResultCode.UnknownError;
                            authCode = "MIGS Server Exptn";
                            break;
                    }

                    statistics.TotalProcessed++;

                    string cardType = PCC.getResultField("DigitalReceipt.CardType");

                    return new AuthorizationResponseFields(resultCode, authCode, cardType, receiptRef, "", "");
                }
            }
            catch (Exception e)
            {
                log("Record " + request.IDString + " - PCCOMLib failed: " + e.ToString());
                return new AuthorizationResponseFields(
                        AuthorizationResultCode.UnknownError, "", "", "", "Record " + request.IDString +
                        " - PCCOMLib failed", "");
            }
        }


        public IAuthorizationStatistics Statistics
        {
            get { return statistics; }
        }


        public PreauthCompleteResponse PreauthComplete(PreauthCompleteRequest request)
        {
            throw new NotImplementedException();
        }
    }
    */ 
}
