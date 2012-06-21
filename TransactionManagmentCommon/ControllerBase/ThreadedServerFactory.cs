using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionManagementCommon.ControllerBase
{
    /// <summary>
    /// Creates object using a lazy initialization methodology, except that the initialization is performed immediately but in a separate thread. 
    /// </summary>
    /// <typeparam name="ServerType"></typeparam>
    public class ThreadedServerFactory<ServerType> : ServerFactory<ServerType>
    {
        public readonly string name;
        SimpleServerFactory<ServerType> simpleFactory;

        public override string ToString()
        {
            return name;
        }

        public ThreadedServerFactory(string name, Func<ServerType> factoryMethod)
        {
            this.simpleFactory = new SimpleServerFactory<ServerType>(factoryMethod);
            this.name = name;
        }

        public override Lazy<ServerType> CreateServer(
            Action initiallyRun = null,
            Action<Progress> progressChange = null,
            Action<FactoryResult> finallyRun = null,
            Action<Exception> catchRun = null)
        {
            if (progressChange != null)
                progressChange(Progress.Initial);
            Task<ServerType> serverInitializerTask = new Task<ServerType>(
                () =>
                {
                    return simpleFactory.CreateServer(initiallyRun, progressChange, finallyRun, catchRun).Value;
                },
                TaskCreationOptions.LongRunning // Puts this in another thread
                );
            if (progressChange != null)
                progressChange(Progress.Queued);
            serverInitializerTask.Start();
            return new Lazy<ServerType>(() => { return serverInitializerTask.Result; });
        }
    }
}
