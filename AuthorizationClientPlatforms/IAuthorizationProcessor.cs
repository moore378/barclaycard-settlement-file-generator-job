using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.Runtime.Serialization;

namespace AuthorizationClientPlatforms
{
    [ServiceContract]
    public interface IAuthorizationProcessor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        [OperationContract]
        void Initialize(Dictionary<string, string> configuration);

        /// <summary>
        /// 
        /// </summary>
        [OperationContract]
        void Shutdown();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [OperationContract]
        AuthorizationResponseFields AuthorizePayment(AuthorizationRequest request, AuthorizeMode mode);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        PingResponse Ping();
    }
}
