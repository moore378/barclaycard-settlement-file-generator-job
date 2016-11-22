using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebserverTest.ServiceReference1;
using WebserverTest.ServiceReference2;

namespace WebserverTest
{
    class Program
    {
        static void Main(string[] args)
        {
            WebserverTest.ServiceReference2.SmartPayNotificationServiceClient mService = new WebserverTest.ServiceReference2.SmartPayNotificationServiceClient();

            var response = mService.SmartPayNotification(getXML());

            Console.WriteLine(response);

        }

        private static string getXML()
        {
            XDocument xml =
                XDocument.Load(@"C:\Users\richard.moore\Documents\Repos\transaction-management\BarclacardSmartPayNotificationService\WebserverTest\testXML.xml");
            return xml.ToString();
        }
    }
}
