using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoDatabase;

namespace Cctm.DualAuth
{
    [DatabaseInterface]
    public interface ICctmDualAuthDatabase
    {
        [StoredProcedure]
        Task UpdTransactionrecordCctmFinalization(DbUpdTransactionrecordcctmFinalizationParams args);
    }
}
