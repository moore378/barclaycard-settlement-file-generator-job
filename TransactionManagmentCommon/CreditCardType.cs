using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MjhGeneral;

using System.ComponentModel;

namespace TransactionManagementCommon
{
    public enum CreditCardType
    {
        [Description("Unknown")]
        Unknown,

        [Description("Visa")]
        Visa,

        [Description("Master")]
        MasterCard,

        [Description("AMEX")]
        AmericanExpress,

        [Description("Discover")]
        Discover,

        [Description("IsraCard")]
        Isracard,

        [Description("PL")]
        PL
    }
}
