using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace BarclacardSmartPayNotificationService
{
    /// <summary>
    /// Interface defining SmartPayNotification Response service contract
    /// </summary>
    [ServiceContract]
    public interface ISmartPayNotificationService
    {
        /// <summary>
        /// REST service accepting POST information. Input is JSON and the return info is XML. More is specified
        /// on the actual method definition.
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        [OperationContract]
        [WebInvoke(Method = "POST", 
            UriTemplate = "/SendNotification", 
            BodyStyle = WebMessageBodyStyle.Bare, 
            RequestFormat = WebMessageFormat.Json, 
            ResponseFormat = WebMessageFormat.Xml)]
        string SendNotification(Stream notification);
    }
}
