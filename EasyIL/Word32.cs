using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyIL
{
    /// <summary>
    /// This is a dummy type representing a common ansestor (super-type) to an object ref and an integer. 
    /// For example Ceq takes Word32 arguments, but (+) only takes integer arguments.
    /// </summary>
    public sealed class Word32 { }
}
