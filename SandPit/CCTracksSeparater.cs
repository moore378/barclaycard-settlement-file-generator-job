using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GiveMeAName //TODO: Put this in a namespace
{
    static class CCTracksSeparater
    {
        /// <summary>
        /// Splits a credit card magnetic stripe into 2 tracks
        /// </summary>
        /// <param name="stripe">Magnetic stripe</param>
        /// <returns>Track one and two</returns>
        public static Tuple<string, string> SplitIntoTracks(string stripe)
        {
            // Search for the start sentinel of the second track
            int secondTrackStart = stripe.IndexOf(';');
            if ((secondTrackStart == -1) // The start sentinal must be there
                || (secondTrackStart > stripe.Length - 1)) // The start sentinal must not be at the end
                throw new ParseException("Error parsing stripe, could not find start sentinal.");

            // Remove track 1
            string trackOne = stripe.Substring(0, secondTrackStart - 1);
            string fromTrack2 = stripe.Substring(secondTrackStart); // Track 2 til end

            // Search for the end sentinel of the second track
            int track2End = fromTrack2.IndexOf('?');
            if (track2End == -1)
                throw new ParseException("Error parsing stripe, could not find end sentinal.");

            // Separate tracks
            string trackTwo = fromTrack2.Substring(0, track2End + 1);

            return Tuple.Create(trackOne, trackTwo);
        }

        /// <summary>
        /// Splits track two into fields
        /// </summary>
        public static CreditCardTrackFields ParseTrackTwo(string trackTwo)
        {
            // Assert that the first character is the start sentinal
            if (trackTwo.IndexOf(';') != 0)
                throw new ParseException("Error parsing track two, could not find start sentinal.");
            // The pan starts just after this
            string fromPan = trackTwo.Substring(1);

            // Search for separator
            int indexOfSeparator = fromPan.IndexOf('=');
            if ((indexOfSeparator == -1) // The separator must be there
                || (indexOfSeparator > fromPan.Length - 9)) // The separator must be at least 9 characters from the end
                throw new ParseException("Error parsing track, could not find separator symbol.");

            // Check that the end sentinal at the end
            if (fromPan.IndexOf('?') == -1)
                throw new ParseException("Error parsing track, could not find termination sentinal.");

            CreditCardTrackFields result = new CreditCardTrackFields();

            // Extract the PAN
            result.Pan = fromPan.Substring(0, indexOfSeparator);
            // Extract expiry date
            result.ExpDateYYMM = fromPan.Substring(indexOfSeparator + 1, 4);
            // Extract service code
            result.ServiceCode = fromPan.Substring(indexOfSeparator + 5, 3);

            return result;
        }
    }

    class CreditCardTrackFields
    {
        public string Pan { get; set; }
        public string ExpDateYYMM { get; set; }
        public string ServiceCode { get; set; }
    }

    class ParseException : Exception
    {
        public ParseException(string message) : base(message) {  }
    }
}
