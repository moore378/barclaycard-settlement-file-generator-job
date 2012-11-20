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
                        transaction.amount = Math.Max(request.AmountDolars, 1.01m).ToString();
                    else
                        transaction.amount = request.AmountDolars.ToString();
                    transaction.ordernum = request.OrderNumber;

                    if (mode != AuthorizeMode.Finalize)
                    {
                        if (request.TrackTwoData != "")
                            transaction.trackdata = request.TrackTwoData;
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
                            statistics.TotalDeclined++;
                            log("Transaction declined (phard_code=" + response.PHardCode + ")");
                            return new AuthorizationResponseFields(
                                    AuthorizationResultCode.Declined,
                                    response.Auth,
                                    response.CardType,
                                    request.IDString,
                                    "Code=" + response.Code + ", PHardCode=" + response.PHardCode + ", MSoftCode=" + response.MSoftCode + ", Verbiage=" + response.Verbiage,
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

    /// <summary>
    /// Implementation of the IMonetraClient interface using the monetra DLL
    /// </summary>
    [Obsolete("Use MonetraDotNetNativeClient")]
    public unsafe class MonetraDLLClient_ : MonetraClient
    {
        /// <summary>
        /// Constructor. Connects to Monetra server using given host name and port.
        /// </summary>
        /// <param name="HostName"></param>
        /// <param name="ServerSocket"></param>
        /// <param name="aLog"></param>
        public MonetraDLLClient_(string HostName, ushort ServerSocket, Action<string> aLog)
        {
            _log = aLog;
            this._hostName = HostName;
            this._serverSocket = ServerSocket;

            lock(this)
            {
                // Initialize the monetra engine
                MonetraDLL.M_InitEngine_ex(MonetraDLL.m_ssllock_style.M_SSLLOCK_INTERNAL);
                // Set up the connection
                MonetraDLL.M_InitConn(ref _connection); // M_CONN*

                // Keep references to these to stop them being garbage collected
                mutexdestroy = M_Unregister_Mutex;
                mutexinit = M_Register_Mutex;
                mutexlock = M_Mutex_Lock;
                mutexunlock = M_Mutex_Unlock;
                threadid = M_ThreadID;
                // Give monetra the multi-threaded call-backs
                /*MCheck(MonetraDLL.M_Register_mutexinit(ref _connection, mutexinit));
                MCheck(MonetraDLL.M_Register_mutexdestroy(ref _connection, mutexdestroy));
                MCheck(MonetraDLL.M_Register_mutexlock(ref _connection, mutexlock));
                MCheck(MonetraDLL.M_Register_mutexunlock(ref _connection, mutexunlock));
                MCheck(MonetraDLL.M_Register_threadid(ref _connection, threadid));
                MCheck(MonetraDLL.M_EnableThreadSafety(ref _connection));*/

                MCheck(MonetraDLL.M_SetIP(ref _connection, HostName, ServerSocket));
                //Monetra.M_SetSSL((IntPtr*)_connectionPtr.ToPointer(), HostName, ServerSocket);
                MCheck(MonetraDLL.M_SetBlocking(ref _connection, 1));
                if (MonetraDLL.M_Connect(ref _connection) != 0)
                {
                    //log("Connected to Monetra server");
                    _connected = true;
                }
                else
                {
                    _log("Failed to connect: " + MonetraDLL.M_ConnectionError(ref _connection));
                    _connected = false;
                    throw new AuthorizerConnectException("Failed to connect: " + MonetraDLL.M_ConnectionError(ref _connection));
                }
            }
        }

        ~MonetraDLLClient_()
        {
            Disconnect();
        }

        void MCheck(int functionResult)
        {
            if (functionResult != 1)
                throw new Exception("Monetra operation failed");
        }

        public override void Disconnect()
        {
            //if (_connected)
            //{
            //    _connected = false;
            //    lock (this)
            //    {
            //        MonetraDLL.M_DestroyConn(ref _connection);
            //        MonetraDLL.M_DestroyEngine();
            //    }
            //    
            //}
        }


        #region Implementation of MonetraClient superclass
        public override bool connected { get { return _connected; } }

        public override string connectionErrorStr
        {
            get
            {
                lock(this)
                {
                    return MonetraDLL.M_ConnectionError(ref _connection);
                }
            }
        }

        public override MonetraClient.Transaction newTransaction()
        {
            return new MonetraDLLTransaction(this, _log);
        }
        #endregion

        #region MonetraDLL connection-coupled calls

        /// <summary>
        /// starts a new transaction. This must be called to obtain an identifier before any transaction Parameters may be added.
        /// </summary>
        /// <returns>Reference for transaction</returns>
        private IntPtr M_TransNew()
        {
            lock(this)
            {
                return MonetraDLL.M_TransNew(ref _connection);
            }
        }

        /// <summary>
        /// Finalizes a transaction and sends it to the Monetra server
        /// </summary>
        /// <param name="transID">Reference for transaction as returned by M_TransNew()</param>
        /// <returns>1 on success, 0 on failure</returns>
        private int M_TransSend(IntPtr transID)
        {
            lock (this)
            {
                return MonetraDLL.M_TransSend(ref _connection, transID);
            }
        }

        /// <summary>
        /// Returns a success/fail response for every transaction. If a detailed code is needed, please see M_ReturnCode
        /// </summary>
        /// <param name="transID">Reference for transaction as returned by M_TransNew()</param>
        /// <returns>1 if transaction successful (authorization), 0 if transaction failed (denial)</returns>
        private int M_ReturnStatus(IntPtr transID)
        {
            lock (this)
            {
                return MonetraDLL.M_ReturnStatus(ref _connection, transID);
            }
        }

        /// <summary>
        /// This function is used to retrieve the response key/value pairs from the Monetra Engine, as specified in the Monetra Client Interface Protocol Specification.
        /// </summary>
        /// <param name="transID">Reference for transaction as returned by M_TransNew()</param>
        /// <param name="key">Response Parameter key as defined in the Monetra Client Interface Specification</param>
        /// <returns>value associated with the key requested. NULL if not found.</returns>
        private string M_ResponseParam(IntPtr transID, string key)
        {
            lock(this)
            {
                return MonetraDLL.M_ResponseParam(ref _connection, transID, key);
            }
        }

        /// <summary>
        /// Adds a key/value pair for a transaction to be sent to Monetra
        /// </summary>
        private void M_TransKeyVal(IntPtr transID, string key, string val)
        {
            lock (this)
            {
                if (MonetraDLL.M_TransKeyVal(ref _connection, transID, key, val) != 1)
                    throw new Exception("Could not set monetra key value pair");
            }
        }


        /// <summary>
        /// Removes a transaction from the queue that was initialized with M_TransNew
        /// </summary>
        private void M_DeleteTrans(IntPtr transactionID)
        {
            lock (this)
            {
                MonetraDLL.M_DeleteTrans(ref _connection, transactionID);
            }
        }

        public IntPtr M_ResponseKeys(IntPtr _transactionID, out int numKeys)
        {
            int _numKeys;
            lock (this)
            {
                IntPtr result = MonetraDLL.M_ResponseKeys(ref _connection, _transactionID, &_numKeys);
                numKeys = _numKeys;
                return result;
            }
        }

        private class MonetraDLLTransaction : MonetraClient.Transaction
        {
            /// <summary>
            /// Create a transaction object
            /// </summary>
            /// <param name="connection">Reference to the monetra connection</param>
            public MonetraDLLTransaction(MonetraDLLClient_ client, Action<string> log)
            {
                _client = client;
                this.log = log;
                _transactionID = _client.M_TransNew();
            }

            ~MonetraDLLTransaction()
            {

            }

            #region IMonetraTransaction Members

            public override string username
            {
                set { _client.M_TransKeyVal(_transactionID, "username", value); }
            }

            public override string password
            {
                set { _client.M_TransKeyVal(_transactionID, "password", value); }
            }

            public override string action
            {
                set { _client.M_TransKeyVal(_transactionID, "action", value); }
            }

            public override string custref
            {
                set { _client.M_TransKeyVal(_transactionID, "custref", value); }
            }

            public override string stationid
            {
                set { _client.M_TransKeyVal(_transactionID, "stationid", value); }
            }

            public override string trackdata
            {
                set { _client.M_TransKeyVal(_transactionID, "trackdata", value); }
            }

            public override string account
            {
                set { _client.M_TransKeyVal(_transactionID, "account", value); }
            }

            public override string amount
            {
                set { _client.M_TransKeyVal(_transactionID, "amount", value); }
            }

            public override string expdate
            {
                set { _client.M_TransKeyVal(_transactionID, "expdate", value); }
            }

            public override string capture
            {
                set { _client.M_TransKeyVal(_transactionID, "capture", value); }
            }

            public override Response send()
            {
                int resultOfSend = _client.M_TransSend(_transactionID);
                return new MonetraDLLTransResponse(this, resultOfSend, log);
            }

            public override void Delete()
            {
                _client.M_DeleteTrans(_transactionID);
            }

            #endregion

            public int M_ReturnStatus()
            {
                return _client.M_ReturnStatus(_transactionID);
            }

            public string M_ResponseParam(string key)
            {
                return _client.M_ResponseParam(_transactionID, key);
            }

            private MonetraDLLClient_ _client;
            private IntPtr _transactionID;
            private Action<string> log;

            public IntPtr M_ResponseKeys(out int numKeys)
            {
                return _client.M_ResponseKeys(_transactionID, out numKeys);
            }

            public string M_ResponseKeys_index(IntPtr keys, int num_keys, int i)
            {
                return _client.M_ResponseKeys_index(keys, num_keys, i);
            }

            public void M_FreeResponseKeys(IntPtr keys, int num_keys)
            {
                _client.M_FreeResponseKeys(keys, num_keys);
            }

            public override string ttid
            {
                set { _client.M_TransKeyVal(_transactionID, "ttid", value); }
            }

            public override string ordernum
            {
                set { _client.M_TransKeyVal(_transactionID, "ordernum", value); }
            }

            public override string nsf
            {
                set { _client.M_TransKeyVal(_transactionID, "nsf", value); }
            }
        }

        private class MonetraDLLTransResponse : MonetraClient.Transaction.Response
        {
            public MonetraDLLTransResponse(MonetraDLLTransaction transaction, int resultOfSend, Action<string> log)
            {
                this._transaction = transaction;
                this._resultOfSend = resultOfSend;
                this.log = log;
            }

            public override bool ResultOfSend
            {
                get { return this._resultOfSend!=0; }
            }

            public override Transaction.Response.Status ReturnStatus
            {
                get
                {
                    switch (_transaction.M_ReturnStatus())
                    {
                        case MonetraDLL.M_SUCCESS:
                            return Transaction.Response.Status.M_SUCCESS;
                        case MonetraDLL.M_FAIL:
                            return Transaction.Response.Status.M_FAIL;
                        default /* MonetraDLL.M_ERROR */:
                            return Transaction.Response.Status.M_ERROR;
                    }
                }
            }

            public override string Code
            {
                get { return _transaction.M_ResponseParam("code"); }
            }

            public override string CardType
            {
                get { return _transaction.M_ResponseParam("cardtype"); }
            }

            public override string PHardCode
            {
                get { return _transaction.M_ResponseParam("phard_code"); }
            }

            public override string Auth
            {
                get { return _transaction.M_ResponseParam("auth"); }
            }

            public override string AllResponseKeys(string delimeter)
            {
                int num_keys;
                string result = "";
                IntPtr keys = _transaction.M_ResponseKeys(out num_keys);
                try
                {
                    //log("All " + num_keys + " repsonse parameters:");
                    for (int i = 0; i < num_keys; i++)
                    {
                        string key = _transaction.M_ResponseKeys_index(keys, num_keys, i);
                        result += key + "=" + _transaction.M_ResponseParam(key) + delimeter;
                    }
                }
                finally
                {
                    _transaction.M_FreeResponseKeys(keys, num_keys);
                }
                return result;
            }

            private int _resultOfSend;
            private MonetraDLLTransaction _transaction;
            private Action<string> log;


            public override string MSoftCode
            {
                get { return _transaction.M_ResponseParam("msoft_code"); }
            }

            public override string Verbiage
            {
                get { return _transaction.M_ResponseParam("verbiage"); }
            }

            public override string Batch
            {
                get { return _transaction.M_ResponseParam("batch"); }
            }

            public override string TTID
            {
                get { return _transaction.M_ResponseParam("ttid"); }
            }
        }
        #endregion

        /// <summary>
        /// Access to the monetra DLL
        /// </summary>
        private unsafe class MonetraDLL
        {
            public enum m_ssllock_style
            {
                M_SSLLOCK_NONE = 0,	/* No locking method defined, should not be set by a user */
                M_SSLLOCK_EXTERNAL = 1,	/* OpenSSL's locks are guaranteed to be
				 * initialized externally, OpenSSL itself
				 * would also be initialized extrenally */
                M_SSLLOCK_INTERNAL = 2,	/* LibMonetra is responsible for all OpenSSL
				 * code.  OpenSSL should not be used outside
				 * of libmonetra calls in an application if this
				 * is used, especially in multi-threaded code */
                FORCE_Int = 2147483647
            };

            const string MonetraDllName = /*"libmonetra_Cdecl.dll";//"..\\..\\..\\..\\MonetraDummyDLL\\MonetraDummy.dll";//*/"libmonetra.dll";

            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_InitEngine(string cafile);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_InitEngine_ex(m_ssllock_style lockstyle);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern void M_DestroyEngine();
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern void M_InitConn(ref IntPtr conn);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern void M_DestroyConn(ref IntPtr conn);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_SetIP(ref IntPtr conn, string host, ushort port);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_SetSSL(ref IntPtr conn, string host, ushort port);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_SetSSL_CAfile(ref IntPtr conn, string path);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_SetSSL_Files(ref IntPtr conn, string sslkeyfile, string sslcertfile);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern void M_VerifySSLCert(ref IntPtr conn, int tf);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_SetBlocking(ref IntPtr conn, int tf);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_Connect(ref IntPtr conn);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern IntPtr M_TransNew(ref IntPtr conn);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_TransKeyVal(ref IntPtr conn, IntPtr id, string key, string val);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_TransSend(ref IntPtr conn, IntPtr id);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_Monitor(ref IntPtr conn);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_CheckStatus(ref IntPtr conn, IntPtr id);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_ReturnStatus(ref IntPtr conn, IntPtr id);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern void M_DeleteTrans(ref IntPtr conn, IntPtr id);
            [DllImport(MonetraDllName, EntryPoint = "M_ResponseParam", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern IntPtr M_ResponseParam_int(ref IntPtr conn, IntPtr id, string key);
            [DllImport(MonetraDllName, EntryPoint = "M_ConnectionError", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern IntPtr M_ConnectionError_int(ref IntPtr conn);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern IntPtr M_ResponseKeys(ref IntPtr conn, IntPtr id, int* num_keys);
            [DllImport(MonetraDllName, EntryPoint = "M_ResponseKeys_index", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern IntPtr M_ResponseKeys_index_int(IntPtr keys, int num_keys, int idx);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_FreeResponseKeys(IntPtr keys, int num_keys);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_IsCommaDelimited(ref IntPtr conn, IntPtr id);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_ParseCommaDelimited(ref IntPtr conn, IntPtr id);
            [DllImport(MonetraDllName, EntryPoint = "M_GetCell", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern IntPtr M_GetCell_int(ref IntPtr conn, IntPtr id, string column, int row);
            [DllImport(MonetraDllName, EntryPoint = "M_GetCellByNum", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern IntPtr M_GetCellByNum_int(ref IntPtr conn, IntPtr id, int column, int row);
            [DllImport(MonetraDllName, EntryPoint = "M_GetHeader", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern IntPtr M_GetHeader_int(ref IntPtr conn, IntPtr id, int column);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_NumRows(ref IntPtr conn, IntPtr id);
            [DllImport(MonetraDllName, CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_NumColumns(ref IntPtr conn, IntPtr id);

            unsafe public static string M_ResponseParam(ref IntPtr conn, IntPtr id, string key)
            {
                return (Marshal.PtrToStringAnsi(M_ResponseParam_int(ref conn, id, key)));
            }
            unsafe public static string M_ConnectionError(ref IntPtr conn)
            {
                return (Marshal.PtrToStringAnsi(M_ConnectionError_int(ref conn)));
            }
            unsafe public static string M_ResponseKeys_index(IntPtr keys, int num_keys, int idx)
            {
                return (Marshal.PtrToStringAnsi(M_ResponseKeys_index_int(keys, num_keys, idx)));
            }
            unsafe public static string M_GetCell(ref IntPtr conn, IntPtr id, string column, int row)
            {
                return (Marshal.PtrToStringAnsi(M_GetCell_int(ref conn, id, column, row)));
            }
            unsafe public static string M_GetCellByNum(ref IntPtr conn, IntPtr id, int column, int row)
            {
                return (Marshal.PtrToStringAnsi(M_GetCellByNum_int(ref conn, id, column, row)));
            }
            unsafe public static string M_GetHeader(ref IntPtr conn, IntPtr id, int column)
            {
                return (Marshal.PtrToStringAnsi(M_GetHeader_int(ref conn, id, column)));
            }

            /* M_ReturnStatus codes */
            public const int M_SUCCESS = 1;
            public const int M_FAIL = 0;
            public const int M_ERROR = -1;

            /* M_CheckStatus codes */
            public const int M_DONE = 2;
            public const int M_PENDING = 1;

            /// <summary>
            /// After registering mutex callbacks, you must enable the use of them by
            /// calling this function. This function must be called before M_Connect().
            /// Enabling thread safety is important if you have more than one thread that
            /// wishes to act upon a single connection at the same time. Most programs do
            /// not have this need, but if you do, make sure you register the callbacks, and
            /// enable this function.
            /// </summary>
            /// <param name="conn">M_CONN structure passed by reference, or an allocated M_CONN pointer</param>
            /// <returns>1 on success, 0 on failure</returns>
            [DllImport(MonetraDllName, EntryPoint = "M_EnableThreadSafety", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_EnableThreadSafety(ref IntPtr conn);

            /// <summary>
            /// Register a callback to destroy the mutex
            /// </summary>
            /// <param name="conn">M_CONN structure passed by reference, or an allocated M_CONN pointer</param>
            /// <param name="reg">Pointer to M_Unregister_Mutex function to call when doing a mutex destroy</param>
            /// <returns>1 on success, 0 on failure</returns>
            [DllImport(MonetraDllName, EntryPoint = "M_Register_mutexdestroy", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_Register_mutexdestroy(ref IntPtr conn, M_Unregister_Mutex reg);

            /// <summary>
            /// Register a callback to intialize a mutex
            /// </summary>
            /// <param name="conn">M_CONN structure passed by reference, or an allocated M_CONN pointer</param>
            /// <param name="reg">pointer to M_Register_Mutex function to call when doing a mutex initialization</param>
            /// <returns>1 on success, 0 on failure</returns>
            [DllImport(MonetraDllName, EntryPoint = "M_Register_mutexinit", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_Register_mutexinit(ref IntPtr conn, M_Register_Mutex reg);

            /// <summary>
            /// Register a callback to lock the mutex
            /// </summary>
            /// <param name="conn">M_CONN structure passed by reference, or an allocated M_CONN pointer</param>
            /// <param name="reg">pointer to M_Mutex_Lock function to call when doing a mutex lock</param>
            /// <returns>1 on success, 0 on failure</returns>
            [DllImport(MonetraDllName, EntryPoint = "M_Register_mutexlock", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_Register_mutexlock(ref IntPtr conn, M_Mutex_Lock reg);

            /// <summary>
            /// Register a callback to unlock the mutex
            /// </summary>
            /// <param name="conn">M_CONN structure passed by reference, or an allocated M_CONN pointer</param>
            /// <param name="reg">pointer to M_Mutex_Unlock function to call when doing a mutex unlock</param>
            /// <returns>1 on success, 0 on failure</returns>
            [DllImport(MonetraDllName, EntryPoint = "M_Register_mutexunlock", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_Register_mutexunlock(ref IntPtr conn, M_Mutex_Unlock reg);

            /// <summary>
            /// Register a callback to the current threadid
            /// </summary>
            /// <param name="conn">M_CONN structure passed by reference, or an allocated M_CONN pointer</param>
            /// <param name="reg">pointer to M_ThreadID function to call when doing a threadid lookup</param>
            /// <returns>1 on success, 0 on failure</returns>
            [DllImport(MonetraDllName, EntryPoint = "M_Register_threadid", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int M_Register_threadid(ref IntPtr conn, M_ThreadID reg);

            /// <summary>
            /// Defines prototype for callback to lock a system mutex.
            /// </summary>
            /// <param name="mutex"></param>
            /// <returns>The function should return 1 on success, 0 on failure</returns>
            /// <remarks>MUST BE Cdecl!</remarks>
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int M_Mutex_Lock(uint mutex);

            /// <summary>
            /// Defines prototype for callback to unlock a system mutex.
            /// </summary>
            /// <param name="mutex"></param>
            /// <returns>The function should return 1 on success, 0 on failure</returns>
            /// <remarks>MUST BE Cdecl!</remarks>
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int M_Mutex_Unlock(uint mutex);

            /// <summary>
            /// Defines prototype for callback to register a system mutex.
            /// </summary>
            /// <param name="mutex"></param>
            /// <returns>The function should return a pointer to the mutex on success, or NULL on failure</returns>
            /// <remarks>MUST BE Cdecl!</remarks>
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate uint M_Register_Mutex();

            /// <summary>
            /// Defines prototype for callback to unregister a system mutex.
            /// </summary>
            /// <param name="mutex"></param>
            /// <returns>The function should return 1 on success, 0 on failure</returns>
            /// <remarks>MUST BE Cdecl!</remarks>
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int M_Unregister_Mutex(uint mutex);

            /// <summary>
            /// Defines prototype for callback to return the current threadid as an unsigned long.
            /// Introduced with libmonetra v5.4.
            /// </summary>
            /// <param name="mutex"></param>
            /// <returns>The function should return the current threadid</returns>
            /// <remarks>MUST BE Cdecl!</remarks>
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate uint M_ThreadID();
        }

        #region Private fields

        private Action<string> _log;
        private string _hostName;
        private ushort _serverSocket;
        IntPtr _connection;
        bool _connected;
        #endregion

        private string M_ResponseKeys_index(IntPtr keys, int num_keys, int i)
        {
            return MonetraDLL.M_ResponseKeys_index(keys, num_keys, i);
        }

        private void M_FreeResponseKeys(IntPtr keys, int num_keys)
        {
            MonetraDLL.M_FreeResponseKeys(keys, num_keys);
        }

        // List of mutexes
        private static Dictionary<uint, Mutex> monetraMutexes = new Dictionary<uint,Mutex>();
        private static uint currentMutexIndex = 2000;

        #region Monetra callback implementations

        /// <summary>
        /// Defines prototype for callback to lock a system mutex.
        /// </summary>
        /// <param name="mutex"></param>
        /// <returns>The function should return 1 on success, 0 on failure</returns>
        private static unsafe int M_Mutex_Lock(uint mutex)
        {
            try
            {
                Mutex toLock;
                lock (monetraMutexes)
                {
                    toLock = monetraMutexes[(uint)mutex];
                }
                toLock.WaitOne();
                return 1;
            }
            catch 
            {
                return 0;
            }
        }
        private MonetraDLL.M_Unregister_Mutex mutexdestroy;
        private MonetraDLL.M_Register_Mutex mutexinit;
        private MonetraDLL.M_Mutex_Lock mutexlock;
        private MonetraDLL.M_Mutex_Unlock mutexunlock;
        private MonetraDLL.M_ThreadID threadid;

        /// <summary>
        /// Defines prototype for callback to unlock a system mutex.
        /// </summary>
        /// <param name="mutex"></param>
        /// <returns>The function should return 1 on success, 0 on failure</returns>
        private static unsafe int M_Mutex_Unlock(uint mutex)
        {
            try
            {
                monetraMutexes[(uint)mutex].ReleaseMutex();
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Defines prototype for callback to register a system mutex.
        /// </summary>
        /// <param name="mutex"></param>
        /// <returns>The function should return a pointer to the mutex on success, or NULL on failure</returns>
        private static unsafe uint M_Register_Mutex()
        {
            try
            {
                uint index = currentMutexIndex;
                currentMutexIndex++;
                monetraMutexes.Add(index, new Mutex());
                return (uint)index;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Defines prototype for callback to unregister a system mutex.
        /// </summary>
        /// <param name="mutex"></param>
        /// <returns>The function should return 1 on success, 0 on failure</returns>
        private static unsafe int M_Unregister_Mutex(uint mutex)
        {
            try
            {
                monetraMutexes.Remove((uint)mutex);
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Defines prototype for callback to return the current threadid as an unsigned long.
        /// Introduced with libmonetra v5.4.
        /// </summary>
        /// <param name="mutex"></param>
        /// <returns>The function should return the current threadid</returns>
        private static unsafe uint M_ThreadID()
        {
            try
            {
                return (uint)System.Threading.Thread.CurrentThread.ManagedThreadId;
            }
            catch
            {
                return 0;
            }
        }

	    #endregion


        public override void NotConnected()
        {
            Disconnect();
        }

        public override bool checkConnection()
        {
            return (MonetraDLL.M_Monitor(ref _connection) != 0);
        }
    }

    /// <summary>
    /// Implements MonetraClient using the native .NET client provided by Monetra
    /// </summary>
    public class MonetraDotNetNativeClient : MonetraClient
    {
        libmonetra.Monetra monetra;
        private bool _connected;
        Action<string> log;

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
                log("Connected");                    
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
                    log("Connected");                    
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
        private class TransactionWrapper : MonetraClient.Transaction
        {
            private int transactionID;
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
