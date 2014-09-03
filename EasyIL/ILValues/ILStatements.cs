using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyIL
{
    public class ILStatements : IStatement
    {
        private List<IStatement> code = new List<IStatement>();

        public void Do(Statement statement)
        {
            code.Add(statement);
        }

        public ILSeq Code
        {
            get { return code.Select(s => s.Code).Aggregate((a, s) => a + s); }
        }
    }
}
