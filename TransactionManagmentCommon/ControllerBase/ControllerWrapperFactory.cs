using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon.ControllerBase
{
    public class ControllerWrapperFactory<ServerType> where ServerType : class
    {
        private Func<string, Func<ServerType>, ServerFactory<ServerType>> factoryFactoryMethod;
        private Func<ServerController<ServerType>, ControllerWrapper<ServerType>> controllerWrapperFactoryMethod;
        private FailedRestartEventHandler failedRestart;

        public ControllerWrapperFactory(
            Func<string, Func<ServerType>, ServerFactory<ServerType>> factoryFactoryMethod,
            Func<ServerController<ServerType>, ControllerWrapper<ServerType>> controllerWrapperFactoryMethod,
            FailedRestartEventHandler failedRestart = null)
        {
            this.factoryFactoryMethod = factoryFactoryMethod;
            this.controllerWrapperFactoryMethod = controllerWrapperFactoryMethod;
            this.failedRestart = failedRestart;
        }

        public Lazy<ServerType> CreateControllerWrapper(
            string factoryName,
            Func<ServerType> factoryMethod,
            Action<string> statusUpdate
            )
        {
            /* Create a controller to wrap the server.
             * To see why we use a controller, see the ServerController class.
             */
            ServerController<ServerType> controller = new ServerController<ServerType>(
                serverFactory: factoryFactoryMethod(factoryName, factoryMethod),
                updatedStatus: statusUpdate,
                failedRestart: failedRestart
                );

            // Create a wrapper to wrap the controller and present it as if it is the server itself
            ControllerWrapper<ServerType> controllerWrapper = controllerWrapperFactoryMethod(controller);

            /* We return a lazy type because in general the server is not required to exist until it is first used.
             * This may or may not be redundant here, because the wrapper may already provide this indirection, but
             * for compatibility with unwrapped architectures the return value remains lazy.
             */
            return new Lazy<ServerType>(
                () => { return controllerWrapper as ServerType; }
                );
        }
    }
}
