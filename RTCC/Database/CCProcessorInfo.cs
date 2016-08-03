using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AutoDatabase;

namespace Rtcc.Database
{
    public class CCProcessorInfo
    {
        /// <summary>
        /// Terminal ID (internal use - could omit)
        /// </summary>
        public decimal TerminalID;

        /// <summary>
        /// Terminal Serial # for Receipt
        /// </summary>
        [DatabaseReturn(SqlName = "TerminalSerNo")]
        public string TerminalSerialNumber;
        /// <summary>
        /// City Name for receipt
        /// </summary>
        public string CompanyName;

        /// <summary>
        /// Username for Monetra
        /// NOTE: Legacy property. Utilize "MerchantID" from  ProcessorSettings instead.
        /// </summary>
        [DatabaseReturn(SqlName = "CCTerminalID")]
        public string MerchantID
        {
            // Use the ProcessorSettings' version under the covers.
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
        }

        /// <summary>
        /// Password for Monetra
        /// NOTE: Legacy property. Utilize "MerchantPassword" from ProcessorSettings instead.
        /// </summary>
        [DatabaseReturn(SqlName = "CCTransactionKey")]
        public string MerchantPassword
        {
            // Use the ProcessorSettings' version under the covers.
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
        }

        /// <summary>
        /// Processor-specific data that needs to be sent out.
        /// </summary>
        [DatabaseReturn(Ignore = true)]
        public Dictionary<string, string> ProcessorSettings;


        private string _clearingPlatform;

        /// <summary>
        /// 'Monetra', Israel-Premium, Fis-PayDirect
        /// </summary>
        [DatabaseReturn(SqlName = "CCClearingPlatform")]
        public string ClearingPlatform
        {
            get
            {
                return _clearingPlatform;
            }
            set
            {
                // Force things to be in lowercase for later easier switch handling...
                _clearingPlatform = value.ToLower();
            }
        }

        /// <summary>
        /// Pole Ser No for receipt (returns null if not associated to pole)
        /// </summary>
        [DatabaseReturn(SqlName = "PoleSerNo")]
        public string PoleSerialNumber;

        /// <summary>
        /// Pole ID (internal use - could omit)
        /// </summary>
        public decimal PoleID;

        /// <summary>
        /// Time Zone Offset from GMT (-8 Pac, -7 mountain, -6 is central and -5 is eastern)
        /// </summary>
        public decimal TimeZoneOffset;

        /// <summary>
        /// 1.00 is daylight saving currently observed, 0 otherwise
        /// </summary>
        [DatabaseReturn(SqlName = "DST_Adjust")]
        public decimal DstAdjust;

        /// <summary>
        /// Phone # for that Terminal to SMS it
        /// </summary>
        public string PhoneNumber;

        /// <summary>
        /// IP address of that phone #
        /// </summary>
        public string IP;
        /// <summary>
        /// 
        /// </summary>
        public decimal CCFee;

        // Only used for pulling information from the DB into ProcessorSettings.
        public string MerchantNumber
        {
            set
            {
                switch (ClearingPlatform)
                {
                    case "israel-premium":
                        ProcessorSettings["MerchantNumber"] = value;
                        break;
                    case "fis-paydirect":
                        ProcessorSettings["SettleMerchantCode"] = value;
                        break;
                    default:
                        break;
                }
            }
        }

        // Only used for pulling information from DB into ProcessorSettings.
        public string CashierNumber
        {
            set
            {
                switch (ClearingPlatform)
                {
                    case "israel-premium":
                        ProcessorSettings["CashierNumber"] = value;
                        break;
                    default:
                        break;
                }
            }
        }

        public CCProcessorInfo()
        {
            // If this is called then that means that the caller will try to access the properties
            // directly. Instantiate the dictionary.
            ProcessorSettings = new Dictionary<string, string>();
        }

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
            string ip,
            decimal ccfee)
            :this()
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
            this.CCFee = ccfee;
        }
    }
}
