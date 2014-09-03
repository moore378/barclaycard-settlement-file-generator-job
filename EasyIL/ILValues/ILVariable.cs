using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public class ILVariable : IRWValue
    {
        private LocalBuilder local;

        public ILVariable(LocalBuilder local)
        {
            this.local = local;
        }

        public ILSeq WritePreCalc
        {
            get { return ILSeq.Empty; }
        }

        public ILSeq WritePostCalc
        {
            get { return ILSeq.Stloc(local); }
        }

        public ILSeq Read
        {
            get { return ILSeq.Ldloc(local); }
        }

        public Type ValueType
        {
            get { return local.LocalType; }
        }
    }

    public class ILVariable<T> : ILVariable, IRWValue<T>
    {
        public ILVariable(LocalBuilder local)
            : base(local)
        {
            if (local.LocalType != typeof(T))
                throw new ArgumentException("local");
        }
    }
}
