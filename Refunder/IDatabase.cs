using AutoDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refunder
{
    [DatabaseInterface(StoredProcNameConverter = typeof(PascalCaseToUnderscoreConverter))]
    public interface IDatabase
    {
        [StoredProcedure]
        Task<IEnumerable<PendingRefund>> SelPendingRefundsCctm();

        [StoredProcedure]
        Task UpdTransactionrecordProcessedRefund(Decimal TransactionRecordID,
            Decimal TTID,
            Int16 BatNum,
            Int16 Status,
            DateTime SettlementDateTime);
    }
}
