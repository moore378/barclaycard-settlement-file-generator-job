using System;
using System.Collections.Generic;
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
        static void Main(string[] args)
        {
            try
            {
                // Based on example from http://msdn.microsoft.com/en-us/library/ms731758.aspx

                var baseAddress = new Uri("http://localhost:56341/israelprocessor");
                using (ServiceHost host = new ServiceHost(typeof(IsraelProcessorService), baseAddress))
                {
                    ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                    smb.HttpGetEnabled = true;
                    smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                    host.Description.Behaviors.Add(smb);

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
            this.client = new IsraelPremium.PremiumServiceSoapClient();
            Console.WriteLine("Session created");
        }

        public int AuthCreditCardFull(string premiumUserName, string premiumPassword, string premiumCashierNum, string merchantNumber, string transactionDate_yyyyMMdd, string transactionTime_HHmm, string uniqueTransactionNumber_SixDigits, string track2, string cardNum, string expDate_YYMM, string amount, string cochavAmount, string transactionType, string creditTerms, string currency, string authNum, string code, string firstAmount, string nonFirstAmount, string numOfPayment, string sapakMutav, string sapakMutavNo, string uniqNum, string clubCode, string paramJ, string addData, string eci, string cvv2, string id, string cavvUcaf, string last4Digits, string transactionCurrency, string transactionAmount, out string TransactionRecord, out string ResultRecord)
        {
            Console.WriteLine("AuthCreditCardFull");
            return client.AuthCreditCardFull(premiumUserName, premiumPassword, premiumCashierNum, merchantNumber, transactionDate_yyyyMMdd, transactionTime_HHmm, uniqueTransactionNumber_SixDigits, track2, cardNum, expDate_YYMM, amount, cochavAmount, transactionType, creditTerms, currency, authNum, code, firstAmount, nonFirstAmount, numOfPayment, sapakMutav, sapakMutavNo, uniqNum, clubCode, paramJ, addData, eci, cvv2, id, cavvUcaf, last4Digits, transactionCurrency, transactionAmount, out TransactionRecord, out ResultRecord);
        }
    }
}
