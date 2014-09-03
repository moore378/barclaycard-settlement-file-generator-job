using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public class Consumes : CustomSequence
    {
        public Consumes(Type consumesType, Action<ILGenerator> generate, string statementStr)
            : base(generate, statementStr, new Type[] { consumesType })
        {

        }
    }
}
