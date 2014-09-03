using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Logging;

namespace AutoDatabase.DebugProxy
{
    public class DebugSqlConnectionSource : IConnectionSource
    {
        private IConnectionSource inner;
        private ITreeLog log;

        public DebugSqlConnectionSource(ITreeLog log)
        {
            this.log = log;
        }

        public DebugSqlConnectionSource(IConnectionSource inner, ITreeLog log)
        {
            this.inner = inner;
            this.log = log;
        }

        public object GetConnection()
        {
            return new DebugSqlConnection((SqlConnection)inner.GetConnection(), log.CreateChild("New connection"));
        }
    }
}
