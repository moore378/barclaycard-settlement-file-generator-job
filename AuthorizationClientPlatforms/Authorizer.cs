using System;
using System.Xml;
using System.Xml.Serialization;
using System.Threading;

using System.Runtime.Serialization;
using System.Collections.Generic;

namespace AuthorizationClientPlatforms
{
    public interface IAuthorizationStatistics
    {
        int TotalProcessed { get; }
        int TotalErrors { get; }
        int TotalConnectionErrors { get; }
        int TotalApproved { get; }
        int TotalDeclined { get; }
        event Action<IAuthorizationStatistics> Changed;
    }

    public class AuthorizationStatistics : IAuthorizationStatistics
    {
        protected void OnChanged()
        {
            if (Changed != null)
                Changed(this);
        }
        public int TotalProcessed { get { return totalProcessed; } set { totalProcessed = value; OnChanged(); } }
        private int totalProcessed;
        private Action<IAuthorizationStatistics> changed = new Action<IAuthorizationStatistics>((stats) => { });

        public event Action<IAuthorizationStatistics> Changed;

        private int totalErrors;

        public int TotalErrors
        {
            get { return totalErrors; }
            set { totalErrors = value; OnChanged(); }
        }

        private int totalConnectionErrors;

        public int TotalConnectionErrors
        {
            get { return totalConnectionErrors; }
            set { totalConnectionErrors = value; OnChanged(); }
        }

        private int totalApproved;

        public int TotalApproved
        {
            get { return totalApproved; }
            set { totalApproved = value; OnChanged(); }
        }

        private int totalDeclined;

        public int TotalDeclined
        {
            get { return totalDeclined; }
            set { totalDeclined = value; OnChanged(); }
        }
    }

    public enum AuthorizeMode { Normal, Preauth, Finalize };

    /// <summary>
    /// This is an abstract class representing an object which can perform an authorization using a specific authorizing processor
    /// </summary>
    /// <exception cref="AuthorizerProcessingException">If there is a problem with the authorization. Note that the AllowRetry field of the exception specifies whether it is safe to retry the Authorization.</exception>
    public interface IAuthorizationPlatform
    {
        /// <summary>
        /// Call this to perform an authorization
        /// </summary>
        AuthorizationResponseFields Authorize(AuthorizationRequest request, AuthorizeMode mode);

        IAuthorizationStatistics Statistics { get; }
    }

    // Create something that implements this interface dynamically
    public class DynamicAuthorizationPlatform : IAuthorizationPlatform, IAuthorizationStatistics
    {
        private Func<AuthorizationRequest, AuthorizeMode, AuthorizationResponseFields> Authorize;

        public DynamicAuthorizationPlatform(Func<AuthorizationRequest, AuthorizeMode, AuthorizationResponseFields> authorize)
        {
            this.Authorize = authorize;
        }

        AuthorizationResponseFields IAuthorizationPlatform.Authorize(AuthorizationRequest request, AuthorizeMode mode)
        {
            AuthorizationResponseFields result = Authorize(request, mode);
            switch (result.resultCode)
            {
                case AuthorizationResultCode.Approved:
                    Interlocked.Increment(ref totalApproved);
                    Interlocked.Increment(ref totalProcessed);
                    break;
                case AuthorizationResultCode.ConnectionError:
                    Interlocked.Increment(ref totalConnectionErrors);
                    Interlocked.Increment(ref totalErrors);
                    break;
                case AuthorizationResultCode.UnknownError:
                    Interlocked.Increment(ref totalErrors);
                    break;
                case AuthorizationResultCode.Declined:
                    Interlocked.Increment(ref totalDeclined);
                    Interlocked.Increment(ref totalProcessed);
                    break;
                default: throw new Exception("Unexpected code");
            }
            if (Changed != null)
                Changed(this);
            return result;
        }

        public IAuthorizationStatistics Statistics
        {
            get { return this; }
        }

        private int totalProcessed;
        public int TotalProcessed { get { return totalProcessed; } }

        private int totalErrors;
        public int TotalErrors { get { return totalErrors; } }

        private int totalConnectionErrors;
        public int TotalConnectionErrors { get { return totalConnectionErrors; } }

        private int totalApproved;
        public int TotalApproved { get { return totalApproved; } }

        private int totalDeclined;
        public int TotalDeclined { get { return totalDeclined; } }

        public event Action<IAuthorizationStatistics> Changed;
    }

    /// <summary>
    /// This structure is returned from the authorization
    /// </summary>
    [DataContract]
    public class AuthorizationResponseFields
    {
        /// <summary>
        /// Authorizer-independent code indicating result of authorization
        /// </summary>
        [DataMember]
        public AuthorizationResultCode resultCode { get; set; }

        /// <summary>
        /// Authorizer-dependent human-readable string indicating result of authorization
        /// </summary>
        [DataMember]
        public string authorizationCode { get; set; }

