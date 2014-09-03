using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace AutoDatabase
{
    /// <summary>
    /// Annotation for arguments going to a database method
    /// </summary>
    public class DatabaseParamAttribute : Attribute
    {
        private SqlDbType dbType;
        private string sqlName;

        public DatabaseParamAttribute(SqlDbType dbType)
        {
            this.DbType = dbType;
        }

        public DatabaseParamAttribute()
        {

        }

        public string SqlName { get { return sqlName; } set { sqlName = value; UseSqlName = true; } }
        public SqlDbType DbType { get { return dbType; } set { dbType = value; UseDbType = true; } }
        /// <summary>
        /// Set to true to not serialize this field/parameter to the database
        /// </summary>
        public bool Ignore { get; set; }

        public bool UseSqlName { get; set; }

        public bool UseDbType { get; set; }
    }
}
