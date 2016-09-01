using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorizationClientPlatforms
{
    /// <summary>
    /// This concrete class performs authorization using the Monetra server
    /// </summary>
    public unsafe class Monetra: IAuthorizationPlatform
    {
        private Action<string> log;
        private MonetraClient monetra;
        private static Semaphore throttle = new Semaphore(50, 50);
        private AuthorizationStatistics statistics = new AuthorizationStatistics();
        private Random random = new Random();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="monetraClient">Client interface with which the authorizer will communicate with monetra.</param>
        /// <param name="aLog"></param>
        public Monetra(MonetraClient monetraClient, Action<string> aLog)
        {
            log = aLog;

            monetra = monetraClient;
        }

        /// <summary>
        /// This will try the given operation a few times at 10ms intervals until a run doesn't throw an exception or maxTries is reached. 
        /// </summary>
        /// <param name="toTry">Operation to try</param>
        /// <param name="maxTries">Max number of times to try</param>
        /// <returns>If the operation was run, then the return is the number of tries to get it to run (first time = 1). If it fails, the return value is 0.</returns>
        public static int TryAFewTimes(Action toTry, int maxTries)
        {
            for (int i = 1; i <= maxTries; i++)
            {
                try
                {
                    toTry();
                    return i;
                }
                catch
                {
                    Thread.Sleep(10);    
                }
            }
            return 0;
        }

        private static string timeWithMiliseconds()
        {
            return DateTime.Now.ToString("HH:mm:ss.ffffff");
        }

        /// <summary>
        /// Perform authorization according to Authorizer.authorize() 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authCode">
        /// AUTH: transaction authorized;
        /// CALL: call processor for authorization;
        /// DENY: transaction denied;
        /// DUPL: duplicate transaction;
        /// PKUP: confiscate card;
        /// RETRY: retry transaction;
        /// SETUP: setup error;
        /// TIMEOUT: transaction not processed in allocated amount of time;
        /// </param>
        /// <param name="cardType"></param>
        /// <returns></returns>
        public AuthorizationResponseFields Authorize(AuthorizationRequest request, AuthorizeMode mode)
        {
            bool sentAuthorizationRequest = false;
            throttle.WaitOne();
            try
            {
                MonetraClient.Transaction transaction;

                if (! monetra.connected)
                    throw new AuthorizerProcessingException("Not connected to Monetra server", true);

                // Create a new transaction
                transaction = monetra.newTransaction();

                try
                {
                    // Assign transaction fields
                    transaction.username = request.MerchantID;
                    transaction.password = request.MerchantPassword;
                    transaction.nsf = "no";

                    if (request.MerchantID.Trim() == "ipspbp")
                    {
                        transaction.rfid = "yes"; //alex 04/01/2016
                        log("RFID set");
                    }

                    switch (mode)
                    {
                        case AuthorizeMode.Normal: transaction.action = "sale"; log("Monetra: Sale"); break;
                        case AuthorizeMode.Preauth: transaction.action = "preauth"; log("Monetra: Preauth"); break;
                        case AuthorizeMode.Finalize: transaction.action = "PreauthComplete"; log("Monetra: PreauthComplete"); break;
                        default: throw new InvalidOperationException("Unknown supported action \"" + mode + "\""); 
                    }

                    if (mode == AuthorizeMode.Finalize)
                    {
                        if (request.PreauthTtid == null)
                            throw new AuthorizerProcessingException("Preauth TTID for finalization is null", false);
                        transaction.ttid = request.PreauthTtid.ToString();
                    }

                    //if (preAuth)
                    //    transaction.capture = "no";
                    transaction.custref = request.CustomerReference;
                    transaction.stationid = request.MeterSerialNumber;
                    if (mode == AuthorizeMode.Preauth)
                        transaction.amount = Math.Max(request.AmountDollars, 1.01m).ToString();
                    else
                        transaction.amount = request.AmountDollars.ToString();
                    transaction.ordernum = request.OrderNumber;

                    if (mode != AuthorizeMode.Finalize)
                    {
                        if (request.TrackTwoData != "")
                        {
                            transaction.trackdata = request.TrackTwoData;
                           // log("T2:>>>" + request.TrackTwoData); //alex 02252016 PCI VIOLATION 
                        }
                        else
                        {
                            transaction.account = request.Pan;
                            transaction.expdate = request.ExpiryDateMMYY;
                        }
                    }

                    //Thread.Sleep(400);

                    log("Verifying server connection"  + "(" + timeWithMiliseconds() + ")");
                    // Send the transaction
                    sentAuthorizationRequest = true;
                    if (!monetra.checkConnection())
                        throw new AuthorizerProcessingException("Server connection error for transaction " + request.IDString + "(" + timeWithMiliseconds() + ")", true);

                    log("Sending transaction " + request.IDString + "(" + timeWithMiliseconds() + ")");

                    MonetraClient.Transaction.Response response = transaction.send();
                    
                    if (response.ResultOfSend == false)
                    {
                        log("Failed to send transaction " + request.IDString);
                        monetra.NotConnected();
                        statistics.TotalConnectionErrors++;
                        throw new AuthorizerProcessingException("Failed to send transaction " + request.IDString + "(" + timeWithMiliseconds() + ")", false);
                    }

                    // This is a fix for a Monetra bug
                    if (response.Code == null)
                    {
                        log("Response code null. Waiting for non-null value.");
                        int count = 0;
                        while ((response.Code == null) && (count++ < 100))
                            Thread.Sleep(100);
                    }

                    int ttid;
                    if (!Int32.TryParse(response.TTID, out ttid))
                        ttid = 0;

                    short batch;
                    if (!Int16.TryParse(response.Batch, out batch))
                        batch = 0;


                    switch (response.Code.ToUpper())
                    {
                        // Approved
                        case "AUTH":
                            statistics.TotalApproved++;
                            return new AuthorizationResponseFields(
                                    AuthorizationResultCode.Approved,
                                    response.Auth,
                                    response.CardType,
                                    request.IDString,
                                    "Code=" + response.Code + ", PHardCode=" + response.PHardCode + ", MSoftCode=" + response.MSoftCode + ", Verbiage=" + response.Verbiage,
                                    ttid,
                                    batch
                                    );
                        // Declined
                        case "CALL":
                        case "DENY":
                        case "PKUP":
                            string note = "Code=" + response.Code + ", PHardCode=" + response.PHardCode + ", MSoftCode=" + response.MSoftCode + ", Verbiage=" + response.Verbiage;
                            statistics.TotalDeclined++;
                            log("Transaction declined " + note);
                            return new AuthorizationResponseFields(
                                    AuthorizationResultCode.Declined,
                                    response.Auth,
                                    response.CardType,
                                    request.IDString,
                                    note,
                                    ttid,
                                    batch);

                        // Error - but where we know its safe to retry
                        case "RETRY":
                            statistics.TotalErrors++;
                            throw new AuthorizerProcessingException("Monetra server says retry transaction, but aborting. " + " TransID=" + request.IDString + ", Code=" + response.Code + ", PHardCode=" + response.PHardCode + ", MSoftCode=" + response.MSoftCode + ", Verbiage=" + response.Verbiage, false);

                        // Other errors
                        default:
                            statistics.TotalErrors++;
                            log("Error processing transaction");
                            return new AuthorizationResponseFields(
                                    AuthorizationResultCode.UnknownError,
                                    response.Auth,
                                    response.CardType,
                                    request.IDString,
                                    response.AllResponseKeys("\t"),
                                    ttid,
                                    batch);
                    }
                }
                finally
                {
                    int tryCount = TryAFewTimes(() => transaction.Delete(), 10);
                    if (tryCount == 0)
                        log("Error, could not delete transaction even after 10 tries.");
                    else if (tryCount > 1)
                        log("Error, deleting transaction only worked on try " + tryCount.ToString());

                    statistics.TotalProcessed++;
                }
            }
            catch (Exception exception)
            {
                if (exception is AuthorizerProcessingException)
                    throw;
                else
                    throw new AuthorizerProcessingException("Unexpected Monetra authorization error", exception, !sentAuthorizationRequest);
            }
            finally
            {
                throttle.Release();
            }
        }

        public IAuthorizationStatistics Statistics
        {
            get { return statistics; }
        }
    }

    /// <summary>
    /// IMonetra is an abstraction of the monetra DLL, particularly aimed at allowing
    /// independent unit testing.
    /// </summary>
    public abstract class MonetraClient
    {
        public abstract bool connected { get; }
        public abstract string connectionErrorStr { get; }
        public abstract Transaction newTransaction();
        public abstract void Disconnect();
        public abstract void NotConnected();
        /// <summary>
        /// Checks if the connection is still active
        /// </summary>
        /// <returns>False if disconnected. True if there is no reason to think the connection might be disconnected.</returns>
        public abstract bool checkConnection();

        public abstract class Transaction
        {
            /// <summary>
            /// General user (or subuser of a general user) username. Must NOT be 'MADMIN'
            /// </summary>
            public abstract string username { set; }
            /// <summary>
            /// Password associated with user/subuser
            /// </summary>
            public abstract string password { set; }
            /// <summary>
            /// Boolean. If true, will attempt to perform a partial authorization if insufficient funds remain on card
            /// </summary>
            public abstract string nsf { set; }
            /// <summary>
            /// appropriate 'Key Value' for General User Request.
            /// </summary>
            public abstract string action { set; }
            /// <summary>
            /// Alpha-numeric.
            /// This is a non-indexed field, so this cannot be queried, but will be sent on to the processing institution for Level II interchange qualification. 
            /// This can be used to specify a customer PO number or other customer reference number.
            /// </summary>
            public abstract string custref { set; }
            /// <summary>
            /// 25-character user-defined reporting field
            /// </summary>
            public abstract string stationid { set; }
            /// <summary>
            /// If account is not present, this must be present. May be track1, track2, or a combined track1/track2 read. Typically recommended to pass the data to Monetra exactly as received (track1/track2 combined). Should not be specified if referencing a ttid. Monetra strictly validates the formatting of trackdata. If sending a combined track1/track2, you must use standardized framing characters. Track 1 must begin with % and end with ? Track 2 must begin with ; and end with ? Track 1 must always begin with a capital B (after the start sentinel if provided). The framing characters are optional if only sending track1 or track2.
            /// </summary>
            public abstract string trackdata { set; }
            /// <summary>
            /// If trackdata not present, the account number must be present. Should not be specified if referencing a ttid.
            /// </summary>
            public abstract string account { set; }
            /// <summary>
            /// Amount of transaction. All amounts are positive. This should be an aggregate amount (e.g. already includes tax and examount)
            /// </summary>
            public abstract string amount { set; }
            /// <summary>
            /// If trackdata not present, and this is not a gift card, this field must be present . Should not be specified if referencing a ttid.
            /// </summary>
            public abstract string expdate { set; }
            /// <summary>
            /// If capture is set to 'no', the transaction will not be added to the batch settlement.  This value defaults to 'yes'
            /// </summary>
            public abstract string capture { set; }
            /// <summary>
            /// transaction id guaranteed to be unique across all transactions for a particular merchant
            /// </summary>
            public abstract string ttid { set; }
            /// <summary>
            /// Alpha-numeric order number
            /// </summary>
            public abstract string ordernum { set; }
            /// <summary>
            /// rfid flag to indicate contactless //alex 04/01/2016
            /// </summary>
            public abstract string rfid { set; }

            /// <summary>
            /// Finalizes a transaction and sends it to the Monetra server
            /// </summary>
            public abstract Response send();

            /// <summary>
            /// Removes a transaction from the queue that was initialized with M_TransNew
            /// </summary>
            public abstract void Delete();

            public abstract class Response
            {
                public enum Status { M_ERROR = -1, M_FAIL = 0, M_SUCCESS = 1 };
                public abstract bool ResultOfSend { get; }

                /// <summary>
                /// Returns a success/fail response for every transaction. If a detailed code is needed, please see M_ReturnCode.
                /// </summary>
                public abstract Status ReturnStatus { get; }

                /// <summary>
                /// AUTH: transaction authorized
                /// CALL: call processor for authorization
                /// DENY: transaction denied
                /// DUPL: duplicate transaction
                /// PKUP: confiscate card
                /// RETRY: retry transaction
                /// SETUP: setup error
                /// TIMEOUT: transaction not processed in allocated amount of time
                /// </summary>
                public abstract string Code { get; }

                public abstract string PHardCode { get; }

                public abstract string MSoftCode { get; }

                public abstract string Verbiage { get; }

                public abstract string CardType { get; }

                public abstract string TTID { get; }

                /// <summary>
                /// authorization number, typically 6 digits numericonly, but some processors in test mode will return alpha-numeric responses 
                /// </summary>
                public abstract string Auth { get; }

                /// <summary>
                /// batch number to which transaction was assigned
                /// </summary>
                public abstract string Batch { get; }

                public abstract string AllResponseKeys(string delimeter);
            }
        }
    }

    // Removed MonetraDLLClient_ since it's been replaced by MonetraDotNetNativeClient.

    /// <summary>
    /// Implements MonetraClient using the native .NET client provided by Monetra
    /// </summary>
    public class MonetraDotNetNativeClient : MonetraClient
    {
        protected libmonetra.Monetra monetra;
        private bool _connected;
        private Action<string> log;

        public override bool connected
        {
            get { return _connected; }
        }

        public override string connectionErrorStr
        {
            get 
            {
                try
                {
                    return monetra.ConnectionError();
                }
                catch (NullReferenceException)
                {
                    return "Unknown error";
                }
            }
        }

        public override MonetraClient.Transaction newTransaction()
        {
            int newTransactionID = monetra.TransNew();
            return new TransactionWrapper(newTransactionID, monetra, this);
        }

        private static string timeWithMiliseconds()
        {
            return DateTime.Now.ToString("HH:mm:ss.ffffff");
        }

        ~MonetraDotNetNativeClient()
        {
            Disconnect();
        }

        public override void Disconnect()
        {
            if (_connected)
            {
                monetra.DestroyConn();
                _connected = false;
            }
        }

        void MCheck(bool functionResult)
        {
            if (functionResult != true)
                throw new Exception("Monetra operation failed");
        }

        public MonetraDotNetNativeClient(string hostName, ushort serverSocket, Action<string> log)
        {
            this.log = log;

            log("Monetra client connecting to: " + hostName + ":" + serverSocket.ToString() );
            // Set up the monetra
            monetra = new libmonetra.Monetra();
            MCheck(monetra.SetBlocking(true));

            log("Attempting to use SSL");
            
            MCheck(monetra.SetSSL(hostName, serverSocket));
            MCheck(monetra.VerifySSLCert(false));
                        
            if (monetra.Connect() == true)
            {
                log("Connected to Monetra");                    
                _connected = true;
                monetra.SetTimeout(30);
            }
            else
            {
                log("Failed to connect to monetra server using SSL: " + monetra.ConnectionError());
                _connected = false;

                log("Trying without SSL");
                // Try without SSL
                MCheck(monetra.SetIP(hostName, serverSocket));

                if (monetra.Connect() == true)
                {
                    log("Connected to Monetra");                    
                    _connected = true;
                    monetra.SetTimeout(30);
                }
                else
                {
                    log("Failed to connect to monetra server: " + monetra.ConnectionError());
                    _connected = false;

                    throw new AuthorizerConnectException("Failed to connect to monetra: " + monetra.ConnectionError());
                }
            }

        }

        /// <summary>
        /// Implements MonetraClient.Transaction using libmonetra.M_TRAN
        /// </summary>
        protected class TransactionWrapper : MonetraClient.Transaction
        {
            public int transactionID;
            private libmonetra.Monetra monetra;
            private MonetraClient monetraClient;

            public TransactionWrapper(int transactionID, libmonetra.Monetra monetra, MonetraClient monetraClient)
            {
                this.transactionID = transactionID;
                this.monetra = monetra;
                this.monetraClient = monetraClient;
            }

            public override string username
            {
                set { monetra.TransKeyVal(transactionID, "username", value.Trim(new char[] { ' ', '\0' })); }
            }

            public override string password
            {
                set { monetra.TransKeyVal(transactionID, "password", value.Trim(new char[] { ' ', '\0' })); }
            }

            public override string action
            {
                set { monetra.TransKeyVal(transactionID, "action", value); }
            }

            public override string custref
            {
                set { monetra.TransKeyVal(transactionID, "custref", value); }
            }

            public override string stationid
            {
                set { monetra.TransKeyVal(transactionID, "stationid", value); }
            }

            public override string trackdata
            {
                set { monetra.TransKeyVal(transactionID, "trackdata", value.Trim(new char[]{' ','\0'})); }
            }

            public override string account
            {
                set { monetra.TransKeyVal(transactionID, "account", value); }
            }

            public override string amount
            {
                set { monetra.TransKeyVal(transactionID, "amount", value); }
            }

            public override string expdate
            {
                set { monetra.TransKeyVal(transactionID, "expdate", value); }
            }

            public override string capture
            {
                set { monetra.TransKeyVal(transactionID, "capture", value); }
            }


            public override Transaction.Response send()
            {
                bool resultOfSend = monetra.TransSend(transactionID);
                return new TransactionResponse(monetra, transactionID, resultOfSend);
            }

            public override void Delete()
            {
                monetra.DeleteTrans(transactionID);
            }

            private class TransactionResponse : Transaction.Response
            {
                bool resultOfSend;
                libmonetra.Monetra monetra;
                int transactionID;

                public TransactionResponse(libmonetra.Monetra monetra, int transactionID, bool resultOfSend)
                {
                    this.resultOfSend = resultOfSend;
                    this.monetra = monetra;
                    this.transactionID = transactionID;
                }

                public override bool ResultOfSend
                {
                    get { return resultOfSend; }
                }

                public override Response.Status ReturnStatus
                {
                    get
                    {
                        switch (monetra.ReturnStatus(transactionID))
                        {
                            case libmonetra.Monetra.M_SUCCESS:
                                return Transaction.Response.Status.M_SUCCESS;
                            case libmonetra.Monetra.M_FAIL:
                                return Transaction.Response.Status.M_FAIL;
                            default /* Error */:
                                return Transaction.Response.Status.M_ERROR;
                        }
                    }
                }

                public override string Code
                {
                    get { return monetra.ResponseParam(transactionID, "code"); }
                }

                public override string PHardCode
                {
                    get { return monetra.ResponseParam(transactionID, "phard_code"); }
                }

                public override string CardType
                {
                    get { return monetra.ResponseParam(transactionID, "cardtype"); }
                }

                public override string Auth
                {
                    get { return monetra.ResponseParam(transactionID, "auth"); }
                }

                public override string Batch
                {
                    get { return monetra.ResponseParam(transactionID, "batch"); }
                }

                public override string TTID
                {
                    get { return monetra.ResponseParam(transactionID, "ttid"); }
                }

                public override string AllResponseKeys(string delimeter)
                {
                    string result = "";
                    string[] keys = monetra.ResponseKeys(transactionID);
                    for (int i = 0; i < keys.Length; i++)
                    {
                        string key = keys[i];
                        result += key + "=" + monetra.ResponseParam(transactionID, key) + delimeter;
                    }
                    return result;
                }

                public override string MSoftCode
                {
                    get { return monetra.ResponseParam(transactionID, "msoft_code"); }
                }

                public override string Verbiage
                {
                    get { return monetra.ResponseParam(transactionID, "verbiage"); }
                }
            }

            public override string ttid
            {
                set { monetra.TransKeyVal(transactionID, "ttid", value); }
            }

            public override string ordernum
            {
                set { monetra.TransKeyVal(transactionID, "ordernum", value); }
            }

            public override string nsf
            {
                set { monetra.TransKeyVal(transactionID, "nsf", value); }
            }


            //.alex 04/01/2016
            public override string rfid
            {
                set { monetra.TransKeyVal(transactionID, "rfid", value); }
            }

        }

        public override void NotConnected()
        {
            Disconnect();
        }

        public override bool checkConnection()
        {
            return monetra.Monitor();
        }
    }
}
