using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;
using System.ServiceProcess;

using AuthorizationClientPlatforms;

using AuthorizationClientPlatforms.Logging;

using MjhGeneral.ServiceProcess;

namespace BarclaycardSmartPayProcessor
{
    [WindowsService(Name=Constants.ApplicationName, Description=Constants.Description)]
    public class BarclaycardSmartPayService : ServiceBase
    {
        public ServiceHost serviceHost = null;

        public BarclaycardSmartPayService( string instanceName )
        {
            if (!String.IsNullOrEmpty(instanceName))
            {
                ServiceName = String.Format("{0} {1}", Constants.ApplicationName, instanceName);
            }
            else
            {
                ServiceName = Constants.ApplicationName;
            }
        }

        // Start the Windows service.
        protected override void OnStart(string[] args)
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            // Create a ServiceHost for the Authorization Processor type and 
            // provide the base address.
            serviceHost = new ServiceHost(typeof(AuthorizationProcessor));

            // Open the ServiceHostBase to create listeners and start 
            // listening for messages.
            serviceHost.Open();

            IpsTmsEventSource.Log.LogInformational(String.Format("{0} service started", ServiceName));
        }

        protected override void OnStop()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }

            IpsTmsEventSource.Log.LogInformational(String.Format("{0} service stopped", ServiceName));
        }
    }
}
