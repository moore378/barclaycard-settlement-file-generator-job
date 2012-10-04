using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cctm
{
    public class CctmPerformanceCounters
    {
        private PerformanceCounter activeTransactions;
        private PerformanceCounter failedTransactions;
        private PerformanceCounter avgTransactionDuration;
        private PerformanceCounter avgTransactionDurationBase;
        private PerformanceCounter startedTransactions;
        private PerformanceCounter successfulTransactions;
        private PerformanceCounter approvedTransactions;
        private PerformanceCounter declinedOrErrorTransactions;
        private PerformanceCounter queuedTransactions;

        public CctmPerformanceCounters()
        {
            this.queuedTransactions = new PerformanceCounter("CCTM 2", "Queued Transactions", false);
            this.activeTransactions = new PerformanceCounter("CCTM 2", "Active Transactions", false);
            this.startedTransactions = new PerformanceCounter("CCTM 2", "Started Transactions / s", false);
            this.failedTransactions = new PerformanceCounter("CCTM 2", "Failed Transactions / s", false);
            this.successfulTransactions = new PerformanceCounter("CCTM 2", "Successful Transactions /s", false);
            this.avgTransactionDuration = new PerformanceCounter("CCTM 2", "Avg Transaction Duration", false);
            this.avgTransactionDurationBase = new PerformanceCounter("CCTM 2", "Avg Transaction Duration Base", false);
            this.approvedTransactions = new PerformanceCounter("CCTM 2", "Approved Transactions / s", false);
            this.declinedOrErrorTransactions = new PerformanceCounter("CCTM 2", "Declined or Error Transactions / s", false);

            this.queuedTransactions.RawValue = 0;
            this.activeTransactions.RawValue = 0;
            this.startedTransactions.RawValue = 0;
            this.failedTransactions.RawValue = 0;
            this.successfulTransactions.RawValue = 0;
            this.avgTransactionDuration.RawValue = 0;
            this.avgTransactionDurationBase.RawValue = 0;
            this.approvedTransactions.RawValue = 0;
            this.declinedOrErrorTransactions.RawValue = 0;
        }

        public virtual void QueuedTransaction()
        {
            queuedTransactions.Increment();
        }

        public virtual TransactionStats StartingTransaction()
        {
            activeTransactions.Increment();
            startedTransactions.Increment();
            queuedTransactions.Decrement();

            return new TransactionStats(this);
        }

        public class TransactionStats : IDisposable
        {
            protected CctmPerformanceCounters stats;
            protected int done = 0;

            public TransactionStats(CctmPerformanceCounters stats)
            {
                this.stats = stats;
            }

            ~TransactionStats()
            {
                // If it gets this far without failing or succeeding, then its a failure
                if (Interlocked.CompareExchange(ref done, 1, 0) == 0)
                {
                    stats.activeTransactions.Decrement();
                    stats.failedTransactions.Increment();
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="durationTicks">Ticks as returned from StopWatch.ElapsedTicks</param>
            public virtual void FailedTransaction(long durationTicks)
            {
                if (Interlocked.CompareExchange(ref done, 1, 0) == 0)
                {
                    stats.activeTransactions.Decrement();
                    stats.failedTransactions.Increment();
                    stats.avgTransactionDuration.IncrementBy(durationTicks);
                    stats.avgTransactionDurationBase.Increment();
                    stats.declinedOrErrorTransactions.Increment();
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="durationTicks">Ticks as returned from StopWatch.ElapsedTicks</param>
            public virtual void SuccessfulTransaction(long durationTicks, bool approved)
            {
                if (Interlocked.CompareExchange(ref done, 1, 0) == 0)
                {
                    stats.activeTransactions.Decrement();
                    stats.successfulTransactions.Increment();
                    stats.avgTransactionDuration.IncrementBy(durationTicks);
                    stats.avgTransactionDurationBase.Increment();
                    if (approved)
                        stats.approvedTransactions.Increment();
                    else
                        stats.declinedOrErrorTransactions.Increment();

                }
            }

            public virtual void Dispose()
            {
                // If it gets this far without failing or succeeding, then its a failure
                if (Interlocked.CompareExchange(ref done, 1, 0) == 0)
                {
                    stats.activeTransactions.Decrement();
                    stats.failedTransactions.Increment();
                    GC.SuppressFinalize(this);
                }
            }
        }

    }
}
