using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TransactionManagementCommon.Common
{
    /// <summary>
    /// Represents a value which is thread-locked using a read-write lock, and has a "changed" event.
    /// </summary>
    /// <typeparam name="ResourceType"></typeparam>
    /// <remarks>
    /// The locking only applies to assignments of the resource, and so this generic class should only
    /// be used to control value-types or immutable classes. Likewise, the "Changed" event only gets
    /// raised when the value is assigned, not changed internally.
    /// </remarks>
    //[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class ThreadSafeResource<ResourceType>
    {
        private ResourceType value;
        public void WriteLock()
        {
            valueLock.EnterWriteLock();
        }
        public void WriteUnlock()
        {
            valueLock.ExitWriteLock();
            if (Changed != null)
                Changed(value);
        }
        private ReaderWriterLockSlim valueLock = new ReaderWriterLockSlim();
        public ResourceType DirectValue { get { return value; } set { this.value = value; } }
        public ResourceType Value
        {
            get
            {
                valueLock.EnterReadLock();
                try { return value; }
                finally { valueLock.ExitReadLock(); }
            }
            set
            {
                WriteLock();
                try
                {
                    this.value = value;
                }
                finally { WriteUnlock(); }

            }
        }
        /// <summary>
        /// This is fired when the value of the resource is changed
        /// </summary>
        public Action<ResourceType> Changed { get; set; }

    } 
}
