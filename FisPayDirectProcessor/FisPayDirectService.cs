using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;
using System.ServiceProcess;

using AuthorizationClientPlatforms;

using AuthorizationClientPlatforms.Logging;

namespace FisPayDirectProcessor
{
    public class FisPayDirectService : ServiceBase
    {
        public ServiceHost _serviceHost = null;

        public FisPayDirectService()
        {
            ServiceName = Constants.SERVICE_NAME;
        }

        // Start the Windows service.
        protected override void OnStart(string[] args)
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
            }

            // Create a ServiceHost for the Authorization Processor type and 
            // provide the base address.
            _serviceHost = new ServiceHost(typeof(AuthorizationProcessor));

            // Open the ServiceHostBase to create listeners and start 
            // listening for messages.
            _serviceHost.Open();

            IpsTmsEventSource.Log.LogInformational(String.Format("{0} service started", ServiceName));
        }

        protected override void OnStop()
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
                _serviceHost = null;
            }

            IpsTmsEventSource.Log.LogInformational(String.Format("{0} service stopped", ServiceName));
        }
    }
}
