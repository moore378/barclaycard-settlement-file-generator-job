using System;
using System.Xml.Linq;
using ServiceLogger;
using System.IO;

namespace BarclaycardSmartPayNotificationService
{
    public class SmartPayNotificationService : ISmartPayNotificationService
    {
        /// <summary>
        /// SendNotification is the service name to be used by Barclaycard to send their card response.
        /// The Stream object type is used to handle any type (XML/JSON/text/etc), convert it to a string
        /// and pass it on to the Logger.
        /// </summary>
        /// <param name="notification">POST information sent via WCF</param>
        /// <returns>XML acceptable response</returns>
        public string SendNotification(Stream notification)
        {
            try
            {
                StreamReader reader = new StreamReader(notification);
                string res = reader.ReadToEnd();

                Logger.Instance.LogInformational(res);

                return Response().ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
                // Note, since XML is not being returned, Barclaycard will try again later to send notification.
                return null;
            }
            
        }   

        /// <summary>
        /// The purpose of this method is to provide the acceptable XML response to Barclaycard.
        /// This is the XML they specified in their documentation.
        /// </summary>
        /// <returns>XDocument containing XML for acceptable response</returns>
        private XDocument Response()
        {
            return XDocument.Parse("<sendNotificationResponse xmlns=\"http://notification.services.adyen.com\" xmlns:ns2=\"http://common.services.adyen.com\"><notificationResponse>[accepted]</notificationResponse></sendNotificationResponse>");
        }

    }
}
 