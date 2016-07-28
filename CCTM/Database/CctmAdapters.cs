using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using Cctm.Common;
using AuthorizationClientPlatforms;
using System.Threading;
using Common;
using TransactionManagementCommon.ControllerBase;

namespace Cctm.Database
{
    /// <summary>
    /// This class is just a group of server controller classes
    /// </summary>
    /// <remarks>
    /// Controllers are used here because they provide a safety and control layer between the
    /// server itself and the CCTM form which uses it. The layer automatically manages the
    /// server, controlling things like initialization, multi-threaded operations, and the
    /// behavior during times of failure (e.g. restating, retrying x number of times etc). In 
    /// order for the controller to police operations, all operations must be done through the
    /// controller class. To integrate this seamlessly and transparently into the CCTM, each 
    /// of these controllers is handled by a wrapper class which implements the original server
    /// interface. All the operations performed on the wrapper are actually performed on the 
    /// server object through the controller object.
    /// </remarks>
    internal class ServerControllers
    {
        /// <summary>
        /// Wraps an authorization controller as an Authorization Platform.
        /// </summary>
        internal class AuthorizerControllerWrapper : ControllerWrapper<IAuthorizationPlatform>, IAuthorizationPlatform
        {
            public AuthorizerControllerWrapper(ServerController<IAuthorizationPlatform> controller)
                : base(controller)
            {

            }

            public AuthorizationResponseFields Authorize(AuthorizationClientPlatforms.AuthorizationRequest request, AuthorizeMode mode)
            {
                return Controller.Perform<AuthorizationResponseFields>(
                    operation: (platform) => platform.Authorize(request, mode),
                    exceptionHandler: (exception, tried) => 
                        {
                            if (exception is AuthorizerProcessingException)
                                if (((AuthorizerProcessingException)exception).AllowRetry && (tried < 5))
                                {
                                    Thread.Sleep(1000);
                                    return OperationFailAction.RestartAndRetry;
                                }

                            return OperationFailAction.AbortAndRestart;
                        }
                    );
            }

            public IAuthorizationStatistics Statistics
            {
                get { return Controller.Get<IAuthorizationStatistics>((platform)=>platform.Statistics); }
            }
        }
    }

    // NOTE: Remove Adapters class since it utilizes the older data set model
    // has been replaced by ICctmDatabase2.
}
