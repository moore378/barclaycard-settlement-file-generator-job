using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon.Common;
using System.Threading;

namespace TransactionManagementCommon.ControllerBase
{
    /// <summary>
    /// Manages the high-level operation of a server module. Maintains the state of the server, and allows operations to be done on the server.
    /// </summary>
    /// <remarks>
    /// The term "server" here refers to a module (object) on which operations can be performed if the object is in a 
    /// suitable state (such as "connected" or "ready"). This is particularly suited for situations where a "server"
    /// represents a remote object or service locally, and as such the object may sometimes not be in a suitable state to
    /// perform operations (if it is disconnected for example). 
    /// <para>
    /// The server controller is thread-safe because the target scope of operations are those which may be remote,
    /// and therefore execute slower or longer than local operations. Similarly, server initialization and restarts
    /// are performed using a server initializer and is therefore performed asynchonously to the calling thread. 
    /// Operations that are performed before the server has finished initializing will be queued and the caller
    /// WILL BE BLOCKED until the operation completes or fails.
    /// </para>
    /// <para>
    /// To perform operations on a server, use <see cref="ServerController.Perform"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="ServerType">The type of server to encapsulate</typeparam>
    //[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class ServerController<ServerType>
    {
        public ServerFactory<ServerType> ServerFactory;
        // The actual server object
        private Lazy<ServerType> encapsulated;
        // A lock on the server reference. This prevents the server from being used while it is changing or restarting
        private UseInstallLock serverLock = new UseInstallLock();
        // The number of operations currently being processed
        public int ActiveOperations { get { return activeOperations; } }
        private void IncActiveOperations() { Interlocked.Increment(ref activeOperations); UpdateStatus(); }
        private void DecActiveOperations() { Interlocked.Decrement(ref activeOperations); UpdateStatus(); }
        private int activeOperations = 0;
        private ManualResetEvent triggerRestart = new ManualResetEvent(true);
        private ManualResetEvent triggerAbort = new ManualResetEvent(false);

        /// <summary>
        /// The status of the server. This is more comprehensive and flexible than the server "State". 
        /// </summary>
        /// <remarks>
        /// The status string is affected by many things. When the server is initializing or restarting, the status
        /// is controlled by the ServerInitializer with information from the ServerController and will take on values
        /// such as "Initializing", "Restarting" or "Error Restarting". 
        /// <para>
        /// While the server is operating, the status may be something like "Busy", "Ready" or "Aborted".
        /// </para>
        /// </remarks>
        public string Status { get { return status.Value; } private set { status.Value = value; } }
        private ThreadSafeResource<string> status = new ThreadSafeResource<string>();

        /// <summary>
        /// The state of the server - E.g. Ready, Restarting or Aborted
        /// </summary>
        public State ServerState { get { return (State)serverState; } private set { serverState = value; UpdateStatus(); } }
        private volatile State serverState = State.Uninitialized;
        public enum State : int
        {
            /// <summary>
            /// The server has not yet entered the initialization routine.
            /// </summary>
            Uninitialized = 0,
            /// <summary>
            /// The server object is being initialized for the first time.
            /// </summary>
            Initializing = 1,
            /// <summary>
            /// The server is ready to perform operations or is already performing operations
            /// </summary>
            Ready = 2,
            /// <summary>
            /// The server is in an error state, waiting to restart
            /// </summary>
            WaitingForRestart = 3,
            /// <summary>
            /// The server has restarted unsuccessfully and is trying again
            /// </summary>
            Restarting = 4,
            /// <summary>
            /// The server encountered an irrecoverable error and has aborted. It may still be manually restarted using <c>ServerController.Restart()</c>.
            /// </summary>
            Aborted = 5
        }

        private enum RestartResult { Restarted, ErrorAbort, Timeout, ErrorRetry }

        private void UpdateStatus()
        {
            string newStatus = serverState.ToString();

            switch (serverState)
            {
                case State.Ready:
                    newStatus = activeOperations > 0 ? "Busy (" + activeOperations.ToString() + ")" : "Ready";
                    break;
                case State.WaitingForRestart:
                    newStatus = "Waiting to restart (" + activeOperations.ToString() + ")";
                    break;
            }

            Status = newStatus;
        }

        private RestartResult PerformRestart(State restartState, int triedCount)
        {
            ManualResetEvent restartingCompleted = new ManualResetEvent(false);
            RestartResult restartResult = RestartResult.ErrorAbort;

            if (!serverLock.EnterInstallLock())
                throw new Exception("Resource state recovery failed: Exclusive access denied");
            try
            {
                ServerState = restartState;
                restartingCompleted.Reset();
                ServerFactory<ServerType>.FactoryResult factoryResult = ServerFactory<ServerType>.FactoryResult.Error;
                Exception restartProblem = null;
                // Create the server (this call may be synchronous/blocking or asynchronous)
                encapsulated = ServerFactory.CreateServer(() => { }, (progress) => { }, (result) => { factoryResult = result; restartingCompleted.Set(); }, (problem) => { restartProblem = problem; });
                // Wait for the server to finish initializing. The 10min timeout here is for unpredictable circumstances where the factory thread may have deadlocked or died unexpectedly. 
                if (!restartingCompleted.WaitOne(600000))
                {
                    // This should never happen, but needs to be catered for.
                    restartResult = RestartResult.Timeout;
                }
                else
                {
                    if (restartProblem != null)
                        throw restartProblem;

                    switch (factoryResult)
                    {
                        case ServerFactory<ServerType>.FactoryResult.Created:
                            restartResult = RestartResult.Restarted;
                            break;
                        case ServerFactory<ServerType>.FactoryResult.Error:
                        default:
                            restartResult = RestartResult.ErrorAbort;
                            break;
                    }
                }
            }
            catch (Exception restartProblem)
            {
                RestartFailAction failAction = RestartFailAction.Abort;
                // See how the user wants to respond to the problem
                if (FailedRestart != null)
                    failAction = FailedRestart(restartProblem, triedCount);
                // Take appropriate action
                switch (failAction)
                {
                    case RestartFailAction.Retry:
                        return RestartResult.ErrorRetry;
                    default: //case RestartFailAction.Abort:
                        return RestartResult.ErrorAbort;
                }
            }
            finally
            {
                serverLock.ExitInstallLock(restartResult == RestartResult.Restarted);
            }
            return restartResult;
        }

        private void ControllerThreadMain()
        {
            int restartCount = 0;

            ServerState = State.Uninitialized;
            try
            {
                // While not aborted
                while (!triggerAbort.WaitOne(0))
                {
                    // Wait for someone to say it needs (re)starting or aborting
                    if (WaitHandle.WaitAny(new WaitHandle[] { triggerRestart, triggerAbort }) == 1)
                        return;

                    ServerState = restartCount == 0 ? State.Uninitialized : State.WaitingForRestart;

                    RestartResult restartResult = RestartResult.ErrorAbort;
                    try
                    {
                        // Attempt the restart
                        restartResult = PerformRestart(restartCount == 0 ? State.Initializing : State.Restarting, restartCount);
                    }
                    finally
                    {
                        restartCount++;
                        triggerRestart.Reset();
                    }

                    // Take action depending on how the restart went
                    switch (restartResult)
                    {
                        case RestartResult.Restarted:
                            ServerState = State.Ready;
                            break;
                        case RestartResult.ErrorAbort:
                            QueueAbort();
                            break;
                        case RestartResult.ErrorRetry:
                            // Retries can occur frequently
                            Thread.Sleep(2000);
                            QueueRestart();
                            break;
                        case RestartResult.Timeout:
                            // A timeout is a critical problem. This should be logged. This 5 second sleep is in addition to the timeout time, so it is actually quite long before it restarts.
                            Thread.Sleep(5000);
                            QueueRestart();
                            break;
                        default: // Should never happen
                            QueueAbort();
                            break;
                    }
                }
            }
            finally
            {
                // Since the controller thread is dead, we've lost control.. all future operations are going to be aborted. (This should never get here).
                ServerState = State.Aborted;
            }
        }

        /// <summary>
        /// Queues the server for a restart if it isnt already.
        /// </summary>
        private void QueueRestart()
        {
            // Signal to the server that it needs to restart
            triggerRestart.Set();
        }

        private void QueueAbort()
        {
            triggerAbort.Set();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationExceptionHandler">The delegate to decide exception behavior</param>
        /// <param name="statusUpdate">The event triggered when the server status changes.</param>
        /// <param name="initialize">The factory method to create a new server object</param>
        /// <param name="restart">The factory method to restart a currently executing server</param>
        /// <remarks>Once the object is created, it will immediately create a new encapsulated server object.</remarks>
        public ServerController(
            ServerFactory<ServerType> serverFactory,
            Action<string> updatedStatus = null,
            FailedRestartEventHandler failedRestart = null
            )
        {
            this.status.Changed += updatedStatus;
            this.ServerFactory = serverFactory;
            this.FailedRestart = failedRestart;

            // Initialize the server
            Initialize();
        }

        public delegate OperationFailAction ExceptionHandler(Exception exception, int triedCount);

        /// <summary>
        /// Performs an operation on the server, where the operation returns the given result type
        /// </summary>
        /// <typeparam name="ResultType">The result-type of the operation to perform</typeparam>
        /// <param name="operation">The operation to perform on the server - A delegate which uses the server and returns the result type</param>
        /// <param name="exceptionHandler">
        /// A delegate to dictate what happens if the operation fails. The fail behavior is specified per operation because it may be different for different operations on the server.
        /// </param>
        /// <returns>The result of the operation</returns>
        /// <exception cref="Exception">May throw any exception that the operation throws</exception>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public ResultType Perform<ResultType>(Func<ServerType, ResultType> operation, ExceptionHandler exceptionHandler = null)
        {
            return _Perform<object, ResultType>((server, param) => operation(server), null, exceptionHandler);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        private ResultType _Perform<ParamType, ResultType>(Func<ServerType, ParamType, ResultType> operation, ParamType param, ExceptionHandler exceptionHandler = null)
        {
            int triedCount = 0;
            bool tryPerform = true;

            while (tryPerform)
            {
                if (ServerState == State.Aborted)
                    throw new Exception("Server failure: Not in state to perform operation");

                triedCount++;
                tryPerform = false;
                try
                {
                    if (!serverLock.EnterUseLock(30000))
                        throw new Exception("Server failure: Timeout waiting to use resource.");

                    IncActiveOperations();
                    try
                    {
                        return operation(encapsulated.Value, param);
                    }
                    finally
                    {
                        DecActiveOperations();
                        serverLock.ExitUseLock();
                    }
                }
                catch (Exception exception)
                {
                    OperationFailAction action;
                    if (exceptionHandler != null)
                        action = exceptionHandler(exception, triedCount + 1);
                    else
                        action = OperationFailAction.AbortAndRestart;

                    switch (action)
                    {
                        case OperationFailAction.Abort:
                            QueueAbort();
                            throw;

                        case OperationFailAction.AbortAndRestart:
                            QueueRestart();
                            throw;

                        case OperationFailAction.RestartAndRetry:
                            // Restart the server - this will return once the server is queued for restart and future operations are paused 
                            QueueRestart(); // Restart
                            Thread.Sleep(500); // Try again in a moment
                            tryPerform = true;
                            break;

                        case OperationFailAction.RetryNoRestart:
                            tryPerform = true;
                            break;

                        default:
                            QueueAbort();
                            throw;
                    }
                }
            }
            throw new Exception("Unexpected branch"); // It should never get here
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Perform(Action<ServerType> operation, ExceptionHandler exceptionHandler = null)
        {
            // Suppress and ignore the result
            _Perform<object, object>((server, param) => { operation(server); return null; }, null, exceptionHandler);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public ResultType Perform<ParamType, ResultType>(Func<ServerType, ParamType, ResultType> operation, ParamType param, ExceptionHandler exceptionHandler = null)
        {
            // Suppress and ignore the result
            return _Perform<ParamType, ResultType>(operation, param, exceptionHandler);
        }

        /// <summary>
        /// Set a property on the server
        /// </summary>
        /// <typeparam name="ValueType">The type of value which will be passed</typeparam>
        /// <param name="setter">The operation to use to set the value</param>
        /// <param name="value">The new value for the property</param>
        /// <param name="exceptionHandler">What to do if there is a problem (default is to abort)</param>
        //[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Set<ValueType>(Action<ServerType, ValueType> setter, ValueType value, ExceptionHandler exceptionHandler = null)
        {
            // Suppress and ignore the result
            _Perform<ValueType, object>((server, param) => { setter(server, value); return null; }, value, exceptionHandler);
        }

        //[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public ValueType Get<ValueType>(Func<ServerType, ValueType> getter, ExceptionHandler exceptionHandler = null)
        {
            return _Perform<object, ValueType>((server, param) => getter(server), null, exceptionHandler);
        }

        private void ServerStateChanged()
        {

        }

        private void Initialize()
        {
            Thread ControllerThread = new Thread(new ThreadStart(ControllerThreadMain));
            ControllerThread.IsBackground = true;
            ControllerThread.Start();
        }

        /// <summary>
        /// What action to take when the server fails to restart.
        /// </summary>
        public FailedRestartEventHandler FailedRestart;
    }
}
