using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    /// <summary>
    /// Decrypted, decoded credit card stripe data
    /// </summary>
    public struct CreditCardStripe 
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
}
