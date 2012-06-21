using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuthorizationClientPlatforms
{
    /*
    [Obsolete("Credit Call is no longer maintained - everything goes through Monetra")]
    public class CreditCall : IAuthorizationPlatform
    {
        private AuthorizationStatistics statistics = new AuthorizationStatistics();

        public AuthorizationResponseFields Authorize(AuthorizationRequest request, bool preAuth)
        {
            if (preAuth)
                throw new NotSupportedException();
            try
            {
                CardEaseXML.Request cardEaseRequest = new CardEaseXML.Request()
                {
                    Amount = request.AmountDolars.ToString(),
                    MachineReference = request.MeterSerialNumber,
                    UserReference = request.CustomerReference,
                    TerminalID = request.MerchantID,          // CCTerminalID as associated by CreditCall
                    TransactionKey = request.MerchantPassword  // CCTransactionKey as issued by CreditCall
                };

                CardEaseXML.Client cardEaseClient = new CardEaseXML.Client();
                cardEaseClient.Request = cardEaseRequest;

                cardEaseRequest.SoftwareName = softwareName;       // String 50
                cardEaseRequest.SoftwareVersion = softwareVersion;    // string 20

                cardEaseRequest.RequestType = CardEaseXML.RequestType.Auth;
                cardEaseRequest.AutoConfirm = true;
                cardEaseRequest.AmountUnit = CardEaseXML.AmountUnit.Major;
                cardEaseRequest.Track2 = request.TrackTwoData;

                switch (authorizationMode)
                {
                    case AuthorizationMode.Test:
                        cardEaseClient.AddServerURL("https://test.cardeasexml.com/generic.cex", 45000);
                        cardEaseRequest.TerminalID = "99970110";
                        cardEaseRequest.TransactionKey = "pvOnp0Dln7vJSIR0";
                        break;

                    case AuthorizationMode.Live:
                        cardEaseClient.AddServerURL("https://live.cardeasexml.com/generic.cex", 45000);
                        break;

                    default:
                        throw new ArgumentException("Unknown authorization mode");
                }

                cardEaseClient.ProcessRequest();
                CardEaseXML.Response cardEaseResponse = cardEaseClient.Response;

                AuthorizationResultCode resultCode = AuthorizationResultCode.UnknownError;

                switch (cardEaseResponse.ResultCode)
                {
                    case CardEaseXML.ResultCode.Approved:
                        resultCode = AuthorizationResultCode.Approved;
                        break;

                    case CardEaseXML.ResultCode.Declined:
                        resultCode = AuthorizationResultCode.Declined;
                        break;

                    case CardEaseXML.ResultCode.None:
                        resultCode = AuthorizationResultCode.UnknownError;
                        break;

                    case CardEaseXML.ResultCode.TestOK:
                        resultCode = AuthorizationResultCode.Approved;
                        break;

                    default:
                        resultCode = AuthorizationResultCode.UnknownError;
                        break;
                }

                return new AuthorizationResponseFields(
                    resultCode,
                    cardEaseResponse.AuthCode,
                    cardEaseResponse.CardScheme,
                    cardEaseResponse.CardEaseReference,
                    "Reference: " + cardEaseResponse.CardEaseReference, ""
                    );
            }
            // Handle Exceptions
            catch (CardEaseXML.CardEaseXMLRequestException exception)
            {
                return new AuthorizationResponseFields(
                    AuthorizationResultCode.UnknownError, "", "", "",
                    "CardEaseXMLRequestException: " + exception.Message + "; ID-String: " + request.IDString, "");
            }
            catch (CardEaseXML.CardEaseXMLCommunicationException exception)
            {
                return new AuthorizationResponseFields(
                    AuthorizationResultCode.UnknownError, "", "", "",
                    "CardEaseXMLCommunicationException: " + exception.Message + "; ID-String: " + request.IDString, "");
            }
            catch (CardEaseXML.CardEaseXMLResponseException exception)
            {
                return new AuthorizationResponseFields(
                    AuthorizationResultCode.UnknownError, "", "", "",
                    "CardEaseXMLResponseException: " + exception.Message + "; ID-String: " + request.IDString, "");
            }
            catch (CardEaseXML.CardEaseXMLException exception)
            {
                return new AuthorizationResponseFields(
                    AuthorizationResultCode.UnknownError, "", "", "",
                    "CardEaseXMLResponseException: " + exception.Message + "; ID-String: " + request.IDString, "");
            }
            catch (Exception exception) // General exception handler
            {
                // Trap/Handle known System Exceptions, else block CCTransactionRecord
                if (exception.Message.IndexOf("Bad Request") != -1)// String found in e.Message
                {
                    return new AuthorizationResponseFields(
                    AuthorizationResultCode.UnknownError, "", "", "",
                    "CardEase: Bad Request: " + exception.Message + "; ID-String: " + request.IDString, "");
                }

                else if (exception.Message.IndexOf("Unable to connect to the remote server") != -1)// String found in e.Message
                {
                    return new AuthorizationResponseFields(
                        AuthorizationResultCode.ConnectionError, "", "", "",
                        "CardEase: Connection Error: " + exception.Message + "; ID-String: " + request.IDString, "");
                }
                else
                {
                    return new AuthorizationResponseFields(
                        AuthorizationResultCode.UnknownError, "", "", "",
                        "CardEase: General Exception: " + exception.Message + "; ID-String: " + request.IDString, "");

                }
            }   
        }
        public enum AuthorizationMode { Live, Test }

        private AuthorizationMode authorizationMode;
        private string softwareName;
        private string softwareVersion;

        public CreditCall(AuthorizationMode authorizationMode, string softwareName, string softwareVersion)
        {
            this.authorizationMode = authorizationMode;
            this.softwareName = softwareName;
            this.softwareVersion = softwareVersion;
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
