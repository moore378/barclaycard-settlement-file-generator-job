using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CryptographicPlatforms;
using Common;

namespace TransactionManagementCommon
{
    /// <summary>
    /// Decrypts a stripe using a given encryption method
    /// </summary>
    public class StripeDecryptor : Object
    {
        /// <summary>
        /// Decrypt given track data
        /// </summary>
        /// <param name="encryptedTrack">Encrypted track data</param>
        /// <returns>Returns an UnencryptedTracks if successful,
        /// and UnencryptedTracks.nullTracks() if unsuccessful.</returns>
        /// <exception cref="StripeErrorException"></exception>
        public UnencryptedStripe decryptStripe(EncryptedStripe encryptedTrack, EncryptionMethod encryptionVersion, int keyVersion, TransactionInfo info, string errorStatus)
        {
            switch (encryptionVersion)
            {
                case EncryptionMethod.Unencrypted:
                    try
                    {
                        // Convert directly from Ascii
                        System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
                        return new UnencryptedStripe(encoder.GetString(encryptedTrack.Data));
                    }
                    catch (Exception exception)
                    {
                        throw new StripeErrorException("Error using plain-text stripe", errorStatus, exception);
                    }

                case EncryptionMethod.IpsEncryption:
                    try
                    {
                        return CryptographicPlatforms.IPSTrackCipher.Decrypt(encryptedTrack, info.RefDateTime??info.StartDateTime,
                            info.TransactionIndex, Int32.Parse(info.MeterSerialNumber),
                            keyVersion);
                    }
                    catch (Exception exception)
                    {
                        throw new StripeErrorException("Error decrypting IPS-encrypted stripe: " + exception.Message, errorStatus, exception);
                    }

                case EncryptionMethod.RsaEncryption:
                    try
                    {
                        return CryptographicPlatforms.RsaCipher.Decrypt(encryptedTrack, keyVersion);
                    }
                    catch (Exception exception)
                    {
                        throw new StripeErrorException("Error decrypting RSA-encrypted stripe", errorStatus, exception);
                    }

                case EncryptionMethod.StandardFormat:
                    try
                    {
                        // Convert directly from Ascii
                        System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
                        return new UnencryptedStripe(encoder.GetString(encryptedTrack.Data));
                    }
                    catch (Exception exception)
                    {
                        throw new StripeErrorException("Error using plain-text stripe", errorStatus, exception);
                    }

                default:
                    throw new StripeErrorException("Unknown encryption method", errorStatus);
            }
        }
    }

    /// <summary>
    /// Decrypted, decoded credit card stripe data
    /// </summary>
    public struct CreditCardStripe : IValidatable
    {
        private string data;
        public string Data { get { return data; } }

        public override string ToString()
        {
            return data.ToString();
        }
        public CreditCardStripe(FormattedStripe data)
        {
            this.data = data;
        }

        /// <summary>
        /// This will check that the data look feasable from a Stripe level. 
        /// </summary>
        /// <param name="failStatus">If FailStatus to used for the thrown ValidationException if the Stripe is invalid.</param>
        /// <exception cref="ValidationException">
        /// Thrown if the stripe is not valid. The exceptions "FailStatus" is set to that provided.
        /// </exception>
        public void Validate(string failStatus)
        {
            if ((data.Length > 128) || (data.Length <= 0))
                throw new ValidationException("Invalid stripe data length", failStatus);
        }

        public CreditCardTracks SplitIntoTracks(string failStatus)
        {
            try
            {
                // Search for the start sentinel of the second track
                int secondTrackStart = data.IndexOf(';');
                if ((secondTrackStart == -1) // The start sentinal must be there
                    || (secondTrackStart > data.Length - 1)) // The start sentinal must not be at the end
                    throw new ParseException("Error parsing stripe, could not find start sentinal.", failStatus);

                // Remove track 1
                CreditCardTrack trackOne = new CreditCardTrack(data.Substring(0, secondTrackStart - 1));
                string fromTrack2 = data.Substring(secondTrackStart);


                // Search for the end sentinel of the second track
                int track2End = fromTrack2.IndexOf('?');
                if (track2End == -1)
                    throw new ParseException("Error parsing stripe, could not find end sentinal.", failStatus);

                // Separate tracks
                CreditCardTrack trackTwo = new CreditCardTrack(fromTrack2.Substring(0, track2End + 1));

                return new CreditCardTracks(trackOne, trackTwo);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ParseException("Error parsing stripe", failStatus, e);
            }
            catch (ArgumentException e)
            {
                throw new ParseException("Error parsing stripe", failStatus, e);
            }
        }
    }

