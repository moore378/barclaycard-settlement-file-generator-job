using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Rtcc
{
    public class RtccPerformanceCounters
    {
        private PerformanceCounter activeSessions;
        private PerformanceCounter failedSessions;
        private PerformanceCounter avgSessionDuration;
        private PerformanceCounter avgSessionDurationBase;
        private PerformanceCounter startedSessions;
        private PerformanceCounter successfulSessions;
        private PerformanceCounter approvedTransactions;
        private PerformanceCounter declinedTransactions;

        public RtccPerformanceCounters()
        {
            this.activeSessions = new PerformanceCounter("RTCC 2", "Active Sessions", false);
            this.startedSessions = new PerformanceCounter("RTCC 2", "Started Sessions / s", false);
            this.failedSessions = new PerformanceCounter("RTCC 2", "Failed Sessions / s", false);
            this.successfulSessions = new PerformanceCounter("RTCC 2", "Successful Sessions /s", false);
            this.avgSessionDuration = new PerformanceCounter("RTCC 2", "Avg Session Duration", false);
            this.avgSessionDurationBase = new PerformanceCounter("RTCC 2", "Avg Session Duration Base", false);
            this.approvedTransactions = new PerformanceCounter("RTCC 2", "Approved Transactions /s", false);
            this.declinedTransactions = new PerformanceCounter("RTCC 2", "Declined or Error Transactions /s", false);

            this.activeSessions.RawValue = 0;
            this.startedSessions.RawValue = 0;
            this.failedSessions.RawValue = 0;
            this.successfulSessions.RawValue = 0;
            this.avgSessionDuration.RawValue = 0;
            this.avgSessionDurationBase.RawValue = 0;
            this.approvedTransactions.RawValue = 0;
            this.declinedTransactions.RawValue = 0;
        }

        public virtual SessionStats NewSession()
        {
            activeSessions.Increment();
            startedSessions.Increment();

            return new SessionStats(this);
        }

        public class SessionStats : IDisposable
        {
            protected RtccPerformanceCounters stats;
            protected int done = 0;

            public SessionStats(RtccPerformanceCounters stats)
            {
                this.stats = stats;
            }

            ~SessionStats()
            {
                // If it gets this far without failing or succeeding, then its a failure
                if (Interlocked.CompareExchange(ref done, 1, 0) == 0)
                {
                    stats.activeSessions.Decrement();
                    stats.failedSessions.Increment();
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="durationTicks">Ticks as returned from StopWatch.ElapsedTicks</param>
            public virtual void FailedSession(long durationTicks)
            {
                if (Interlocked.CompareExchange(ref done, 1, 0) == 0)
                {
                    stats.activeSessions.Decrement();
                    stats.failedSessions.Increment();
                    stats.avgSessionDuration.IncrementBy(durationTicks);
                    stats.avgSessionDurationBase.Increment();
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="durationTicks">Ticks as returned from StopWatch.ElapsedTicks</param>
            public virtual void SuccessfulSession(long durationTicks)
            {
                if (Interlocked.CompareExchange(ref done, 1, 0) == 0)
                {
                    stats.activeSessions.Decrement();
                    stats.successfulSessions.Increment();
                    stats.avgSessionDuration.IncrementBy(durationTicks);
                    stats.avgSessionDurationBase.Increment();
                }
            }

            public virtual void ApprovedTransaction()
            {
                stats.approvedTransactions.Increment();
            }

            public virtual void DeclinedTransaction()
            {
                stats.declinedTransactions.Increment();
            }

            public virtual void Dispose()
            {
                // If it gets this far without failing or succeeding, then its a failure
                if (Interlocked.CompareExchange(ref done, 1, 0) == 0)
                {
                    stats.activeSessions.Decrement();
                    stats.failedSessions.Increment();
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}
