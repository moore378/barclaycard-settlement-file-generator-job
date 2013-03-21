using AutoDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refunder
{
    class Tracker : IDatabaseTracker
    {
        private Action<string> log;

        public Tracker(Action<string> log)
        {
            this.log = log;
        }

        public IDatabaseMethodTracker StartingQuery(string name, params object[] args)
        {
            log("-> " + name);
            return new MethodTracker(name, log);
        }

        class MethodTracker : IDatabaseMethodTracker
        {
            private Action<string> log;
            private string name;

            public MethodTracker(string name, Action<string> log)
            {
                this.name = name;
                this.log = log;
            }
            public void Successful(object result)
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
