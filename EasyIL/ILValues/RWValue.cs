using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyIL
{
    public class RWValue : Value, IRWValue
    {
        private ILSeq read;
        private ILSeq writePreCalc;
        private ILSeq writePostCalc;

        public RWValue(Type valueType, ILSeq read, ILSeq writePreCalc, ILSeq writePostCalc)
            : base(valueType)
        {
            this.read = read;
            this.writePreCalc = writePreCalc;
            this.writePostCalc = writePostCalc;

            if (read.Pops.Count() != 0 || read.Pushes.Count() != 1)
                throw new InvalidOperationException("Stack imbalance");
            if (read.Pushes.Single() != valueType)
                throw new InvalidOperationException("Type mismatch");
            if (writePreCalc.Pops.Count() != 0 || writePostCalc.Pushes.Count() != 0)
                throw new InvalidOperationException("Stack imbalance");
            if (writePostCalc.Pops.Count() - writePreCalc.Pushes.Count() != 1)
                throw new InvalidOperationException("Stack imbalance");
        }

        public ILSeq WritePreCalc
        {
            get { return writePreCalc; }
        }

        public ILSeq WritePostCalc
        {
            get { return writePostCalc; }
        }

        public ILSeq Read
        {
            get { return read; }
        }
    }

    public class RWValue<T> : RWValue, IRWValue<T>
    {
        public RWValue(ILSeq read, ILSeq writePreCalc, ILSeq writePostCalc)
            : base(typeof(T), read, writePreCalc, writePostCalc)
        {

        }
    }
}
