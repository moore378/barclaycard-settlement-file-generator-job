using System;
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
            return new MethodTracker(log, name);
        }

        private static string ArgValue(object arg)
        {
            Type argType = arg.GetType();
            if (argType.IsClass && !argType.IsPrimitive && argType != typeof(string))
                return "{" + ObjectEx.FieldsAndProps(arg).Select(k => k.Key + "=" + ArgValue(k.Value)).JoinStr(";") + "}";
            else
                return arg.ToString();
        }

        private class MethodTracker : IDatabaseMethodTracker
        {
            private Action<string> log;
            private string name;

            public MethodTracker(Action<string> log, string name)
            {
                this.log = log;
                this.name = name;
            }

            public void Successful()
            {
                log("<- " + name);
            }

            public void Failed()
            {
                log("x- " + name);
            }
        }
    }
}
