using System;
using System.Runtime.InteropServices;

namespace MonetraNS // Monetra namespace
{
    /// <summary>
    /// This concrete class performs authorization using the Monetra server
    /// </summary>
    public unsafe class Monetra_Authorizer : Authorization.Authorizer
    {
        private Delegates.Log log;
        private MonetraClient monetra;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="monetraClient">Client interface with which the authorizer will communicate with monetra.</param>
        /// <param name="aLog"></param>
        public Monetra_Authorizer(MonetraClient monetraClient, Delegates.Log aLog)
        {
            log = aLog;

            monetra = monetraClient;
        }

        ~Monetra_Authorizer()
        {

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
        public override ResultCode authorize(Authorization.AuthRequest request,
            out string authCode, out string cardType, out string note)
        {
            MonetraClient.Transaction transaction;
            ResultCode result = ResultCode.Error;

            if (!monetra.connected)
            {
                log("Cannot perform transaction, not connected to Monetra server");
                authCode = "";
                cardType = "";
                note = "Monetra not connected";
                return ResultCode.Error;
            }

            // Create a new transaction
            transaction = monetra.newTransaction();

            // Assign transaction fields
            transaction.username = request.merchantID;
            transaction.password = request.merchantPassword;
            transaction.action = "sale";
            transaction.custref = request.customerRef;
            transaction.stationid = request.terminalSerNo;
            transaction.amount = request.amount;

            if (request.trackData != "")
                transaction.trackdata = request.trackData;
            else
            {
                transaction.account = request.pan;
                transaction.expdate = request.expDateMMYY;
            }

            // Send the transaction
            MonetraClient.Transaction.Response response = transaction.send();

            if (response.resultOfSend == 0)
            {
                log("Failed to send transation: " + monetra.connectionErrorStr);
                result = ResultCode.Error;
            }

            switch (response.returnStatus)
            {
                case MonetraClient.Transaction.Response.ReturnStatus.M_SUCCESS:
                    {
                        result = ResultCode.Approved;
                        note = "Approved";
                        break;
                    }
                case MonetraClient.Transaction.Response.ReturnStatus.M_FAIL:
                    {
                        log("Transaction declined (phard_code=" + response.phard_code + ")");
                        result = ResultCode.Declined;
                        note = "Declined";
                        break;
                    }
                default /*case IMonetraTransResponse.ReturnStatus.M_FAIL*/:
                    {
                        log("Error processing transaction");
                        result = ResultCode.Error;
                        note = response.allResponseKeys("\t");
                        break;
                    }
            }

            cardType = response.cardType;
            authCode = response.auth;

            transaction.Delete();

            return result;
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
            /// Finalizes a transaction and sends it to the Monetra server
            /// </summary>
            public abstract Response send();

            /// <summary>
            /// Removes a transaction from the queue that was initialized with M_TransNew
            /// </summary>
            public abstract void Delete();

            public abstract class Response
            {
                public enum ReturnStatus { M_ERROR = -1, M_FAIL = 0, M_SUCCESS = 1 };
                public abstract int resultOfSend { get; }

                /// <summary>
                /// Returns a success/fail response for every transaction. If a detailed code is needed, please see M_ReturnCode.
                /// </summary>
                public abstract ReturnStatus returnStatus { get; }

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
                public abstract string code { get; }

                public abstract string phard_code { get; }

                public abstract string cardType { get; }

                /// <summary>
                /// authorization number, typically 6 digits numericonly, but some processors in test mode will return alpha-numeric responses 
                /// </summary>
                public abstract string auth { get; }

                public abstract string allResponseKeys(string delimeter);
            }


        }
    }

