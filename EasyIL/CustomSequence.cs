using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public class CustomSequence : ILSeq
    {
        private Action<ILGenerator> generate;
        private Type[] pops;
        private Type[] pushes;
        private string statementStr;

        public CustomSequence(Action<ILGenerator> generate, string statementStr, Type[] inputs, params Type[] outputs)
        {
            this.statementStr = statementStr;
            this.generate = generate;
            if (inputs == null)
                this.pops = new Type[] { };
            else
                this.pops = inputs;
            this.pushes = outputs;
        }

        public override string StatementStr { get { return statementStr; } }

        public override IEnumerable<Type> Pops { get { return pops; } }

        public override IEnumerable<Type> Pushes { get { return pushes; } }

        protected override void Generate(ILGenerator ilGenerator)
        {
            generate(ilGenerator);
        }
    }
}
