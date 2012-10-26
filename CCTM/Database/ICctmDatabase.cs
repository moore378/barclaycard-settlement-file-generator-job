using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cctm.Common;
using TransactionManagementCommon;

namespace Cctm.Database
{
    public interface ICctmDatabase
    {
        [Obsolete]
        IEnumerable<TransactionRecord> SelectNewTransactionRecords();

        [Obsolete]
        void UpdateTransactionRecord(UpdatedTransactionRecord record);

        [Obsolete]
        void UpdatePreauthRecord(TransactionRecord record, UpdatedTransactionRecord updates);

        [Obsolete]
        void UpdateFinalizeRecord(TransactionRecord record, UpdatedTransactionRecord updates);

        [Obsolete]
        TransactionStatus? UpdateRecordStatus(int transactionRecordID, TransactionStatus oldStatus, TransactionStatus updatedStatus);

        [Obsolete]
        TransactionStatus? UpdatePreauthStatus(int transactionRecordID, TransactionStatus? oldStatus, TransactionStatus updatedStatus);

        [Obsolete]
        void UpdateTrack(int transactionRecordID, string track);

        [Obsolete]
        void UpdateTransactionRecordCctm(TransactionRecord record, UpdatedTransactionRecord updates);
    }
}
