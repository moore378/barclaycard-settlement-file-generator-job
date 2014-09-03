using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace AutoDatabase
{
    /// <summary>
    /// Annotation for fields comming from the database to a local POD type
    /// </summary>
    public class DatabaseReturnAttribute : Attribute
    {
        private string sqlName;
        private int columnIndex;
        
        public string SqlName { get { return sqlName; } set { sqlName = value; FieldSource = SqlFieldSource.BySqlName; } }
        public int ColumnIndex { get { return columnIndex; } set { columnIndex = value; FieldSource = SqlFieldSource.ByColumnIndex; } }

        public SqlFieldSource FieldSource { get; set; }

        /// <summary>
        /// Set to true to ignore the field (not populate the field in the POD class)
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// The name of the getter method in SqlDataReader
        /// </summary>
        public string SqlDataReaderGetterName { get; set; }
    }

    public enum SqlFieldSource
    {
        ByFieldName, 
        BySqlName,
        ByColumnIndex
    }
}
