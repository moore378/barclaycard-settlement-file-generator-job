using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
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
}
