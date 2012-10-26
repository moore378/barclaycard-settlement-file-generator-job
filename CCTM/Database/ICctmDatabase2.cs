using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoDatabase;

namespace Cctm.Database
{
    [DatabaseInterface(StoredProcNameConverter=typeof(PascalCaseToUnderscoreConverter))]
    public interface ICctmDatabase2 :
        DualAuth.ICctmDualAuthDatabase
    {
        [return: DatabaseReturn(ColumnIndex=0)]
        Task<short> UpdTransactionrecordStatus(decimal TransactionRecordID, string CCTransactionStatus, short Status, short OldStatus);

        [return: DatabaseReturn(ColumnIndex = 0)]
        Task<short> UpdTransactionauthorizationStatus(decimal TransactionRecordID, short Status, short OldStatus);

        Task<IEnumerable<DbTransactionRecord>> SelNewTransactionrecords();

        Task UpdTransactionrecordCctm(DbUpdTransactionrecordCctmParams args);
    }
}
