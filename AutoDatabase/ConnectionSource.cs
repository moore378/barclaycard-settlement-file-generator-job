using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace AutoDatabase
{
    public class ConnectionSource : IConnectionSource
    {
        private string connectionString;

        public ConnectionSource(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public object GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
