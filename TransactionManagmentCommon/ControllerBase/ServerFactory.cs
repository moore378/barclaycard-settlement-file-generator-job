using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon.ControllerBase
{
    /// <summary>
    /// Creates servers of the given parametrized type.
    /// </summary>
    /// <typeparam name="ServerType">The type of server the factory will produce</typeparam>
    public abstract class ServerFactory<ServerType>
    {
        public enum Progress
        {
            /// <summary>
            /// The initial state - has not started being created yet
            /// </summary>
            Initial,
            /// <summary>
            /// The server is queued for creation - this is relevant in a multi-tasking environment when the factory method is queued to execute
            /// </summary>
            Queued,
            /// <summary>
            /// The server is being created
            /// </summary>
            Initializing,
            /// <summary>
            /// The server was created successfully
            /// </summary>
            Ready,
            /// <summary>
            /// There was an exception thrown during the factory method
            /// </summary>
            Error
        };

        public enum FactoryResult
        {
            Created,
            Error
        }

        /// <summary>
        /// Create a server of the specified parametrized type. The server is guaranteed to exist by first
        /// time the Lazy reference is navigated (Lazy.Value), even if this means blocking Lazy.Value until
        /// the server is finished creating, but is not guaranteed to have any value before then Lazy.Value
        /// is accessed. 
        /// </summary>
        /// <param name="progressChange">A delegate to track the progress of the server creation</param>
        /// <param name="finallyRun">By specification, the implementing method MUST execute "finallyRun" (if provided) after the attempting to create the server, regardless of the outcome or exceptions thrown. </param>
        /// <param name="catchRun">This will be executed if there is an exception during the initialization process</param>
        /// <returns>A lazy reference to the server.</returns>
        /// <remarks>
        /// The returned reference is Lazy because by specification the server need not 
        /// actually exist until it is explicitly accessed. This design model is used here 
        /// because some servers may be very slow to create (for example connections to
        /// slow or non-existent remote network servers) and shouldnt block the builder 
        /// thread.
        /// <para>
        /// Since this call does not necesserily create the server synchronously (blocking), the progressChange
        /// delegate allows a way to track the progres. If the server creation fails, the progress will be set
        /// to "Error", and a call to access the Lazy value will fail will the specific exception.
        /// </para>
        /// </remarks>
        public abstract Lazy<ServerType> CreateServer(
            Action initiallyRun = null,
            Action<Progress> progressChange = null,
            Action<FactoryResult> finallyRun = null,
            Action<Exception> catchRun = null);
    }
}
