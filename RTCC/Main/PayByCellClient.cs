using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TransactionManagementCommon;

namespace Rtcc.Main
{
    /// <summary>
    /// This class implements a PayByCellClient using the PayByCell web service. 
    /// </summary>
    public class PayByCellClient : LoggingObject
    {
        // The soap client. This is thread-local because I don't know if the service adapters are thread-safe
        private static ThreadLocal<PayByCell.GetActiveParkingInformationSoapClient> client = new ThreadLocal<PayByCell.GetActiveParkingInformationSoapClient>(() => new PayByCell.GetActiveParkingInformationSoapClient());

        /// <summary>
        /// This method will asynchronously register a PayByCell terminal. The registration is done asynchronously, and will return immediately with no indication of success. This will probably only be called when an authorization request
        /// </summary>
        public virtual void RegisterTransaction(long accountNumber, int poleID, string poleSerialNumber, DateTime startDateTime, double amount, int purchasedTime, string authorizationCode, string phoneNumber)
        {
            LogDetail("Processing pay-by-cell request");
            // Add the following work item to the thread pool
            ThreadPool.QueueUserWorkItem(
                (object state) =>
                {
                    try
                    {
                        // Process the command
                        bool result = client.Value.AddParkingForPayByCell(
                            accountNumber,
                            poleID,
                            poleSerialNumber,
                            startDateTime,
                            amount,
                            purchasedTime,
                            authorizationCode,
                            phoneNumber);

                        LogDetail("Pay-by-cell request sent. Reply: " + result);
                    }
                    catch (Exception exception)
                    {
                        LogError("Unable to register Pay-By-Cell transaction. " + exception.Message, exception);
                    }
                }
            );
        }
    }
}