        [DataMember]
        public string cardType { get; set; }

        [DataMember]
        public string receiptReference { get; set; }

        /// <summary>
        /// Any notes about the transaction that should be logged
        /// </summary>
        [DataMember]
        public string note { get; set; }

        [DataMember]
        public int Ttid { get; set; }

        [DataMember]
        public short BatchNum { get; set; }

        /// <summary>
        /// Some processors return back the fee that are added onto the charge amount.
        /// ONLY use this for when the processor's authorized amount is different
        /// than the requested amount due to the extra credit card fee being added on
        /// by the processor.
        /// </summary>
        [DataMember]
        public decimal AdditionalCCFee { get; set; }

        public AuthorizationResponseFields(
            AuthorizationResultCode resultCode,
            string authorizationCode,
            string cardType,
            string receiptReference,
            string note,
            int ttid,
            short batchNum)
        {
            this.resultCode = resultCode;
            this.authorizationCode = authorizationCode;
            this.cardType = cardType;
            this.receiptReference = receiptReference;
            this.note = note;
            this.Ttid = ttid;
            this.BatchNum = batchNum;
        }

        public AuthorizationResponseFields()
        {
            // Nothing on purpose.
        }
    }

    public class FinalizeResponse
    {
    }

    public enum AuthorizationResultCode
    {
        /// <summary>
        /// The transaction was approved
        /// </summary>
        Approved,
        /// <summary>
        /// The transaction was declined
        /// </summary>
        Declined,
        /// <summary>
        /// The authorizer could not communicate to the remote server - ONLY SPECIFY THIS IF
        /// YOU KNOW THAT THE TRANSACTION WAS NOT SUBMITTED TO THE SERVER (I.E. IT WOULD NOT
        /// BE A PROBLEM IF THE CONTROLLER RETRIES THE TRANSACTION)
        /// </summary>
        ConnectionError,
        /// <summary>
        /// Unspecified error. Details may be given in the AuthorizationResponseFields.authorizationCode and AuthorizationResponseFields.note
        /// </summary>
        UnknownError
    };

    /// <summary>
    /// Details needed by an Authorization Processor to complete an authorization.
    /// NOTE: This is an interface used by both the Authorization Platform and Authorization Processor.
    /// </summary>
    [DataContract]
    public class AuthorizationRequest
    {
        [DataMember]
        public string MeterSerialNumber { get; set; }

        [DataMember]
        public DateTime StartDateTime { get; set; }

        [DataMember]
        public string MerchantID
        {
            /*
            get
            {
                string value;
                
                ProcessorSettings.TryGetValue("MerchantID", out value);

                return value;
            }
            set
            {
                ProcessorSettings["MerchantID"] = value;
            }
             */

            get;
            set;
        }

        [DataMember]
        public string MerchantPassword
        {
            /*
            get
            {
                string value;

                ProcessorSettings.TryGetValue("MerchantPassword", out value);

                return value;
            }
            set
            {
                ProcessorSettings["MerchantPassword"] = value;
            }
             */
            get;
            set;
        }

        [DataMember]
        public Dictionary<string, string> ProcessorSettings{ get; set; }

        [DataMember]
        public string Pan { get; set; }

        [DataMember]
        public string ExpiryDateMMYY { get; set; }

        [DataMember]
        public decimal AmountDollars { get; set; }

        [DataMember]
        public string OrderNumber { get; set; }

        [DataMember]
        public string TransactionDescription { get; set; }

        [DataMember]
        public string Invoice { get; set; }

        [DataMember]
        public string TrackTwoData { get; set; }

        /// <summary>
        /// Fixed length track 1 and track 2 data.
        /// NOTE: This is formatted such that it's a fixed length string with Track 1 not having sentinels but Track 2 does.
        /// </summary>
        [DataMember]
        public string FullTrackData { get; set; }

        /// <summary>
        /// Unique string identifying the transaction record as provided by the database.
        /// </summary>
        [DataMember]
        public string IDString { get; set; }

        [DataMember]
        public string CustomerReference { get; set; }

        [DataMember]
        public int? PreauthTtid { get; set; }

        public AuthorizationRequest()
        {
            // NOTE: newer processors should be using the processor settings 
            // dictionary instead of the MerchantID and MerchantPassword 
            // properties.
            ProcessorSettings = new Dictionary<string, string>();
        }

