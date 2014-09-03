using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace EasyIL
{
    public class RValue : Value, IRValue
    {
        private ILSeq read;
        
        public RValue(Type valueType, ILSeq read)
            : base (valueType)
        {
            this.read = read;

            if (read.Pops.Count() != 0)
            {
                throw new InvalidOperationException("Stack imbalance\n" +
                    read.ToString().Replace("; ", ";\n"));
            }
            if (read.Pushes.Count() > 1)
            {
                throw new InvalidOperationException("Stack imbalance\n" +
                    "read operation \"" + read + "\" pushes " + read.Pushes.Count() + " values onto the stack:\n" +
                    read.Pushes.Select(t => t.ToString()).Aggregate((a, s) => a + ", " + s));
            }
            if (read.Pushes.Count() < 1)
            {
                throw new InvalidOperationException("Stack imbalance\n" +
                    "read operation \"" + read + "\" pushes no values onto the stack");
            }

            if (!valueType.IsILAssignableFrom(read.Pushes.Single()))
                throw new InvalidOperationException("Type mismatch - expected " + valueType.Name + " but got " + read.Pushes.Single().Name);
        }

        public RValue(Type valueType, Action<ILGenerator> read, string statementStr)
            : this(valueType, new CustomSequence(read, statementStr, new Type[] {}, valueType))
        {
            
        }

        public ILSeq Read { get { return read; } }
    }

    public class RValue<T> : RValue, IRValue<T>
    {
        public RValue(ILSeq read)
            : base (typeof(T), read)
        {
        }
    }
}
