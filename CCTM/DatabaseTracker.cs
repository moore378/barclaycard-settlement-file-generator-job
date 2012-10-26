using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoDatabase;

namespace Cctm
{
    class DatabaseTracker : IDatabaseTracker
    {
        private Action<string> log;
        private bool logArgs;

        public DatabaseTracker(Action<string> log, bool logArgs)
        {
            this.log = log;
            this.logArgs = logArgs;
        }

        public IDatabaseMethodTracker StartingQuery(string name, params object[] args)
        {
            if (logArgs)
                log("-> " + name + "(" + args.Select(ArgValue).JoinStr(", ") +")");
            else
                log("-> " + name);
            return new MethodTracker(log, name, logArgs);
        }

        private static string ArgValue(object arg)
        {
            if (arg == null)
                return "null";
            Type argType = arg.GetType();
            if (arg is string)
                return "\"" + (string)arg + "\"";
            else if (arg is IEnumerable<object>)
                return "[" + ((IEnumerable<object>)arg).Select(ArgValue).JoinStr("; ") + "]";
            else if (argType.IsClass && !argType.IsPrimitive)
                return "{" + ObjectEx.FieldsAndProps(arg).Select(k => k.Key + "=" + ArgValue(k.Value)).JoinStr(";") + "}";
            else
                return arg.ToString();
        }

        private class MethodTracker : IDatabaseMethodTracker
        {
            private Action<string> log;
            private string name;
            private bool logArgs;

            public MethodTracker(Action<string> log, string name, bool logArgs)
            {
                this.log = log;
                this.name = name;
                this.logArgs = logArgs;
            }

            public void Successful(object result)
            {
                if (!logArgs || result == null || result.GetType() == typeof(object))
                    log("<- " + name);
                else
                    log("<- " + name + ": " + ArgValue(result));
            }

            public void Failed()
            {
                log("x- " + name);
            }
        }
    }
}
