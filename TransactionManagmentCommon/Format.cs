using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace TransactionManagementCommon
{
    public class TrackFormat
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
    }
}
