using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon.ControllerBase
{
    public class SimpleServerFactory<ServerType> : ServerFactory<ServerType>
    {
        private Func<ServerType> factoryMethod;

        public SimpleServerFactory(
            Func<ServerType> factoryMethod)
        {
            this.factoryMethod = factoryMethod;
        }

        public override Lazy<ServerType> CreateServer(
            Action initiallyRun = null,
            Action<ServerFactory<ServerType>.Progress> progressChange = null,
            Action<ServerFactory<ServerType>.FactoryResult> finallyRun = null,
            Action<Exception> catchRun = null)
        {
            if (progressChange != null)
                progressChange(Progress.Initializing);
            FactoryResult result = FactoryResult.Error;
            if (initiallyRun != null)
                initiallyRun();
            try
            {
                // Create the server by calling the factory method
                ServerType server = factoryMethod();
                Lazy<ServerType> lazyRef = new Lazy<ServerType>(() => server);
                result = FactoryResult.Created;
                if (progressChange != null)
                    progressChange(Progress.Ready);
                return lazyRef;
            }
            catch (Exception exception)
            {
                if (progressChange != null)
                    progressChange(Progress.Error);
                if (catchRun != null)
                    catchRun(exception);
                throw;
            }
            finally
            {
                if (finallyRun != null)
                    finallyRun(result);
            }
        }
    }
}
