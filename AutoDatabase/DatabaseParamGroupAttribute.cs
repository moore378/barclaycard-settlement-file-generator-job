using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDatabase
{
    /// <summary>
    /// Attribute for a composite type passed to a stored procedure
    /// </summary>
    public class DatabaseParamGroupAttribute : Attribute
    {
        public DatabaseParamGroupAttribute()
        {
            UseAllFields = true;
        }

        /// <summary>
        /// Indicates whether all the fields and properties of the class should be
        /// used, or false to indicate that only the fields/properties marked
        /// with DatabaseField should be used.
        /// </summary>
        public bool UseAllFields { get; set; }
    }
}
