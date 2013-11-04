using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace IsraelProcessor
{
    /*
     * The Israel processor is a stand-alone application that intercedes between the CCTM and the IsraelPremium web service
     */
    class Program
    {
        private static object logLock = new object();

        static void Main(string[] args)
        {
            try
            {
                // Based on example from http://msdn.microsoft.com/en-us/library/ms731758.aspx
                
                var baseAddress = new Uri(Properties.Settings.Default.HostingURL);
                using (ServiceHost host = new ServiceHost(typeof(IsraelProcessorService), baseAddress))
                {
                    ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                    smb.HttpsGetEnabled = true;
                    smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                    host.Description.Behaviors.Add(smb);
                    
                    var contract = ContractDescription.GetContract(typeof(IIsraelProcessorService));
                    var sep = new ServiceEndpoint(contract);
                    sep.Address = new EndpointAddress(baseAddress);
                    var bi = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
                    sep.Binding = bi;
                                        
                    host.AddServiceEndpoint(sep);

                    // Open the ServiceHost to start listening for messages. Since
                    // no endpoints are explicitly configured, the runtime will create
                    // one endpoint per base address for each service contract implemented
                    // by the service.
                    host.Open();

                    Console.WriteLine("The service is ready at {0}", baseAddress);
                    Console.WriteLine("Press <Enter> to stop the service.");
                    Console.ReadLine();

                    // Close the ServiceHost.
                    host.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void Log(string msg)
        {
            var timestamp = DateTime.Now;
            
            string fileName = Path.Combine(Properties.Settings.Default.LogPath, timestamp.ToString("yyyy-MM-dd HH") + ".txt");
            lock(logLock)
            {
                Console.WriteLine(msg);
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                File.AppendAllText(fileName, timestamp.ToString("HH:mm:ss") + ":: " + msg + Environment.NewLine);
            }
        }
    }

    [ServiceContract]
    public interface IIsraelProcessorService
    {
        [OperationContract]
        int AuthCreditCardFull(string premiumUserName, string premiumPassword, string premiumCashierNum, string merchantNumber, string transactionDate_yyyyMMdd, string transactionTime_HHmm, string uniqueTransactionNumber_SixDigits, string track2, string cardNum, string expDate_YYMM, string amount, string cochavAmount, string transactionType, string creditTerms, string currency, string authNum, string code, string firstAmount, string nonFirstAmount, string numOfPayment, string sapakMutav, string sapakMutavNo, string uniqNum, string clubCode, string paramJ, string addData, string eci, string cvv2, string id, string cavvUcaf, string last4Digits, string transactionCurrency, string transactionAmount, out string TransactionRecord, out string ResultRecord);
    }

    public class IsraelProcessorService : IIsraelProcessorService
    {
        private IsraelPremium.PremiumServiceSoapClient client;

        public IsraelProcessorService()
        {
            try
            {
                this.client = new IsraelPremium.PremiumServiceSoapClient();
                Program.Log("Session created");
            }
            catch (Exception e)
            {
                Program.Log(e.ToString());
                throw;
            }
        }

        public int AuthCreditCardFull(string premiumUserName, string premiumPassword, string premiumCashierNum, string merchantNumber, string transactionDate_yyyyMMdd, string transactionTime_HHmm, string uniqueTransactionNumber_SixDigits, string track2, string cardNum, string expDate_YYMM, string amount, string cochavAmount, string transactionType, string creditTerms, string currency, string authNum, string code, string firstAmount, string nonFirstAmount, string numOfPayment, string sapakMutav, string sapakMutavNo, string uniqNum, string clubCode, string paramJ, string addData, string eci, string cvv2, string id, string cavvUcaf, string last4Digits, string transactionCurrency, string transactionAmount, out string TransactionRecord, out string ResultRecord)
        {
            try
            {
                Program.Log("AuthCreditCardFull");
                return client.AuthCreditCardFull(premiumUserName, premiumPassword, premiumCashierNum, merchantNumber, transactionDate_yyyyMMdd, transactionTime_HHmm, uniqueTransactionNumber_SixDigits, track2, cardNum, expDate_YYMM, amount, cochavAmount, transactionType, creditTerms, currency, authNum, code, firstAmount, nonFirstAmount, numOfPayment, sapakMutav, sapakMutavNo, uniqNum, clubCode, paramJ, addData, eci, cvv2, id, cavvUcaf, last4Digits, transactionCurrency, transactionAmount, out TransactionRecord, out ResultRecord);
            }
            catch (Exception e)
            {
                Program.Log(e.ToString());
                throw;
            }
        }
    }
}
