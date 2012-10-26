using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoDatabase;

namespace Cctm.DualAuth
{
    public interface ICctmDualAuthDatabase
    {
        Task UpdTransactionrecordCctmFinalization(DbUpdTransactionrecordcctmFinalizationParams args);

        Task UpdTransactionauthorization(DbUpdTransactionauthorizationParams args);
    }
}
