using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Rtcc.RtsaInterfacing
{
    /// <summary>
    /// Encapsulates a data stream
    /// </summary>
    public struct RawDataMessage
    {
        public Stream Data;
        public RawDataMessage(Stream data)
        {
            this.Data = data;
        }
    }
}
