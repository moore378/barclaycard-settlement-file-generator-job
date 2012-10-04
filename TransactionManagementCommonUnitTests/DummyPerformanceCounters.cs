using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests
{
    public class DummyPerformanceCounters : Rtcc.RtccPerformanceCounters
    {
        public override SessionStats NewSession()
        {
            return new DummySessionStats();
        }

        private class DummySessionStats : SessionStats
        {
            public DummySessionStats()
                : base (null)
            {
                base.done = 1; // Prevent destructor
            }

            public override void Dispose()
            {
                
            }

            public override void FailedSession(long durationTicks)
            {

            }

            public override void SuccessfulSession(long durationTicks)
            {
                
            }
        }
    }
}
