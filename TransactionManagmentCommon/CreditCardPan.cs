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
                    return new CreditCardPan(CryptographicPlatforms.CreditCardHashing.HashPAN(panString), method);
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
                if (panString.Length <= 8)
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
    }
}
