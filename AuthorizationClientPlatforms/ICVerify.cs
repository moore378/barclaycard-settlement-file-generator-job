using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AuthorizationClientPlatforms
{
    /*
    /// <summary>
    /// This concrete class performs authorization using the IC Verify server
    /// </summary>
    [Obsolete("ICVerify is no longer maintained - everything goes through Monetra")]
    public class IcvAuthorizer : IAuthorizationPlatform
    {
        private Action<string> log;
        string ICV_HostName;
        string ICV_ServerSocket;
        AuthorizationStatistics statistics = new AuthorizationStatistics();

        ThreadLocal<AUTHCTLLib.AuthCtl> authorizationControl;

        public IcvAuthorizer(string ICV_HostName, string ICV_ServerSocket, Action<string> aLog)
        {
            log = aLog;
            this.ICV_HostName = ICV_HostName;
            this.ICV_ServerSocket = ICV_ServerSocket;
            // Create a new authorization control, which will be different for every thread
            authorizationControl = new ThreadLocal<AUTHCTLLib.AuthCtl>(
                () =>
                {
                    AUTHCTLLib.AuthCtl result = new AUTHCTLLib.AuthCtl();
                    result.KeepSocketOpen = true;
                    result.Protocol = "SOCKETS";
                    result.SocketTimeoutValue = "60";

                    result.HostName = ICV_HostName;
                    result.Port = ICV_ServerSocket;

                    return result;
                }
            );
        }

        public AuthorizationResponseFields Authorize(
            AuthorizationRequest request, bool preAuth)
        {
            if (preAuth)
                throw new NotSupportedException();

            // Retrieve the authorization control specific to this thread
            AUTHCTLLib.AuthCtl axAuthCtl1 = authorizationControl.Value;
            axAuthCtl1.Merchant = "ICV20001";
            axAuthCtl1.Desc1 = request.TransactionDescription;
            axAuthCtl1.Invoice = request.Invoice;
            axAuthCtl1.Account = request.Pan;
            axAuthCtl1.Amount = request.AmountDolars.ToString();
            axAuthCtl1.ExpDate = request.ExpiryDateMMYY;

            log("Host name " + axAuthCtl1.HostName);
            log("Port " + axAuthCtl1.Port);
            log("Merchant " + axAuthCtl1.Merchant);
            log("Account " + axAuthCtl1.Account);
            log("Amount " + axAuthCtl1.Amount);
            log("ExpDate " + axAuthCtl1.ExpDate);

            log("Authorizing...");
            axAuthCtl1.Authorize();

            if (!axAuthCtl1.ErrMsg.Equals(""))
            {
                log(DateTime.Now + "ICVEE " + axAuthCtl1.ErrMsg + Environment.NewLine);
                return new AuthorizationResponseFields(
                    AuthorizationResultCode.UnknownError,
                    axAuthCtl1.ErrMsg,
                    "",
                    "",
                    axAuthCtl1.ErrMsg,
                    "");
            }
            else
            {
                string recipientRef = axAuthCtl1.RespMMDD + " " +
                                axAuthCtl1.RespHHMM + " " +
                                axAuthCtl1.Seqn;

                AuthorizationResultCode resultCode = AuthorizationResultCode.UnknownError;

                switch (axAuthCtl1.RespText){
                    case "Approved":
                        log("ICVEE responded '" + axAuthCtl1.RespText + '"');
                        // The default response
                        resultCode = AuthorizationResultCode.Approved;
                        break;
                    case "Declined":
                        log("ICVEE responded '" + axAuthCtl1.RespText + '"');
                        resultCode = AuthorizationResultCode.Declined;
                        break;
                    default:
                        log(DateTime.Now + "ICVEE Unknown response " + axAuthCtl1.RespText + Environment.NewLine);
                        // The default response
                        resultCode = AuthorizationResultCode.UnknownError;
                        break;
                }
                
                return new AuthorizationResponseFields(
                    resultCode,
                    axAuthCtl1.RespAuthCode,
                    axAuthCtl1.RespCardName,
                    axAuthCtl1.RespMMDD + " " + axAuthCtl1.RespHHMM + " " + axAuthCtl1.Seqn,
                    axAuthCtl1.RespText,
                    "");
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
