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

namespace FisPayDirectProcessor
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
            _listener.DisableEvents(IpsTmsEventSource.Log);
            _listener.Dispose();
        }

        static void Main(string[] args)
        {
            bool printUsage = false;

            // Initialize the logging system.
            LogInitialize();

            switch (args.Length)
            {
                case 1:
                    switch (args[0])
                    {
                        case "console":
                            RunConsole();
                            break;
                        case "install":
                            InstallService();
                            break;
                        case "uninstall":
                            StopService();
                            UninstallService();
                            break;
                        default:
                            printUsage = true;
                            break;
                    }
                    break;
                case 0:
                    // Run the service normally.
                    ServiceBase.Run(new FisPayDirectService());
                    break;
                default:
                    printUsage = true;
                    break;
            }

            if (printUsage)
            {
                Console.WriteLine("Invalid arguments");
            }

            LogShutdown();
        }

        public static void RunConsole()
        {
            using (ServiceHost host = new ServiceHost(typeof(AuthorizationProcessor)))
            {
                IpsTmsEventSource.Log.LogInformational(String.Format("{0} started service as console application", Constants.SERVICE_NAME));

                host.Open();

                Console.WriteLine("The service is ready");
                Console.WriteLine("Press <Enter> to stop the service.");
                Console.ReadLine();

                // Close the ServiceHost.
                host.Close();

                IpsTmsEventSource.Log.LogInformational(String.Format("{0} console application stopped", Constants.SERVICE_NAME));
            }
        }


        private static AssemblyInstaller GetInstaller()
        {
            AssemblyInstaller installer = new AssemblyInstaller(
                typeof(FisPayDirectService).Assembly, null);
            installer.UseNewContext = true;
            return installer;
        }

        private static bool IsInstalled()
        {
            using (ServiceController controller =
                new ServiceController(Constants.SERVICE_NAME))
            {
                try
                {
                    ServiceControllerStatus status = controller.Status;
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        private static bool IsRunning()
        {
            using (ServiceController controller =
                new ServiceController(Constants.SERVICE_NAME))
            {
                if (!IsInstalled()) return false;
                return (controller.Status == ServiceControllerStatus.Running);
            }
        }

        private static void InstallService()
        {
            Console.WriteLine("Installing service {0}", Constants.SERVICE_NAME);

            if (IsInstalled()) return;

            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Install(state);
                        installer.Commit(state);
                    }
                    catch
                    {
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }

            Console.WriteLine("Installing service completed");
        }

        private static void UninstallService()
        {
            if (!IsInstalled()) return;
            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Uninstall(state);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        private static void StopService()
        {
            if (!IsInstalled()) return;
            using (ServiceController controller =
                new ServiceController(Constants.SERVICE_NAME))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Stopped)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped,
                             TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}
