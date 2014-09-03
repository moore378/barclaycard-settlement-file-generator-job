using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDatabase
{
    public interface IDatabaseTracker
    {
        IDatabaseMethodTracker StartingQuery(string name, params object[] args);
    }
}
