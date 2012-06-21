using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TransactionManagementCommon.Common
{
    /// <summary>
    /// A thread-safe mutex mechanism which allows multiple simultaneous "uses" of a resource but only one "install" of the 
    /// resource. 
    /// </summary>
    /// <remarks>
    /// This is like a read-write lock for a multi-threading environment. The conceptual model is that a resource is
    /// "used" and and "installed" (analogous to "read" and "write" respectively). Many threads may use the resource
    /// simultaneously, but only one thread may install the resource at a time, and no thread may use the resource
    /// while it is being installed. This also has the extra functionality over a read-write lock in that the resource 
    /// can be "flagged" as unstable by any thread, which will block use-locks from occuring until an new install happens.
    /// <para>
    /// The initial state of the lock is that it is unstable (needs its initial value before use).
    /// </para>
    /// <para>
    /// Note: If multiple simultaneous uses of the resource all fail at a similar time (for example the server
    /// loses connection), one should try avoiding restarting the server multiple times - each time will be 
    /// queued and render the server unstable for those times. It will not however cause any threading
    /// conflict since only one installation will be preempted at a time.
    /// </para>
    /// </remarks>
    public class UseInstallLock
    {
        private ReaderWriterLockSlim resourceLock = new ReaderWriterLockSlim();
        private ManualResetEvent stable = new ManualResetEvent(false);
        private bool isInstalling;
        private bool isStable;
        private int isUsing;

        public override string ToString()
        {
            string s;

            if (isInstalling)
                s = "Installing";
            else
                s = "Installed";

            if (isStable)
                s += ", stable";
            else
                s += ", unstable";

            if (isUsing > 0)
                s += ", being used";
            else
                s += ", not used";

            return s;
        }


        /// <summary>
        /// Indicate that the calling thread wants to use the resource.
        /// </summary>
        /// <remarks>
        /// After a call to EnterUseLock, the caller MUST call ExitUseLock when it
        /// has finished using the resource.
        /// <para>
        /// This call blocks until the resource is in a stable state, meaning that it
        /// has been "installed" since it was created or marked unusable. 
        /// </para>
        /// </remarks>
        public bool EnterUseLock(int timeoutMilliseconds)
        {
            bool haveStableReadLock = false; // will be true iff we have a read lock AND the resource is guaranteed to be stable

            do
            {
                if (!resourceLock.TryEnterReadLock(timeoutMilliseconds))
                    return false;
                // Is it stable? We can only check this now that we have a read lock, because "stable" may change otherwise
                // If it is stable, then the user can continue to use the resource. So long as the read-lock is 
                // held, the resource WILL remain flagged as stable. 
                if (stable.WaitOne(0))
                    haveStableReadLock = true;
                else
                {
                    // If it isn't stable, then we need to wait for it. We can't hold the lock while we wait because the lock guarentees it not change
                    resourceLock.ExitReadLock();
                    // Wait for it to be stable
                    stable.WaitOne();
                    // Now: The resource may or may not be stable (because we don't have a read lock, there is no guarantee that another thread didn't change it between the last line and this one)
                    haveStableReadLock = false;
                }
            }
            while (!haveStableReadLock);

            Interlocked.Increment(ref isUsing);

            return true;
        }
        public void ExitUseLock()
        {
            Interlocked.Decrement(ref isUsing);
            if (!stable.WaitOne(0)) // This should never happen
                throw new Exception("Unexpected lock condition");
            resourceLock.ExitReadLock();
        }

        /// <summary>
        /// Signals that the calling thread would like to install/reinstall the resource. 
        /// </summary>
        /// <returns>Returns true if lock is acquired, and false if there is another install in progress.</returns>
        /// <remarks>
        /// After a call to EnterInstallLock, the calling thread MUST call ExitInstallLock
        /// when the installation is complete.
        /// </remarks>
        public bool EnterInstallLock()
        {
            // If there is another user installing, we assume that this install is invalid.
            if (resourceLock.IsWriteLockHeld)
                return false;

            // Get a write-lock - any existing use-locks will finish first before we can proceed here, and it will stop new use-locks from starting
            resourceLock.EnterWriteLock();
            // The resource is not stable (not in a usable state). Because we have a write-lock, we're free to change the state of "stable"
            stable.Reset();

            isInstalling = true;
            isStable = false;

            return true;
        }
        /// <summary>
        /// Signals that the calling thread has completed the installation started after the 
        /// call to EnterInstallLock().
        /// </summary>
        /// <param name="nowStable">Is the resource now stable?</param>
        /// <remarks>
        /// ExitInstallLock MUST be called for every EnterInstallLock(), regardless of the outcome
        /// of the installation. If the resource is still not in a stable state (the installation
        /// is required again), the caller can set "nowStable" to false, indicating that queued uses
        /// may not resume. 
        /// <para>
        /// WARNING: Setting "nowStable" to false will keep the server in an unusable state, meaning
        /// that all use operations will be queued indefinitely. To indicate that the resource is
        /// in a permanent state of failure (which is a stable state in itself), one should set
        /// "nowStable" to TRUE, and use another flag mechanism to specify that the resource is in
        /// an invalid stable state and operations must fail.
        /// </para>
        /// </remarks>
        public void ExitInstallLock(bool nowStable = true)
        {
            try
            {
                isInstalling = false;

                // If the resource is again usable, any waiting EnterUseLock calls will proceed (until they try  
                if (nowStable)
                {
                    isStable = true;
                    // Since we have a write-lock, we're free to change the value of "stable"
                    stable.Set();
                }
            }
            finally
            {
                // The resource write lock is released (the install is finished)
                resourceLock.ExitWriteLock();
            }

        }
        /// <summary>
        /// Call this when the resource is unstable (for example in a temporary error state that 
        /// requires fixing before uses may continue).
        /// </summary>
        /// <remarks>
        /// While the resource lock is flagged as unstable, no thread will be able to obtain a 
        /// use-lock. Once the resource is reinstalled (call EnterInstallLock and ExitInstallLock)
        /// the resource will be considered stable again (unless otherwise specified).
        /// <para>
        /// In common use of this lock class, Destabilized will be called as soon as there is an error,
        /// but the reinstallation may only occur later (when the reinstall task is preempted). Operations
        /// currently using the resource will continue (and may even aquire additional locks on the 
        /// resource), but new use-operations will be paused until the resource is flagged as stable (
        /// by a reinstall). 
        /// </para>
        /// <para>
        /// Note: Flagging the resource as unstable while it is busy installing will not have any effect
        /// because it is already unstable. When the installation is complete it will be considered stable.
        /// </para>
        /// </remarks>
        public void Destabilized()
        {
            isStable = false;
            stable.Reset();
        }
    }
}
