using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    /// <summary>
    /// Different types of cards, rated according to the first four digits of the PAN. These constants are taken from Malan's code in the first versions of the CCTM.
    /// </summary>
    public enum CardType
    {
        /// <summary>
        /// Card type not defined
        /// </summary>
        Unknown,
        /// <summary>
        /// Normal card
        /// </summary>
        Normal,
        /// <summary>
        /// 1010
        /// </summary>
        Maintenance,
        /// <summary>
        /// 1011
        /// </summary>
        CoinCollector,
        /// <summary>
        /// 1100
        /// </summary>
        Special,
        /// <summary>
        /// 1111
        /// </summary>
        Diagnostic,
        /// <summary>
        /// 1???
        /// </summary>
        SpecialUndefined,

        IsraelSpecial
    }
}
