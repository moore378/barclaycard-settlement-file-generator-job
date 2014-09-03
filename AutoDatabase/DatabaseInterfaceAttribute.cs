using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDatabase
{
    public class DatabaseInterfaceAttribute : Attribute
    {
        public Type StoredProcNameConverter { get; set; }
    }
}
