using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rtcc.Database
{
    public struct CCProcessorInfo
    {
        /// <summary>
        /// Terminal ID (internal use - could omit)
        /// </summary>
        public int TerminalID;
        /// <summary>
        /// Terminal Serial # for Receipt
        /// </summary>
        public string TerminalSerialNumber;
        /// <summary>
        /// City Name for receipt
        /// </summary>
        public string CompanyName;
        /// <summary>
        /// Username for Monetra
        /// </summary>
        public string MerchantID;
        /// <summary>
        /// Password for Monetra
        /// </summary>
        public string MerchantPassword;
        /// <summary>
        /// 'Monetra' only 
        /// </summary>
        public string ClearingPlatform;
        /// <summary>
        /// Pole Ser No for receipt (returns null if not associated to pole)
        /// </summary>
        public string PoleSerialNumber;
        /// <summary>
        /// Pole ID (internal use - could omit)
        /// </summary>
        public int PoleID;
        /// <summary>
        /// Time Zone Offset from GMT (-8 Pac, -7 mountain, -6 is central and -5 is eastern)
        /// </summary>
        public decimal TimeZoneOffset;
        /// <summary>
        /// 1.00 is daylight saving currently observed, 0 otherwise
        /// </summary>
        public decimal DstAdjust;
        /// <summary>
        /// Phone # for that Terminal to SMS it
        /// </summary>
        public string PhoneNumber;
        /// <summary>
        /// IP address of that phone #
        /// </summary>
        public string IP;

        public CCProcessorInfo(
            int terminalID,
            string terminalSerialNumber,
            string companyName,
            string merchantID,
            string merchantPassword,
            string clearingPlatform,
            string poleSerialNumber,
            int poleID,
            decimal timeZoneOffset,
            decimal dstAdjust,
            string phoneNumber,
            string ip)
        {
            this.TerminalID = terminalID;
            this.TerminalSerialNumber = terminalSerialNumber;
            this.CompanyName = companyName;
            this.MerchantID = merchantID;
            this.MerchantPassword = merchantPassword;
            this.ClearingPlatform = clearingPlatform;
            this.PoleSerialNumber = poleSerialNumber;
            this.PoleID = poleID;
            this.TimeZoneOffset = timeZoneOffset;
            this.DstAdjust = dstAdjust;
            this.PhoneNumber = phoneNumber;
            this.IP = ip;
        }
    }
}
