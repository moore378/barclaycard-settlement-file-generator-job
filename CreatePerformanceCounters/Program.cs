using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;

namespace CreatePerformanceCounters
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                CreateRtccCounters();
                CreateCctmCounters();
            }
            catch (SecurityException)
            {
                Console.WriteLine("Run as administrator");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void CreateCctmCounters()
        {
            if (PerformanceCounterCategory.Exists("CCTM 2"))
            {
                Console.WriteLine("CCTM counters already exist. Deleting.");
                PerformanceCounterCategory.Delete("CCTM 2");
            }

            var counters = new CounterCreationDataCollection();

            counters.Add(new CounterCreationData()
            {
                CounterName = "Active Transactions",
                CounterType = PerformanceCounterType.NumberOfItems32
            });

            counters.Add(new CounterCreationData()
            {
                CounterName = "Started Transactions / s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Failed Transactions / s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Successful Transactions /s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Avg Transaction Duration",
                CounterType = PerformanceCounterType.AverageTimer32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Avg Transaction Duration Base",
                CounterType = PerformanceCounterType.AverageBase
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Approved Transactions /s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Declined or Error Transactions /s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            PerformanceCounterCategory.Create(
                "CCTM 2", "", PerformanceCounterCategoryType.SingleInstance, counters);

            Console.WriteLine("CCTM counters created");
        }

        private static void CreateRtccCounters()
        {
            if (PerformanceCounterCategory.Exists("RTCC 2"))
            {
                Console.WriteLine("RTCC counters already exist. Deleting.");
                PerformanceCounterCategory.Delete("RTCC 2");
            }

            var counters = new CounterCreationDataCollection();

            counters.Add(new CounterCreationData()
            {
                CounterName = "Active Sessions",
                CounterType = PerformanceCounterType.NumberOfItems32
            });

            counters.Add(new CounterCreationData()
            {
                CounterName = "Started Sessions / s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Failed Sessions / s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Successful Sessions /s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Avg Session Duration",
                CounterType = PerformanceCounterType.AverageTimer32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Avg Session Duration Base",
                CounterType = PerformanceCounterType.AverageBase
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Approved Transactions /s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            counters.Add(new CounterCreationData
            {
                CounterName = "Declined or Error Transactions /s",
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32
            });

            PerformanceCounterCategory.Create(
                "RTCC 2", "", PerformanceCounterCategoryType.SingleInstance, counters);


            Console.WriteLine("RTCC counters created");
        }
    }
}
