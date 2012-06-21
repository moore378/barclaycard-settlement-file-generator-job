using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cctm.Common
{
    [Serializable]
    /// <summary>
    /// This exception class is thrown when the card is not a normal card (an exceptional circumstance), but is rather a maintenance card or other.
    /// </summary>
    public class SpecialCardException : Exception
    {
        public SpecialCardException()
            : base()
        {
        }

        public SpecialCardException(string message)
            : base(message)
        {
        }

        public SpecialCardException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
