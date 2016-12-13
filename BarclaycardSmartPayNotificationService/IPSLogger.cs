using System;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;

namespace BarclaycardSmartPayNotificationService
{
    public sealed class IPSLogger
    {
        private readonly ObservableEventListener listener;
        private static volatile IPSLogger mInstance;
        private static Object syncRoot = new Object();

        // Get the location and name of the log file.
        string logFileName = ConfigurationManager.AppSettings["logFileName"];
        int rollSizeKB = Convert.ToInt32(ConfigurationManager.AppSettings["rollSizeKB"]);
            
        RollInterval rollInterval = RollInterval.Week;            
        

        private IPSLogger()
        {
            // Create the event listener
            listener = new ObservableEventListener();
            //var eventSource = new EventSource();
            listener.EnableEvents(IpsEventSource.Log, EventLevel.LogAlways);
            //listener.EnableEvents(new EventSource(), EventLevel.LogAlways, Keywords.All);
            listener.LogToConsole();
            //listener.LogToFlatFile(@"logs\newlog.txt", new JsonEventTextFormatter(), true);

            //Week, Day, Hour, Midnight, Minute, Month, and Year
            switch (ConfigurationManager.AppSettings["rollInterval"])
            {
                case "Day":
                    rollInterval = RollInterval.Day;
                    break;
                case "Hour":
                    rollInterval = RollInterval.Hour;
                    break;
                case "Midnight":
                    rollInterval = RollInterval.Midnight;
                    break;
                case "Minute":
                    rollInterval = RollInterval.Minute;
                    break;
                case "Month":
                    rollInterval = RollInterval.Month;
                    break;
                case "Year":
                    rollInterval = RollInterval.Year;
                    break;
                default:
                    rollInterval = RollInterval.Week;
                    break;
            }

            listener.LogToRollingFlatFile(logFileName, rollSizeKB, "MM-dd-yyyy", RollFileExistsBehavior.Increment, rollInterval, new IpsTextFormatter(), 4);
        }

        public static IPSLogger Instance
        {
            get
            {
                if (mInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (mInstance == null)
                            mInstance = new IPSLogger();
                    }
                }
                return mInstance;
            }
        }

        ~IPSLogger()
        {
            listener.DisableEvents(IpsEventSource.Log);
            //listener.Dispose();
        }
        
        public string LogException(Exception ex)
        {
            var message = ex.Message;
            try
            {
                message += " {" + ex.InnerException.Source + ":" + ex.InnerException.Message + ":" +
                           ex.InnerException.StackTrace + "} ";
                IpsEventSource.Log.Critical(message);
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return "worked";
            //IpsEventSource.Log.Critical(message);
        }

        public void LogWarning(string message)
        {
            IpsEventSource.Log.Warning(message);
        }

        public string LogInformational(string message)
        {
            IpsEventSource.Log.Informational(message);
            return "worked";
        }

        
    }
}
