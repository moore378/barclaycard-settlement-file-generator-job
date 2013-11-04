using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    public struct CreditCardTracks 
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
