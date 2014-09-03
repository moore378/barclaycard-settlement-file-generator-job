using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AutoDatabase
{
    class DebugContext
    {
        private string contextName;
        private DebugContext parentContext;

        [DebuggerNonUserCode]
        public DebugContext(string contextName)
        {
            this.contextName = contextName;
        }

        [DebuggerNonUserCode]
        private DebugContext(DebugContext parentContext, string contextName)
            : this (contextName)
        {
            this.parentContext = parentContext;
        }

        [DebuggerNonUserCode]
        public static DebugContext operator +(DebugContext parent, string newContext)
        {
            return new DebugContext(parent, newContext);
        }

        public override string ToString()
        {
            if (parentContext == null)
                return contextName;
            else
                return parentContext.ToString() + "." + contextName;
        }
    }
}
