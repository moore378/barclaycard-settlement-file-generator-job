using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDatabase
{
    public class StoredProcedureAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
