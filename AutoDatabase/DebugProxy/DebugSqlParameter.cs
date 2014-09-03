using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Logging;

namespace AutoDatabase.DebugProxy
{
    public class DebugSqlParameter
    {
        private ITreeLog log;
        private SqlParameter inner;

        public DebugSqlParameter(SqlParameter inner, ITreeLog log)
        {
            this.inner = inner;
            this.log = log;
        }

        public object Value
        {
            get { object res = inner.Value;  log.Log("Get: " + res); return res; }
            set { inner.Value = value; log.Log("= " + value); }
        }
        
    }
}
