using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyIL
{
    public class WValue : Value, IWValue
    {
        private ILSeq writePreCalc;
        private ILSeq writePostCalc;

        public WValue(Type valueType, ILSeq writePreCalc, ILSeq writePostCalc)
            : base (valueType)
        {
            this.writePreCalc = writePreCalc;
            this.writePostCalc = writePostCalc;

            if (writePreCalc.Pops.Count() != 0 || writePostCalc.Pushes.Count() != 0)
                throw new InvalidOperationException("Stack imbalance");
            if (writePostCalc.Pops.Count() - writePreCalc.Pushes.Count() != 1)
                throw new InvalidOperationException("Stack imbalance");
            if (writePostCalc.Pops.First() != valueType)
                throw new InvalidOperationException("Type mismatch");
        }

        public ILSeq WritePreCalc
        {
            get { return writePreCalc; }
        }

        public ILSeq WritePostCalc
        {
            get { return writePostCalc; }
        }
    }

    public class WValue<T> : WValue, IWValue<T>
    {
        public WValue(ILSeq writePreCalc, ILSeq writePostCalc)
            : base(typeof(T), writePreCalc, writePostCalc)
        {
        }
    }
}
