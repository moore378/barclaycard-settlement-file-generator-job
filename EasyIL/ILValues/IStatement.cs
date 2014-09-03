using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public interface IStatement
    {
        ILSeq Code { get; }
    }
}
