using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    public enum PreauthStatus
    {
        New = 3,
        Processing = 5,
        Preauthorizing = 6,
        Approved = 7,
        Declined = 8,
        StripeError = 9,
        AuthError = 10,
    }
}
