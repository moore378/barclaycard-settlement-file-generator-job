using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDatabase
{
    public class PersistData<TResult, TSqlCommand, TSqlConnection>
    {
        public TSqlConnection Connection;
        public TSqlCommand Command;
        public IDatabaseMethodTracker MethodTracker;
        public TaskCompletionSource<TResult> TaskCompletionSource;
    }
}
