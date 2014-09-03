using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Logging
{
    public interface ITreeLog : IDisposable
    {
        [DebuggerNonUserCode]
        void Log(string msg);
        [DebuggerNonUserCode]
        ITreeLog CreateChild(string childName);
    }
}
