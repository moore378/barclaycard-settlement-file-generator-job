using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace TransactionManagementCommon
{
    public static class TrackFormat
    {
        /// <summary>
        /// Given a stripe with track two in a known location, this function will insert the 
        /// beginning and end sentinels for track two. The beginning sentinel will be placed
        /// just before the index specified by trackTwoStart. The end sentinel will be placed
        /// at the first null character after the start.
        /// </summary>
        /// <param name="unencryptedStripe"></param>
        /// <param name="trackTwoStart">The index of the first character in track two</param>
        /// <returns>The stripe with sentinels inserted</returns>
        public static UnencryptedStripe InsertSentinels(UnencryptedStripe unencryptedStripe, int trackTwoStart)
        {
            if ((trackTwoStart < 2) || (trackTwoStart > unencryptedStripe.Data.Length - 1))
                throw new ArgumentOutOfRangeException("Cannot insert sentinels, " + trackTwoStart.ToString() + " is not a valid track start");

            //if (unencryptedStripe.Data.Length != 128)
            //    throw new ArgumentException("Invalid stripe");

            string result = unencryptedStripe.Data;

            // Remove invalid symbols
            for (int i = 0; i < result.Length; i++)
                if (result[i] == ';'){
                    result.Remove(i, 1);
                    result.Insert(i, "\0");
                }
            for (int i = 0; i < result.Length; i++)
                if (result[i] == '?')
                {
                    result.Remove(i, 1);
                    result.Insert(i, "\0");
                }

            // Start sentinel
            result = result.Remove(trackTwoStart - 1, 1);
            result = result.Insert(trackTwoStart - 1, ";");

            // End sentinel
            int firstNull = result.IndexOf('\0', trackTwoStart);

            if (firstNull < 0)
                throw new ArgumentException("Invalid track - No place for end sentinel");

            result = result.Remove(firstNull, 1);
            result = result.Insert(firstNull, "?");

            return new UnencryptedStripe(result);
        }

        public static FormattedStripe FormatSpecialStripeCases(UnencryptedStripe unencryptedStripe, EncryptionMethod encryptionMethod, string failStatus)
        {
            try
            {
                switch (encryptionMethod)
                {
                    case EncryptionMethod.StandardFormat:
                        return new FormattedStripe(unencryptedStripe.Data);

                    case EncryptionMethod.Unencrypted:
                        // Insert sentinels
                        return new FormattedStripe(TrackFormat.InsertSentinels(unencryptedStripe.Data, 88));

                    case EncryptionMethod.RsaEncryption:
                        // Shift track two to the right by 10 chars
                        string shiftedtrack = ShiftTrack(unencryptedStripe.Data, 78, 10);
                        // Insert sentinels
                        return new FormattedStripe(TrackFormat.InsertSentinels(shiftedtrack, 88));

                    case EncryptionMethod.IpsEncryption:
                        // Insert sentinels
                        return new FormattedStripe(TrackFormat.InsertSentinels(unencryptedStripe.Data, 88));

                    case EncryptionMethod.IpsEncryption_withSentinels:
                        return new FormattedStripe(unencryptedStripe.Data);
                        
                    case EncryptionMethod.RsaEncryption_withSentinels:
                        return new FormattedStripe(unencryptedStripe.Data);

                    default:
                        throw new StripeErrorException("Unknown encryption method: " + encryptionMethod.ToString(), failStatus);
                }
            }
            catch (StripeErrorException) { throw; }
            catch (Exception e)
            {
                throw new StripeErrorException("Error formatting stripe", failStatus, e);
            }
        }

        /// <summary>
        /// This shifts the right part of the string to the right by "shiftBy" chars, padding 
        /// the left with null chars and removing the chars at the right in order to keep the
        /// length the same
        /// </summary>
        /// <param name="unencryptedStripe"></param>
        /// <param name="trackStart">Current start of the track. The new start will be (trackStart + shiftBy)</param>
        /// <param name="shiftBy">Positive integer specifying the amount to shift the track by</param>
        /// <returns>The stripe containing the shifted track</returns>
        private static string ShiftTrack(string unencryptedStripe, int trackStart, uint shiftBy)
        {
            //if (unencryptedStripe.Length != 128)
            //    throw new ArgumentException("Invalid stripe");

            if (trackStart < 0)
                throw new ArgumentOutOfRangeException("Cannot shift track, " + trackStart.ToString() + " is not within stripe");

            if (trackStart + shiftBy > unencryptedStripe.Length - 1)
                throw new ArgumentOutOfRangeException("Cannot shift track beyond end of stripe");

            string result = unencryptedStripe;

            // Insert nulls
            for (int i = 0; i < shiftBy; i++)
                result = result.Insert(trackStart, "\0");

            // Chop off the end
            if (result.Length > 128)
                return result.Remove(128);
            else
                return result;
        }


        public static CreditCardTrackFields ParseTrackTwoIso7813(CreditCardTrack TrackTwo, string failStatus)
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

                // Extract the card type
                if (result.Pan.ToString().Length < 4)
                    result.CardType = CardType.Unknown;

                string panFirstFour = result.Pan.ToString().Substring(0, 4);
                char panPrefixDigit = panFirstFour[0];

                result.CardType = CardType.Unknown;

                // It seems from Malan's code that a normal card has a first digit in the range '2' to '7'
                if ((panPrefixDigit >= '2') && (panPrefixDigit <= '7'))
                    result.CardType = CardType.Normal;

                if (panPrefixDigit == '1')
                {
                    switch (panFirstFour)
                    {
                        case "1010": result.CardType = CardType.Maintenance; break;
                        case "1011": result.CardType = CardType.CoinCollector; break;
                        case "1100": result.CardType = CardType.Special; break;
                        case "1111": result.CardType = CardType.Diagnostic; break;
                        default: result.CardType = CardType.SpecialUndefined; break;
                    }
                }


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

        public static bool ParseTrackTwoIsraelSpecial(CreditCardTrack trackTwo, string failStatus, out CreditCardTrackFields fields)
        {
            fields = new CreditCardTrackFields();

            string trackTwoString = trackTwo.ToString();

            string firstPart = Spanning(trackTwoString, ';', '=') ?? "";
            if (firstPart.Length <= 18)
                return false;

            string secondPart = Spanning(trackTwoString, '=', '?') ?? "";

            if (secondPart.Length != 21)
                return false;

            // Zero at end means 8 digit card
            if (secondPart[19] == '0')
                fields.Pan = new CreditCardPan(secondPart.Substring(2, 8), CreditCardPan.ObscurationMethod.None);
            else // 9 digit card
                fields.Pan = new CreditCardPan(secondPart[19] + secondPart.Substring(2, 8), CreditCardPan.ObscurationMethod.None);

            fields.ExpDateYYMM = secondPart.Substring(15, 4);
            fields.CardType = CardType.IsraelSpecial;

            fields.ServiceCode = "";

            fields.Validate(failStatus);

            return true;
        }

        /// <summary>
        /// Copies the string between startSentinal and endSentinal, or returns null
        /// </summary>
        /// <param name="track"></param>
        /// <param name="startSentinal"></param>
        /// <param name="endSentinal"></param>
        /// <returns></returns>
        public static string Spanning(string track, char startSentinal, char endSentinal)
        {
            int startIndex = track.IndexOf(startSentinal);
            if (startIndex == -1)
                return null;
            int endIndex = track.IndexOf(endSentinal, startIndex);
            if (endIndex == -1)
                return null;
            return track.Substring(startIndex, endIndex - startIndex + 1);
        }
    }
}
