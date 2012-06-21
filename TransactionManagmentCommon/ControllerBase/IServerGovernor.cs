using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionManagementCommon.ControllerBase
{
    //TODO: Current unused. This is a possible simplified replacement for ServerController.
    /// <summary>
    /// When multiple processes are accessing a common server (such as a database), this can 
    /// help coordinate failures
    /// </summary>
    public interface IServerGovernor
    {
        /// <summary>
        /// Enqueue a new job to start when the server is available. 
        /// The job should call the onFailed action argument in ServerJob to
        /// alert the server of jobs that are failing because of the server.
        /// </summary>
        /// <param name="job">An unstarted task to perform.</param>
        /// <returns>Returns a task that encapsulates the new job</returns>
        Task<TResult> EnqueueJob<TResult>(ServerJob<TResult> job);
    }
}
