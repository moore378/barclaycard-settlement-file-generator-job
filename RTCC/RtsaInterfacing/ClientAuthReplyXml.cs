using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Rtcc.RtsaInterfacing
{
    [XmlRootAttribute("AuthRequestReply")]
    public struct ClientAuthResponseXML
    {
        [XmlElement(ElementName = "Accepted")]
        public int Accepted;

        [XmlElement(ElementName = "ReceiptReference")]
        public string ReceiptReference;

        [XmlElement(ElementName = "RespCode")]
        public string ResponseCode;

        [XmlElement(ElementName = "Amount")]
        public decimal AmountDollars;
    }
}
