using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Logging;

namespace AutoDatabase.DebugProxy
{
    public class DebugSqlParameterCollection
    {
        private SqlParameterCollection inner;
        private ITreeLog log;

        public DebugSqlParameterCollection(SqlParameterCollection inner, ITreeLog log)
        {
            this.inner = inner;
            this.log = log;
        }

        public DebugSqlParameter Add(string parameterName, SqlDbType sqlDbType)
        {
            var result = new DebugSqlParameter(inner.Add(parameterName, sqlDbType), log.CreateChild(parameterName + ": " + sqlDbType));
            return result;
        }
    }
}
