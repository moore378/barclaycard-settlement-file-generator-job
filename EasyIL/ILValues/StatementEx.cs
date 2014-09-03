using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyIL
{
    public static class StatementEx
    {
        public static IStatement Concat(this IStatement stat1, IStatement stat2)
        {
            return new Statement(stat1.Code + stat2.Code);
        }
    }
}
