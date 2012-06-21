using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon.ControllerBase
{
    public enum OperationFailAction
    {
        /// <summary>
        /// Retries the operation without restarting the server
        /// </summary>
        RetryNoRestart,
        /// <summary>
        /// Restart the server, and then retry the operation.
        /// </summary>
        /// <remarks> 
        /// This would probably be used in a circumstance where it is known that the operation can be 
        /// safely retried, and that the server should be restarted first. Do not use this option if
        /// it is an operation that should not be retried (for example Authorizing a credit-card transaction
        /// should not be retried unless that you can guarantee that it would not cause the authorization to
        /// be performed twice).
        /// </remarks>
        RestartAndRetry,
        /// <summary>
        /// Abort the operation, and restart the server. This is the default option.
        /// </summary>
        /// <remarks>
        /// This would probably be used in the case where the server should be restarted, but the operation
        /// is not safe to retry once its been tried once (for example authorizing a transaction with an 
        /// unknown error).
        /// </remarks>
        AbortAndRestart,
        /// <summary>
        /// Abort the operation, and leave the server in Aborted mode (future operations will fail).
        /// </summary>
        /// <remarks>
        /// This is used to abort the operation and the server (perhaps the operation has been retried 
        /// too many times). Note that every following operation will then fail, until perhaps the server
        /// is restarted manually using <see cref="ServerController.Restart()"/>.
        /// </remarks>
        Abort
    };
}
