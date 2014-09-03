using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyIL
{
    public class Value : IValue
    {
        private Type valueType;

        public Value(Type valueType)
        {
            this.valueType = valueType;
        }
        
        public Type ValueType
        {
            get { return valueType; }
        }
    }
}