    /// <summary>
    /// Implementation of the IMonetraClient interface using the monetra DLL
    /// </summary>
    public unsafe class MonetraDLLClient : MonetraClient
    {
        /// <summary>
        /// Constructor. Connects to Monetra server using given host name and port.
        /// </summary>
        /// <param name="pConnPtr">Pointer to connection pointer. This pointer must remain valid and fixed
        /// for the entire existence of the MonetraDLLClient object. </param>
        /// <param name="HostName"></param>
        /// <param name="ServerSocket"></param>
        /// <param name="aLog"></param>
        public MonetraDLLClient(IntPtr pConnPtr, string HostName, ushort ServerSocket, Delegates.Log aLog)
        {
            _log = aLog;
            this._hostName = HostName;
            this._serverSocket = ServerSocket;
            this._connectionPtr = pConnPtr;

            // Initialize the monetra engine
            MonetraDLL.M_InitEngine_ex(MonetraDLL.m_ssllock_style.M_SSLLOCK_NONE);// M_SSLLOCK_INTERNAL);
            // Set up the connection
            MonetraDLL.M_InitConn((IntPtr*)_connectionPtr.ToPointer()); // M_CONN*

            MonetraDLL.M_SetIP((IntPtr*)_connectionPtr.ToPointer(), HostName, ServerSocket);
            //Monetra.M_SetSSL((IntPtr*)_connectionPtr.ToPointer(), HostName, ServerSocket);
            MonetraDLL.M_SetBlocking((IntPtr*)_connectionPtr.ToPointer(), 1);
            if (MonetraDLL.M_Connect((IntPtr*)_connectionPtr.ToPointer()) != 0)
            {
                //log("Connected to Monetra server");
                _connected = true;
            }
            else
            {
                _log("Failed to connect: " + MonetraDLL.M_ConnectionError((IntPtr*)_connectionPtr.ToPointer()));
                _connected = false;
            }

        }

        ~MonetraDLLClient()
        {
            Disconnect();
        }

        public override void Disconnect()
        {
            if (_connected)
            {
                _connected = false;
                MonetraDLL.M_DestroyConn((IntPtr*)_connectionPtr.ToPointer());
                MonetraDLL.M_DestroyEngine();
            }
        }


        #region Implementation of MonetraClient superclass
        public override bool connected { get { return _connected; } }

