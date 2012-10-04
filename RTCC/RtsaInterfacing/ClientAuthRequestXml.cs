using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Rtcc.RtsaInterfacing
{
    /// <summary>
    /// This object represents authorization request data from the client.
    /// </summary>
    [XmlRootAttribute("AuthRequest")]
    public class ClientAuthRequestXML
    {
        [XmlElement(ElementName = "StructureVersion")]
        public string StructureVersion;
        [XmlElement(ElementName = "MerchantID")]
        public string merchantID;
        [XmlElement(ElementName = "TransactionDesc")]
        public string transactionDesc;
        [XmlElement(ElementName = "Invoice")]
        public string invoice;
        [XmlElement(ElementName = "Amount")]
        public string amount;
        [XmlElement(ElementName = "MerchantPassword")]
        public string merchantPassword;

        [XmlElement(ElementName = "MeterID")]
        public string meterID;
        /// <summary>
        /// The StartDateTime of the transaction, recorded as UMT+00:00 (even though it isn't). If the 
        /// transaction started at 9am UMT-08:00, then this time will say 9am UMT+00:00. This is so 
        /// that one can use DateTime.ToUniversalTime() to get the time of the transaction as measured
        /// from the local parking meter.
        /// </summary>
        [XmlElement(ElementName = "LocalStartDateTime")]
        public DateTime LocalStartDateTime;
        [XmlElement(ElementName = "CCTransactionIndex")]
        public int ccTransactionIndex;
        [XmlElement(ElementName = "EncryptionMethod")]
        public int encryptionMethod;
        [XmlElement(ElementName = "KeyVer")]
        public int keyVer;
        [XmlElement(ElementName = "RequestType")]
        public int requestType;
        [XmlElement(ElementName = "UniqueRecNo")]
        public string uniqueRecNo;
        [XmlElement(ElementName = "CCTrack")]
        public string ccTrackBase64;
        [XmlElement(ElementName = "PurchasedTime")]
        public int purchasedTime;
        [XmlElement(ElementName = "UniqueNumber2")]
        public long UniqueNumber2;

        [XmlElement(ElementName = "Flags")]
        public int Flags;
    }
}
