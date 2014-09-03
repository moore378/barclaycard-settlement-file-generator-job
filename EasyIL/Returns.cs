using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public class Returns : CustomSequence
    {
        public Returns(Type returnType, Action<ILGenerator> generate, string statementStr)
            : base(generate, statementStr, null, returnType)
        {

        }
    }
}
