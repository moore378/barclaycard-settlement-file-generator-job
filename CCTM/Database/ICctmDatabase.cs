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
        IEnumerable<TransactionRecord> SelectNewTransactionRecords();
        void UpdateTransactionRecord(UpdatedTransactionRecord record);
        void UpdatePreauthRecord(TransactionRecord record, UpdatedTransactionRecord updates);
        void UpdateFinalizeRecord(TransactionRecord record, UpdatedTransactionRecord updates);
        TransactionStatus? UpdateRecordStatus(int transactionRecordID, TransactionStatus oldStatus, TransactionStatus updatedStatus);
        TransactionStatus? UpdatePreauthStatus(int transactionRecordID, TransactionStatus? oldStatus, TransactionStatus updatedStatus);
        void UpdateTrack(int transactionRecordID, string track);
        void UpdateTransactionRecordCctm(TransactionRecord record, UpdatedTransactionRecord updates);
    }
}