        /// <summary>
        /// Legacy constructor for authorization request properties.
        /// </summary>
        /// <param name="meterID"></param>
        /// <param name="startDateTime"></param>
        /// <param name="merchantID">ID identifying the merchant. NOTE: Newer integration should be referencing ProcessorSettings["MerchantID"] instead.</param>
        /// <param name="merchantPassword">Password credentials of the merchant. NOTE: Newer integration should be referencing ProcessorSettings["MerchantPassword"] instead.</param>
        /// <param name="pan"></param>
        /// <param name="expDateMMYY"></param>
        /// <param name="amount"></param>
        /// <param name="transactionDesc"></param>
        /// <param name="invoice"></param>
        /// <param name="customerReference"></param>
        /// <param name="trackTwoData"></param>
        /// <param name="fullTrackData"></param>
        /// <param name="idString"></param>
        /// <param name="orderNumber"></param>
        /// <param name="preauthTtid"></param>
        public AuthorizationRequest(
            string meterID,
            DateTime startDateTime,
            string merchantID,
            string merchantPassword,
            string pan,
            string expDateMMYY,
            decimal amount,
            string transactionDesc,
            string invoice,
            string customerReference,
            string trackTwoData,
            string fullTrackData,
            string idString,
            string orderNumber,
            int? preauthTtid):this()
        {
            MeterSerialNumber = meterID;
            StartDateTime = startDateTime;
            MerchantID = merchantID;
            MerchantPassword = merchantPassword;
            Pan = pan;
            ExpiryDateMMYY = expDateMMYY;
            AmountDollars = amount;
            TransactionDescription = transactionDesc;
            Invoice = invoice;
            TrackTwoData = trackTwoData;
            FullTrackData = fullTrackData;
            IDString = idString;
            CustomerReference = customerReference;
            OrderNumber = orderNumber;
            PreauthTtid = preauthTtid;

            // Add support for newer integration code that only looks
            // at the ProcessorSettings property.
            ProcessorSettings["MerchantID"] = merchantID;
            ProcessorSettings["MerchantPassword"] = merchantPassword;
        }

        /// <summary>
        /// Constructor for authorization request properties.
        /// </summary>
        /// <param name="meterID"></param>
        /// <param name="startDateTime"></param>
        /// <param name="processorSettings">Processor-specific data that needs to be sent out as part of the authorization, such as merchant ID and password.</param>
        /// <param name="pan"></param>
        /// <param name="expDateMMYY"></param>
        /// <param name="amount"></param>
        /// <param name="transactionDesc"></param>
        /// <param name="invoice"></param>
        /// <param name="customerReference"></param>
        /// <param name="trackTwoData"></param>
        /// <param name="fullTrackData"></param>
        /// <param name="idString"></param>
        /// <param name="orderNumber"></param>
        /// <param name="preauthTtid"></param>
        public AuthorizationRequest
            (
            string meterID,
            DateTime startDateTime,
            Dictionary<string, string> processorSettings,
            string pan,
            string expDateMMYY,
            decimal amount,
            string transactionDesc,
            string invoice,
            string customerReference,
            string trackTwoData,
            string fullTrackData,
            string idString,
            string orderNumber,
            int? preauthTtid
            ):this()
        {
            MeterSerialNumber = meterID;
            StartDateTime = startDateTime;
            ProcessorSettings = processorSettings;
            Pan = pan;
            ExpiryDateMMYY = expDateMMYY;
            AmountDollars = amount;
            TransactionDescription = transactionDesc;
            Invoice = invoice;
            TrackTwoData = trackTwoData;
            FullTrackData = fullTrackData;
            IDString = idString;
            CustomerReference = customerReference;
            OrderNumber = orderNumber;
            PreauthTtid = preauthTtid;

            // Add support for any legacy code that references the properties.
            MerchantID = ProcessorSettings["MerchantID"];
            MerchantPassword = ProcessorSettings["MerchantPassword"];
        }
    }

    public class FinalizeRequest
    {
        public string ttid;
        public decimal finalAmount;
        public string merchantID;
        public string merchantPassword;

        public FinalizeRequest(string ttid, decimal finalAmount, string merchantID, string merchantPassword)
        {
            this.ttid = ttid;
            this.finalAmount = finalAmount;
            this.merchantID = merchantID;
            this.merchantPassword = merchantPassword;
        }
    }

    /// <summary>
    /// Occurs when there is a problem authorizing. 
    /// </summary>
    public class AuthorizerProcessingException : Exception
    {
        /// <summary>
        /// Is it safe to retry the transaction?
        /// </summary>
        public bool AllowRetry { get; private set; }

        public AuthorizerProcessingException(string message, bool allowRetry)
            : base(message)
        {
            AllowRetry = allowRetry;
        }

        public AuthorizerProcessingException(string message, Exception innerException, bool allowRetry)
            : base(message, innerException)
        {
            AllowRetry = allowRetry;
        }
    }

    /// <summary>
    /// Occurs when the authorizer can't establish a connection to the sever and has not attempted the authorization.
    /// </summary>
    public class AuthorizerConnectException : Exception
    {
        public AuthorizerConnectException(string message)
            : base(message)
        {
        }

        public AuthorizerConnectException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

}