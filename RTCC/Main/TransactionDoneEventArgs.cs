using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rtcc.Main
{
    public class TransactionDoneEventArgs : EventArgs
    {
        public TransactionDoneEventArgs(bool success)
        {
            this.Success = success;
        }

        public bool Success { get; private set; }
    }
}
