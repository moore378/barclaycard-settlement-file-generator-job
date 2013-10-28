using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
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
}
