using Common;
using CryptographicPlatforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    public struct CreditCardPan
    {
        private string panString;
        private ObscurationMethod obscured;

        private static BinRange[] binRanges;

        /// <summary>
        /// Static constructor to initialize resources used for static methods.
        /// </summary>
        static CreditCardPan()
        {
            // Create BIN ranges for the different credit card schemes.
            CreateBinRanges();
        }

        internal CreditCardPan(string panString, ObscurationMethod obscured)
        {
            this.panString = panString;
            this.obscured = obscured;
        }

        /// <summary>
        /// Obscures the PAN using the method provided (e.g hashing it). This can only be called on a currently unobscured PAN.
        /// </summary>
        /// <param name="method"></param>
        /// <returns>The obscured PAN</returns>
        /// <exception cref="InvalidOperationException">The PAN is already obscured</exception>
        /// <exception cref="InvalidDataException">The PAN is not valid</exception>
        public CreditCardPan Obscure(ObscurationMethod method)
        {
            if (obscured != ObscurationMethod.None)
                throw new InvalidOperationException("Cannot re-obscure PAN");

            if (obscured != ObscurationMethod.None)
                throw new InvalidDataException("Invalid PAN to obscure");

            switch (method)
            {
                case ObscurationMethod.None:
                    return new CreditCardPan(panString, method);
                case ObscurationMethod.Hash:
                    return new CreditCardPan(CryptographicPlatforms.CCCrypt.HashPAN(panString), method);
                case ObscurationMethod.FirstSixLastFour:
                    // Get the first six and last four
                    return new CreditCardPan(FirstSixDigits + "..." + LastFourDigits, method);
                default:
                    throw new ArgumentException("Unknown obscuration method \"" + method.ToString() + "\"");
            }

        }

        public enum ObscurationMethod { None, FirstSixLastFour, Hash };

        public string FirstSixDigits
        {
            get
            {
                if (obscured == ObscurationMethod.Hash)
                    throw new InvalidOperationException("Cannot get the first six digits of a hashed PAN.");
                if (panString.Length < 6)
                    throw new InvalidOperationException("Cannot get the first six digits of PAN shorter than 6 digits.");
                if (panString.Length <= 10)
                    return panString.Substring(0, 2);

                return panString.Substring(0, 6);
            }
        }

        public string LastFourDigits
        {
            get
            {
                if (obscured == ObscurationMethod.Hash)
                    throw new InvalidOperationException("Cannot get the last four digits of a hashed PAN.");
                if (panString.Length < 4)
                    throw new InvalidOperationException("Cannot get the last four digits of PAN shorter than 4 digits.");
                return panString.Substring(panString.Length - 4);
            }
        }

        public void Validate(string failStatus)
        {
            // Check that the PAN is the right length
            if ((panString.Length > 19) // The pan cannot be longer than 19 chars
                || (panString.Length < 1)) // There must be a pan
                throw new ValidationException("Invalid PAN length", failStatus);

            // Check that each character in the PAN is numeric
            foreach (char c in panString)
                if ((c < '0') || (c > '9'))
                    throw new ValidationException("Invalid PAN", failStatus);
        }

        public override string ToString()
        {
            return panString;
        }

        /// <summary>
        /// Determine the credit card type for the given PAN.
        /// </summary>
        /// <param name="panString">PAN</param>
        /// <returns>Credit card type</returns>
        public static CreditCardType DetermineCreditCardType(string panString)
        {
            CreditCardType creditCardType = CreditCardType.Unknown;

            // Loop through each card
            foreach (BinRange entry in binRanges)
            {
                // Only when the BIN length matches.
                if (entry.Length == panString.Length)
                {
                    // Verify the starting and ending BIN ranges.
                    if ((0 <= String.Compare(panString, 0, entry.BinStart, 0, entry.BinStart.Length))
                        && (0 >= String.Compare(panString, 0, entry.BinStop, 0, entry.BinStop.Length)))
                    {
                        // On a match, stop looking for more.
                        creditCardType = entry.CreditCardType;
                        break;
                    }
                }
            }

            return creditCardType;
        }

        #region BIN Range Helpers

        private static void CreateBinRanges()
        {
            binRanges = new BinRange[]
            {
                //            BinStart,     BinStop,    Length, CardType
                // Visa
                new BinRange( "4",                      16,     CreditCardType.Visa ),

                // MasterCard
                new BinRange( "51",         "55",       16,     CreditCardType.MasterCard ),    // (classic)
                new BinRange( "222100",     "272099",   16,     CreditCardType.MasterCard ),    // Added 2016

                // Discover
                new BinRange( "6011",                   16,     CreditCardType.Discover ),
                new BinRange( "650",        "659",      16,     CreditCardType.Discover ),
                new BinRange( "644",        "649",      16,     CreditCardType.Discover ),      // (New 2009 range)
                new BinRange( "62212600",   "62292599", 16,     CreditCardType.Discover ),      // (China Union Pay)
                new BinRange( "62400000",   "62699999", 16,     CreditCardType.Discover ),      // (China Union Pay 2009)
                new BinRange( "62820000",   "62889999", 16,     CreditCardType.Discover ),      // (China Union Pay 2009)
                new BinRange( "36",                     14,     CreditCardType.Discover ),      // (formerly Mastercard Diners) 10/16/2009
                new BinRange( "300",        "305",      16,     CreditCardType.Discover ),      // (formerly Diners Intl) 10/16/2009
                new BinRange( "3095",       "3095",     16,     CreditCardType.Discover ),      // (formerly Diners Intl) 10/16/2009
                new BinRange( "380",        "399",      16,     CreditCardType.Discover ),      // (formerly Diners Intl) 10/16/2009
                new BinRange( "3528",       "3589",     16,     CreditCardType.Discover ),      // (formerly JCB Intl)    10/16/2009

                // Amex
                new BinRange( "37",                 15,     CreditCardType.AmericanExpress ),
                new BinRange( "34",                 15,     CreditCardType.AmericanExpress )
            };
        }

        /// <summary>
        /// BIN Range identifier
        /// </summary>
        internal struct BinRange
        {
            public string BinStart { get; set; }

            public string BinStop { get; set; }

            public int Length { get; set; }

            public CreditCardType CreditCardType { get; set; }

            /// <summary>
            /// Constructor that accepts only a single BIN number and not a range.
            /// </summary>
            /// <param name="binStart">BIN</param>
            /// <param name="length">BIN length</param>
            /// <param name="cardType">Credit card type</param>
            /// <remarks>the ending bin will be equal to the starting bin</remarks>
            public BinRange(string binStart, int length, CreditCardType creditCardType)
                : this(binStart, binStart, length, creditCardType)
            {
                // Nothing on purpose.
            }

            /// <summary>
            /// Constructor that accepts a bin range.
            /// </summary>
            /// <param name="binStart">Starting BIN</param>
            /// <param name="binStop">Ending BIN</param>
            /// <param name="length">BIN length</param>
            /// <param name="creditCardType">Credit card type</param>
            public BinRange(string binStart, string binStop, int length, CreditCardType creditCardType)
                : this()
            {
                BinStart = binStart;
                BinStop = binStop;
                Length = length;
                CreditCardType = creditCardType;
            }
        }

        #endregion
    }
}
