// In it's own project since it'll be good to test it out.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

using System.Reflection;

using System.Diagnostics;

using System.Threading;

using AuthorizationClientPlatforms.Logging;

namespace AuthorizationClientPlatforms
{
    /// <summary>
    /// 
    /// </summary>
    // Since the interface is a WCF service, it's safe to allow multiple threads to be run at a time.
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class AuthorizationProcessor : IAuthorizationProcessor
    {
        protected IProcessorPlugin _plugin;

        protected static bool _initialized;

        /// <summary>
        /// Lock for the different instances accessing the configuration data.
        /// </summary>
        private static ReaderWriterLockSlim _lock;

        /// <summary>
        /// 
        /// </summary>
        public AuthorizationProcessor()
        {
            // Get the configuration for the plugin assembly.
            string assemblyName;
            string className;

            // Read from the configuration to find the plugin to use.
            assemblyName = System.Configuration.ConfigurationManager.AppSettings["PluginAssembly"];
            className = System.Configuration.ConfigurationManager.AppSettings["PluginClass"];

            IpsTmsEventSource.Log.LogInformational(String.Format("Processor plug in loading assembly {0} and class {1}", assemblyName, className));

            // Instantiate the plugin.
            IProcessorPlugin plugin = (IProcessorPlugin)Activator.CreateInstance(assemblyName, className).Unwrap();

            InternalInitialize(plugin);

            IpsTmsEventSource.Log.LogInformational("Processor plug in loaded");
        }

        /// <summary>
        /// Instead of auto instantiating the plugin, use the one provided.
        /// </summary>
        /// <param name="plugin"></param>
        public AuthorizationProcessor(IProcessorPlugin plugin)
        {
            InternalInitialize(plugin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plugin"></param>
        private void InternalInitialize(IProcessorPlugin plugin)
        {
            _plugin = plugin;

            // Only need to create the lock once.
            if (null == _lock)
            {
                _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            }
        }

        public AuthorizationResponseFields AuthorizePayment(AuthorizationRequest request, AuthorizeMode mode)
        {
            AuthorizationResponseFields response;

            _lock.EnterReadLock();

            try
            {
                IpsTmsEventSource.Log.LogInformational(String.Format("Authorizing Transaction Record ID {0} Terminal Serial {1} Amount {2}", 
                    request.IDString, request.MeterSerialNumber, request.AmountDollars));

                if (!_initialized)
                {
                    throw new AuthorizerProcessingException("Not initialized", true);
                }

                response = _plugin.AuthorizePayment(request, mode);

                IpsTmsEventSource.Log.LogInformational(String.Format("Payment authorization response Transaction Record ID {0}, Terminal Serial {1}, Result Code {2}", 
                    request.IDString, request.MeterSerialNumber, response.resultCode));
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return response;
        }

        public void Initialize(Dictionary<string, string> configuration)
        {
            _lock.EnterWriteLock();

            try
            {
                // Initialize.
                IpsTmsEventSource.Log.LogInformational("Initializing processor");

                // Call the plugin to do the true initialization for the processor.
                _plugin.ModuleInitialize(configuration);

                _initialized = true;

                IpsTmsEventSource.Log.LogInformational("Processor initialized");
            }
            catch (Exception e)
            {
                IpsTmsEventSource.Log.LogError(e.Message);

                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Shutdown()
        {
            _lock.EnterWriteLock();

            try
            {
                IpsTmsEventSource.Log.LogInformational("Shutting down processor");

                // Shutdown the processor plugin.
                if (null != _plugin)
                {
                    _plugin.ModuleShutdown();
                }

                // reset the state back to uninitialized.
                _initialized = false;

                IpsTmsEventSource.Log.LogInformational("Processor shut down");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public PingResponse Ping()
        {
            PingResponse response = new PingResponse()
            {
                Initialized = _initialized
            };

            // Heartbeat
            return response;
        }
    }
}
