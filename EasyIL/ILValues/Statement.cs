using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public class Statement : IStatement
    {
        private ILSeq code;

        public Statement(ILSeq seq)
        {
            if (seq.Pushes.Count() != 0 || seq.Pops.Count() != 0)
                throw new ArgumentException("Statement cannot push or pop from IL stack");
            this.code = seq;
        }

        public ILSeq Code
        {
            get { return code;  }
        }
    }
}
