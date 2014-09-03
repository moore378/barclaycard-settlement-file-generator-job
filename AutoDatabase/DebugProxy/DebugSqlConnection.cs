using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Logging;

namespace AutoDatabase.DebugProxy
{
    public class DebugSqlConnection
    {
        private SqlConnection inner;
        private Logging.ITreeLog log;

        public DebugSqlConnection(SqlConnection inner, Logging.ITreeLog log)
        {
            this.inner = inner;
            this.log = log;
        }

        public void Open()
        {
            log.Log("DebugSqlConnection.Open()");
            var innerOpen = inner.GetType().GetMethod("Open");
            innerOpen.Invoke(inner, new object[0]);
        }

        public void Dispose()
        {
            log.Log("DebugSqlConnection.Dispose()");
            var innerOpen = inner.GetType().GetMethod("Dispose");
            innerOpen.Invoke(inner, new object[0]);
        }

        public ITreeLog CreateChildLog(string childName)
        {
            return log.CreateChild(childName);
        }

        public SqlConnection Inner { get { return inner; } }
    }
}