        public override string connectionErrorStr
        {
            get
            {
                return MonetraDLL.M_ConnectionError((IntPtr*)_connectionPtr.ToPointer());
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
            return MonetraDLL.M_TransNew((IntPtr*)_connectionPtr.ToPointer());
        }

        /// <summary>
        /// Finalizes a transaction and sends it to the Monetra server
        /// </summary>
        /// <param name="transID">Reference for transaction as returned by M_TransNew()</param>
        /// <returns>1 on success, 0 on failure</returns>
        private int M_TransSend(IntPtr transID)
        {
            return MonetraDLL.M_TransSend((IntPtr*)_connectionPtr.ToPointer(), transID);
        }

        /// <summary>
        /// Returns a success/fail response for every transaction. If a detailed code is needed, please see M_ReturnCode
        /// </summary>
        /// <param name="transID">Reference for transaction as returned by M_TransNew()</param>
        /// <returns>1 if transaction successful (authorization), 0 if transaction failed (denial)</returns>
        private int M_ReturnStatus(IntPtr transID)
        {
            return MonetraDLL.M_ReturnStatus((IntPtr*)_connectionPtr.ToPointer(), transID);
        }

        /// <summary>
        /// This function is used to retrieve the response key/value pairs from the Monetra Engine, as specified in the Monetra Client Interface Protocol Specification.
        /// </summary>
        /// <param name="transID">Reference for transaction as returned by M_TransNew()</param>
        /// <param name="key">Response Parameter key as defined in the Monetra Client Interface Specification</param>
        /// <returns>value associated with the key requested. NULL if not found.</returns>
        private string M_ResponseParam(IntPtr transID, string key)
        {
            return MonetraDLL.M_ResponseParam((IntPtr*)_connectionPtr.ToPointer(), transID, key);
        }

        /// <summary>
        /// Adds a key/value pair for a transaction to be sent to Monetra
        /// </summary>
        private void M_TransKeyVal(IntPtr transID, string key, string val)
        {
            if (MonetraDLL.M_TransKeyVal((IntPtr*)_connectionPtr.ToPointer(), transID, key, val) != 1)
                throw new Exception("Could not set monetra key value pair");
        }


        /// <summary>
        /// Removes a transaction from the queue that was initialized with M_TransNew
        /// </summary>
        private void M_DeleteTrans(IntPtr transactionID)
        {
            MonetraDLL.M_DeleteTrans((IntPtr*)_connectionPtr.ToPointer(), transactionID);
        }

        public IntPtr M_ResponseKeys(IntPtr _transactionID, out int numKeys)
        {
            int _numKeys;
            IntPtr result = MonetraDLL.M_ResponseKeys((IntPtr*)_connectionPtr.ToPointer(), _transactionID, &_numKeys);
            numKeys = _numKeys;
            return result;
        }

        private class MonetraDLLTransaction : MonetraClient.Transaction
        {
            /// <summary>
            /// Create a transaction object
            /// </summary>
            /// <param name="connection">Reference to the monetra connection</param>
            public MonetraDLLTransaction(MonetraDLLClient client, Delegates.Log log)
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

            private MonetraDLLClient _client;
            private IntPtr _transactionID;
            private Delegates.Log log;

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
        }

        private class MonetraDLLTransResponse : MonetraClient.Transaction.Response
        {
            public MonetraDLLTransResponse(MonetraDLLTransaction transaction, int resultOfSend, Delegates.Log log)
            {
                this._transaction = transaction;
                this._resultOfSend = resultOfSend;
                this.log = log;
            }

            public override int resultOfSend
            {
                get { return this._resultOfSend; }
            }

            public override Transaction.Response.ReturnStatus returnStatus
            {
                get
                {
                    switch (_transaction.M_ReturnStatus())
                    {
                        case MonetraDLL.M_SUCCESS:
                            return Transaction.Response.ReturnStatus.M_SUCCESS;
                        case MonetraDLL.M_FAIL:
                            return Transaction.Response.ReturnStatus.M_FAIL;
                        default /* MonetraDLL.M_ERROR */:
                            return Transaction.Response.ReturnStatus.M_ERROR;
                    }
                }
            }

            public override string code
            {
                get { return _transaction.M_ResponseParam("code"); }
            }

            public override string cardType
            {
                get { return _transaction.M_ResponseParam("cardtype"); }
            }

            public override string phard_code
            {
                get { return _transaction.M_ResponseParam("phard_code"); }
            }

            public override string auth
            {
                get { return _transaction.M_ResponseParam("auth"); }
            }

            public override string allResponseKeys(string delimeter)
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
            private Delegates.Log log;
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
                M_SSLLOCK_INTERNAL = 2	/* LibMonetra is responsible for all OpenSSL
				 * code.  OpenSSL should not be used outside
				 * of libmonetra calls in an application if this
				 * is used, especially in multi-threaded code */
            };

            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_InitEngine(string cafile);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_InitEngine_ex(m_ssllock_style lockstyle);
            [DllImport("libmonetra.dll")]
            unsafe public static extern void M_DestroyEngine();
            [DllImport("libmonetra.dll")]
            unsafe public static extern void M_InitConn(IntPtr* conn);
            [DllImport("libmonetra.dll")]
            unsafe public static extern void M_DestroyConn(IntPtr* conn);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_SetIP(IntPtr* conn, string host, ushort port);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_SetSSL(IntPtr* conn, string host, ushort port);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_SetSSL_CAfile(IntPtr* conn, string path);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_SetSSL_Files(IntPtr* conn, string sslkeyfile, string sslcertfile);
            [DllImport("libmonetra.dll")]
            unsafe public static extern void M_VerifySSLCert(IntPtr* conn, int tf);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_SetBlocking(IntPtr* conn, int tf);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_Connect(IntPtr* conn);
            [DllImport("libmonetra.dll")]
            unsafe public static extern IntPtr M_TransNew(IntPtr* conn);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_TransKeyVal(IntPtr* conn, IntPtr id, string key, string val);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_TransSend(IntPtr* conn, IntPtr id);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_Monitor(IntPtr* conn);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_CheckStatus(IntPtr* conn, IntPtr id);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_ReturnStatus(IntPtr* conn, IntPtr id);
            [DllImport("libmonetra.dll")]
            unsafe public static extern void M_DeleteTrans(IntPtr* conn, IntPtr id);
            [DllImport("libmonetra.dll", EntryPoint = "M_ResponseParam")]
            unsafe public static extern IntPtr M_ResponseParam_int(IntPtr* conn, IntPtr id, string key);
            [DllImport("libmonetra.dll", EntryPoint = "M_ConnectionError")]
            unsafe public static extern IntPtr M_ConnectionError_int(IntPtr* conn);
            [DllImport("libmonetra.dll")]
            unsafe public static extern IntPtr M_ResponseKeys(IntPtr* conn, IntPtr id, int* num_keys);
            [DllImport("libmonetra.dll", EntryPoint = "M_ResponseKeys_index")]
            unsafe public static extern IntPtr M_ResponseKeys_index_int(IntPtr keys, int num_keys, int idx);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_FreeResponseKeys(IntPtr keys, int num_keys);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_IsCommaDelimited(IntPtr* conn, IntPtr id);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_ParseCommaDelimited(IntPtr* conn, IntPtr id);
            [DllImport("libmonetra.dll", EntryPoint = "M_GetCell")]
            unsafe public static extern IntPtr M_GetCell_int(IntPtr* conn, IntPtr id, string column, int row);
            [DllImport("libmonetra.dll", EntryPoint = "M_GetCellByNum")]
            unsafe public static extern IntPtr M_GetCellByNum_int(IntPtr* conn, IntPtr id, int column, int row);
            [DllImport("libmonetra.dll", EntryPoint = "M_GetHeader")]
            unsafe public static extern IntPtr M_GetHeader_int(IntPtr* conn, IntPtr id, int column);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_NumRows(IntPtr* conn, IntPtr id);
            [DllImport("libmonetra.dll")]
            unsafe public static extern int M_NumColumns(IntPtr* conn, IntPtr id);

            unsafe public static string M_ResponseParam(IntPtr* conn, IntPtr id, string key)
            {
                return (Marshal.PtrToStringAnsi(M_ResponseParam_int(conn, id, key)));
            }
            unsafe public static string M_ConnectionError(IntPtr* conn)
            {
                return (Marshal.PtrToStringAnsi(M_ConnectionError_int(conn)));
            }
            unsafe public static string M_ResponseKeys_index(IntPtr keys, int num_keys, int idx)
            {
                return (Marshal.PtrToStringAnsi(M_ResponseKeys_index_int(keys, num_keys, idx)));
            }
            unsafe public static string M_GetCell(IntPtr* conn, IntPtr id, string column, int row)
            {
                return (Marshal.PtrToStringAnsi(M_GetCell_int(conn, id, column, row)));
            }
            unsafe public static string M_GetCellByNum(IntPtr* conn, IntPtr id, int column, int row)
            {
                return (Marshal.PtrToStringAnsi(M_GetCellByNum_int(conn, id, column, row)));
            }
            unsafe public static string M_GetHeader(IntPtr* conn, IntPtr id, int column)
            {
                return (Marshal.PtrToStringAnsi(M_GetHeader_int(conn, id, column)));
            }

            /* M_ReturnStatus codes */
            public const int M_SUCCESS = 1;
            public const int M_FAIL = 0;
            public const int M_ERROR = -1;

            /* M_CheckStatus codes */
            public const int M_DONE = 2;
            public const int M_PENDING = 1;
        }

        #region Private fields

        private Delegates.Log _log;
        private string _hostName;
        private ushort _serverSocket;
        IntPtr _connectionPtr;
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
    }

}

namespace Delegates
{
    /// <summary>
    /// Call this to log a message to the subscribers
    /// </summary>
    /// <param name="msg"></param>
    public delegate void Log(string msg);
}
