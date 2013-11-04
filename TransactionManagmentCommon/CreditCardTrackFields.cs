using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    public struct CreditCardTrackFields
    {
        public CreditCardPan Pan; // Unencrypted primary account number
        public string ExpDateYYMM; // expiry date
        public string ServiceCode;

        public string ExpDateMMYY { get { return ExpDateYYMM.Substring(2, 2) + ExpDateYYMM.Substring(0, 2); } }

        /// <exception cref="ValidationException">
        /// Thrown if the fields are not all valid. The exceptions "FailStatus" is set to that provided.
        /// </exception>
        public void Validate(string failStatus)
        {
            try
            {
                Pan.Validate(failStatus);

                // Check that the expiry date is there
                if (ExpDateYYMM.Length != 4)
                    throw new ValidationException("Unknown expiry date", failStatus);

                // Check that the expiry date is numeric
                foreach (char c in ExpDateYYMM)
                    if ((c < '0') || (c > '9'))
                        throw new ValidationException("Invalid expiry date", failStatus);

                // Check that the expiry date has valid ranges
                int month = Int32.Parse(ExpDateMMYY.Substring(0, 2));
                if ((month < 1) || (month > 12))
                    throw new ValidationException("Invalid month in expiry date", failStatus);

            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ValidationException("Error validating track fields", failStatus, e);
            }
            catch (ArgumentException e)
            {
                throw new ValidationException("Error validating track fields", failStatus, e);
            }
            catch (FormatException e)
            {
                throw new ValidationException("Error validating track fields", failStatus, e);
            }
            catch (OverflowException e)
            {
                throw new ValidationException("Error validating track fields", failStatus, e);
            }
        }

        public CardType CardType;
    }
}
