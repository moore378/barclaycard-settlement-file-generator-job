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

        public DatabaseTracker(Action<string> log)
        {
            this.log = log;
        }

        public IDatabaseMethodTracker StartingQuery(string name, params object[] args)
        {
            log("-> " + name);
            return new MethodTracker(log, name);
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
