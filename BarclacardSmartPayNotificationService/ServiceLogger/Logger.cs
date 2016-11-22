using System;
using System.Diagnostics.Tracing;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using System.Configuration;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;

namespace ServiceLogger
{
    public sealed class Logger
    {

        private readonly ObservableEventListener _listener;
        private static volatile Logger _mInstance;
        private static readonly Object SyncRoot = new Object();

        private Logger()
        {
            string logFileName = ConfigurationManager.AppSettings["logFileName"];
            int rollSizeKb = Convert.ToInt16(ConfigurationManager.AppSettings["rollSizeKB"]);
            RollInterval rollInterval;
            switch (ConfigurationManager.AppSettings["rollInterval"])
            {
                case "Week":
                    rollInterval = RollInterval.Week;
                    break;
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

            // Create the event listener
            _listener = new ObservableEventListener();
            //var eventSource = new EventSource();
            _listener.EnableEvents(ServiceLogger.Log, EventLevel.LogAlways);
            //listener.EnableEvents(new EventSource(), EventLevel.LogAlways, Keywords.All);
            _listener.LogToConsole();
            //listener.LogToFlatFile(@"logs\newlog.txt", new JsonEventTextFormatter(), true);
            _listener.LogToRollingFlatFile(logFileName, rollSizeKB: rollSizeKb, timestampPattern: "MM-dd-yyyy", rollFileExistsBehavior: RollFileExistsBehavior.Increment,
                rollInterval: rollInterval, formatter: new LoggerTextFormatter(), maxArchivedFiles: 4);
        }

        public static Logger Instance
        {
            get
            {
                if (_mInstance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_mInstance == null)
                            _mInstance = new Logger();
                    }
                }
                return _mInstance;
            }
        }

        ~Logger()
        {
            _listener.DisableEvents(ServiceLogger.Log);
        }

        public void LogException(Exception ex)
        {
            var message = ex.Message;
            try
            {
                if (ex.InnerException != null)
                    message += " {" + ex.InnerException.Source + ":" + ex.InnerException.Message + ":" + ex.InnerException.StackTrace + "} ";
            }
            catch (Exception)
            {
                // ignored
            }

            ServiceLogger.Log.Critical(message);
        }

        public void LogInformational(string message)
        {
            ServiceLogger.Log.Informational(message);
        }
    }
}
