using System;
using System.Xml;
using System.Xml.Serialization;

namespace Authorization
{
    public interface AuthRequest
    {
        string merchantID { get; }
        string amount { get; }
        string transactionDesc { get; }
        string customerRef { get; }
        string merchantPassword { get; }
        string terminalSerNo { get; }

        string pan { get; }
        string expDateMMYY { get; }
        /// <summary>
        /// The track data from the credit card. Note that EITHER the track data OR the PAN 
        /// and expiry date must be provided.
        /// </summary>
        string trackData { get; }
    }

    /// <summary>
    /// This is an abstract class representing an object which can perform an authorization
    /// </summary>
    public abstract class Authorizer
    {
        public enum ResultCode { Approved, Declined, Error };

        /// <summary>
        /// Call this to perform an authorization
        /// </summary>
        /// <param name="request">The request object containing the request parameters</param>
        /// <param name="authCode">Depends on CC processor. Code string representing transaction result.</param>
        /// <param name="cardType"></param>
        /// <returns>Returns "true" for approved, and "false" for failed</returns>
        public abstract ResultCode authorize(AuthRequest request, 
            out string authCode, out string cardType, out string note);
    }

    /// <summary>
    /// This object presents a request with some given values for the read-only properties.
    /// </summary>
    public class PrescribedRequest : Authorization.AuthRequest
    {
        private string _amount;
        private string _pan;
        private string _expDateMMYY;
        private string _merchantID;
        private string _transactionDesc;
        private string _customerRef;
        private string _merchantPassword;
        private string _trackData;
        private string _terminalSerNo;
        
        public PrescribedRequest(string amount, string pan, string expDateMMYY, string merchantID,
            string merchantPassword, string transactionDesc, string customerRef, string trackData,
            string terminalSerNo)
        {
            this._amount = amount;
            this._pan = pan;
            this._expDateMMYY = expDateMMYY;
            this._merchantID = merchantID;
            this._transactionDesc = transactionDesc;
            this._customerRef = customerRef;
            this._merchantPassword = merchantPassword;
            this._trackData = trackData;
            this._terminalSerNo = terminalSerNo;
        }

        // Default constructor
        public PrescribedRequest()
        {
        }

        public string amount { get { return _amount; } }
        public string pan { get { return _pan; } }
        public string expDateMMYY { get { return _expDateMMYY; } }
        public string merchantID { get { return _merchantID; } }
        public string transactionDesc { get { return _transactionDesc; } }
        public string customerRef { get { return _customerRef; } }
        public string merchantPassword { get { return _merchantPassword; } }
        public string trackData { get { return _trackData; } }
        public string terminalSerNo { get { return _terminalSerNo; } }
    }

}