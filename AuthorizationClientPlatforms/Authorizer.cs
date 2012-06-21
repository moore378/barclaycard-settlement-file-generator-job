using System;
using System.Xml;
using System.Xml.Serialization;
using System.Threading;

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
                switch (result.resultCode) {
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
    public class AuthorizationResponseFields
    {
        /// <summary>
        /// Authorizer-independent code indicating result of authorization
        /// </summary>
        public AuthorizationResultCode resultCode;
        /// <summary>
        /// Authorizer-dependent human-readable string indicating result of authorization
        /// </summary>
        public string authorizationCode;
        public string cardType;
        public string receiptReference;
        /// <summary>
        /// Any notes about the transaction that should be logged
        /// </summary>
        public string note;
        public int Ttid;
        public short BatchNum;

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
    }

    public class FinalizeResponse
    {
    }

    public enum AuthorizationResultCode {
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
    /// This structure is passed to the processor server object with details needed
    /// to complete the authorization.
    /// </summary>
    public class AuthorizationRequest
    {
        public string MeterSerialNumber;
        public DateTime StartDateTime;
        public string MerchantID;
        public string MerchantPassword;
        public string Pan;
        public string ExpiryDateMMYY;
        public decimal AmountDolars;
        public string OrderNumber;
        public string TransactionDescription;
        public string Invoice;
        public string TrackTwoData;
        public string FullTrackData;
        /// <summary>
        /// Unique string identifying the record as provided by the database
        /// </summary>
        public string IDString;
        public string CustomerReference;
        public int? PreauthTtid;

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
            int? preauthTtid)
        {
            this.MeterSerialNumber = meterID;
            this.StartDateTime = startDateTime;
            this.MerchantID = merchantID;
            this.MerchantPassword = merchantPassword;
            this.Pan = pan;
            this.ExpiryDateMMYY = expDateMMYY;
            this.AmountDolars = amount;
            this.TransactionDescription = transactionDesc;
            this.Invoice = invoice;
            this.TrackTwoData = trackTwoData;
            this.FullTrackData = fullTrackData;
            this.IDString = idString;
            this.CustomerReference = customerReference;
            this.OrderNumber = orderNumber;
            this.PreauthTtid = preauthTtid;
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