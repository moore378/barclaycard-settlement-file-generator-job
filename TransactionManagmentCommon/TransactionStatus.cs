using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    public enum TransactionStatus
    {
        AwaitingFinalizationInfo = 1,
        AwaitingPreauth = 2,
        ReadyToProcessNew = 3,
        PreauthFailed = 4,
        Processing = 5,
        Authorizing = 6,
        Approved = 7,
        Declined = 8,
        StripeError = 9,
        AuthError = 10,
    }

    public static class TransactionStatusEx
    {
        public static string ToText(this TransactionStatus status)
        {
            string statusString;
            switch (status)
            {
                case TransactionStatus.Approved: statusString = "Approved"; break;
                case TransactionStatus.AuthError: statusString = "Authorization error"; break;
                case TransactionStatus.Authorizing: statusString = "Authorizing"; break;
                case TransactionStatus.AwaitingFinalizationInfo: statusString = "Awaiting finalization info"; break;
                case TransactionStatus.AwaitingPreauth: statusString = "Awaiting preauthorization"; break;
                case TransactionStatus.Declined: statusString = "Declined"; break;
                case TransactionStatus.PreauthFailed: statusString = "Preauthorization failed"; break;
                case TransactionStatus.Processing: statusString = "Processing"; break;
                case TransactionStatus.ReadyToProcessNew: statusString = "New"; break;
                case TransactionStatus.StripeError: statusString = "Stripe error"; break;
                default: statusString = "Unknown"; break;

            }
            return statusString;// +" (" + ((int)status).ToString() + ")";
        }
    }
}