    /// <summary>
    /// Unencrypted credit card track
    /// </summary>
    public struct CreditCardTrack
    {
        private string track;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="track">String to be contained in the track</param>
        public CreditCardTrack(string track)
        {
            this.track = track;
        }

        public override string ToString()
        {
            return track;
        }
    }

    public struct CreditCardPan : IValidatable
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

        #region IValidatable Members

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

        #endregion
    }

    public struct CreditCardTracks : IValidatable
    {
        public CreditCardTrack TrackOne;
        public CreditCardTrack TrackTwo;

        public CreditCardTracks(
            CreditCardTrack trackOne,
            CreditCardTrack trackTwo)
        {
            this.TrackTwo = trackTwo;
            this.TrackOne = trackOne;
        }

        public CreditCardTrackFields ParseTrackTwo(string failStatus)
        {
            try
            {
                CreditCardTrackFields result = new CreditCardTrackFields();

                string trackTwoString = TrackTwo.ToString();

                // Assert that the first character is the start sentinal
                if (trackTwoString.IndexOf(';') != 0)
                    throw new ParseException("Error parsing track two, could not find start sentinal.", failStatus);
                // The pan starts just after this
                string fromPan = trackTwoString.Substring(1);

                // Search for separator
                int indexOfSeparator = fromPan.IndexOf('=');
                if ((indexOfSeparator == -1) // The separator must be there
                    || (indexOfSeparator > fromPan.Length - 9)) // The separator must be at least 9 characters from the end
                    throw new ParseException("Error parsing track, could not find separator symbol.", failStatus);

                // Extract the PAN
                result.Pan = new CreditCardPan(fromPan.Substring(0, indexOfSeparator), CreditCardPan.ObscurationMethod.None);
                // Extract expiry date
                result.ExpDateYYMM = fromPan.Substring(indexOfSeparator + 1, 4);
                // Extract service code
                result.ServiceCode = fromPan.Substring(indexOfSeparator + 5, 3);

                // Check that the end sentinal at the end
                if (fromPan.IndexOf('?') == -1)
                    throw new ParseException("Error parsing track, could not find termination sentinal.", failStatus);

                return result;
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ParseException("Error parsing track", failStatus, e);
            }
            catch (ArgumentException e)
            {
                throw new ParseException("Error parsing track", failStatus, e);
            }
        }

        /// <exception cref="ValidationException">
        /// Thrown if the tracks are not valid. The exceptions "FailStatus" is set to that provided.
        /// </exception>
        public void Validate(string failStatus)
        {
            string trackTwoString = TrackTwo.ToString();

            if (trackTwoString.IndexOf(';') != 0)
                throw new ValidationException("Could not find start sentinal.", failStatus);
            if (trackTwoString.IndexOf('=') == -1)
                throw new ValidationException("Could not find field separator symbol.", failStatus);
            if (trackTwoString.IndexOf('?') != trackTwoString.Length - 1)
                throw new ValidationException("Could not find end sentinal.", failStatus);

        }
    }

    public struct CreditCardTrackFields : IValidatable
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

        public CardType CardType
        {
            get
            {
                if (Pan.ToString().Length < 4)
                    return CardType.Unknown;

                string panFirstFour = Pan.ToString().Substring(0, 4);
                char panPrefixDigit = panFirstFour[0];

                // It seems from Malan's code that a normal card has a first digit in the range '2' to '7'
                if ((panPrefixDigit >= '2') && (panPrefixDigit <= '7'))
                    return CardType.Normal;

                if (panPrefixDigit == '1')
                {
                    switch (panFirstFour)
                    {
                        case "1010": return CardType.Maintenance;
                        case "1011": return CardType.CoinCollector;
                        case "1100": return CardType.Special;
                        case "1111": return CardType.Diagnostic;
                        default: return CardType.SpecialUndefined;
                    }
                }

                // If all else fails, then we have no idea what card it is
                return CardType.Unknown;
            }
        }
    }

    
    public interface IValidatable
    {
        void Validate(string failStatus);
    }

    /// <summary>
    /// This class holds information about the transaction being processed, 
    /// and can be passed to functions which need it.
    /// </summary>
    public class TransactionInfo
    {
        public DateTime StartDateTime;
        public int TransactionIndex;
        public string MeterSerialNumber;
        public decimal AmountDollars;
        public DateTime? RefDateTime;

        public TransactionInfo(DateTime startDateTime,
            int transactionIndex,
            string meterSerialNumber,
            decimal amountDollars,
            DateTime? refDateTime)
        {
            this.StartDateTime = startDateTime;
            this.TransactionIndex = transactionIndex;
            this.MeterSerialNumber = meterSerialNumber;
            this.AmountDollars = amountDollars;
            this.RefDateTime = refDateTime;
        }
    }

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
        SpecialUndefined
    }
}
