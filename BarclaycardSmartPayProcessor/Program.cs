using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Configuration.Install;
using System.ServiceProcess;
using System.ServiceModel;
using System.Collections;
using System.IO;

using System.Diagnostics;
using System.Diagnostics.Tracing;

using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;

using AuthorizationClientPlatforms;
using AuthorizationClientPlatforms.Logging;

using MjhGeneral.ServiceProcess;

namespace BarclaycardSmartPayProcessor
{
    class Program
    {
        private static ObservableEventListener _listener;

        /// <summary>
        /// Initializes the logging system.
        /// TODO: Put this in some generic AuthorizationProcessorService
        /// </summary>
        private static void LogInitialize()
        {
            // Get the location and name of the log file.
            string fileName = ConfigurationManager.AppSettings["LogFile"];

            // Initialize the listeners and sinks during application start-up 
            _listener = new ObservableEventListener();
            _listener.EnableEvents(IpsTmsEventSource.Log,
                EventLevel.LogAlways, Keywords.All
                );

            RollingFlatFileSink sink = new RollingFlatFileSink(
                fileName,
                0,
                "yyyy_MM_dd_HH",
                RollFileExistsBehavior.Overwrite,
                RollInterval.Hour,
                0,
                //new IpsTextFormatter("yyyy-MM-dd HH:mm:ss"),
                new IpsTextFormatter("O"),
                false);
            _listener.Subscribe(sink);
        }

        private static void LogShutdown()
        {
            if (null != _listener)
            {
                _listener.DisableEvents(IpsTmsEventSource.Log);
                _listener.Dispose();
            }
        }

        static void Main(string[] args)
        {
            string instanceName = null;
            string mode;

            bool printUsage = false;

            if (Environment.UserInteractive)
            {
                if (0 == args.Length)
                {
                    printUsage = true;
                }
                else
                { 
                    // For now, any secondary argument is the instance's name.
                    if (2 <= args.Length)
                    {                        
                        instanceName = args[1];
                    }

                    switch (args[0])
                    {
                        case "console":
                            RunConsole(instanceName);
                            break;
                        case "install":
                            WindowsServiceInstaller.RuntimeInstall<BarclaycardSmartPayService>(instanceName);
                            break;
                        case "uninstall":
                            WindowsServiceInstaller.RuntimeUninstall<BarclaycardSmartPayService>(instanceName);
                            break;
                        default:
                            printUsage = true;
                            break;
                    }
                }

                if (printUsage)
                {
                    Console.WriteLine("Invalid arguments");
                }
            }
            else
            {
                Debugger.Launch();

                // Initialize the logging system.
                LogInitialize();

                if (1 == args.Length)
                {
                    instanceName = args[0];
                }

                // Run the service normally.
                ServiceBase.Run(new BarclaycardSmartPayService(instanceName));

                LogShutdown();
            }
        }

        public static void RunConsole(string instanceName)
        {
            string applicationName;

            if (!String.IsNullOrEmpty(instanceName))
            {
                applicationName = String.Format("{0} {1}", Constants.ApplicationName, instanceName);
            }
            else
            {
                applicationName = Constants.ApplicationName;
            }

            using (ServiceHost host = new ServiceHost(typeof(AuthorizationProcessor)))
            {
                // Initialize the logging system.
                LogInitialize();

                IpsTmsEventSource.Log.LogInformational(String.Format("{0} started service as console application", applicationName));

                host.Open();

                Console.WriteLine("The service is ready");
                Console.WriteLine("Press <Enter> to stop the service.");
                Console.ReadLine();

                // Close the ServiceHost.
                host.Close();

                IpsTmsEventSource.Log.LogInformational(String.Format("{0} console application stopped", applicationName));

                LogShutdown();
            }
        }
    }
}
