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
        [StoredProcedure(Name="UPD_TRANSACTIONRECORD_CCTM_FINALIZATION")]
        Task UpdTransactionrecordCctmFinalization(DbUpdTransactionrecordcctmFinalizationParams args);
    }
}
