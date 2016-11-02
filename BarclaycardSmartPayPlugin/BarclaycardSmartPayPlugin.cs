using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using AuthorizationClientPlatforms;
using AuthorizationClientPlatforms.Plugins;
using AuthorizationClientPlatforms.Plugins.SmartPay;
using AuthorizationClientPlatforms.Logging;

using TransactionManagementCommon;
using MjhGeneral;

namespace AuthorizationClientPlatforms.Plugins
{
    public class BarclaycardSmartPayPlugin : IProcessorPlugin
    {
        //private PaymentPortTypeClient _client;

        private static string endpoint;

        private static bool isTestMode;

        // Only allow TLS 1.1 and higher.
        private static System.Net.SecurityProtocolType _protocolsAccepted = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

        public BarclaycardSmartPayPlugin()
        {
            // Need to force the TLS 1.1+ for security reasons.
            if (_protocolsAccepted != System.Net.ServicePointManager.SecurityProtocol)
            {
                System.Net.ServicePointManager.SecurityProtocol = _protocolsAccepted;
            }
        }

        /// <summary>
        /// Set up any configuration needed for accessing the FIS PayDirect service.
        /// </summary>
        /// <param name="configuration">configuration settings</param>
        public void ModuleInitialize(Dictionary<string, string> configuration)
        {
            isTestMode = false;

            // Only need the endpoint.
            if (configuration.ContainsKey("endpoint"))
            {
                endpoint = configuration["endpoint"];
            }

            // However if the configured endpoint is still bad...
            if (String.IsNullOrEmpty(endpoint))
            {
                // Use the default.
                PaymentPortTypeClient defaultClient = new PaymentPortTypeClient();
                endpoint = defaultClient.Endpoint.Address.Uri.AbsoluteUri;
            }

            // Determine if this is a test port.
            if (-1 != endpoint.IndexOf("https://pal-test", StringComparison.CurrentCultureIgnoreCase))
            {
                isTestMode = true;
            }

            IpsTmsEventSource.Log.LogInformational(String.Format("Looking at endpoint of {0} and test mode of {1}", endpoint, isTestMode));
        }

        public void ModuleShutdown()
        {
        }

        public AuthorizationResponseFields AuthorizePayment(AuthorizationRequest request, AuthorizeMode mode)
        {
            AuthorizationResponseFields response;

            try
            {
                PaymentPortTypeClient client = new PaymentPortTypeClient();

                // Set up the URL based on the configuration.
                client.Endpoint.Address = new System.ServiceModel.EndpointAddress(endpoint);

                // Pass in the merchant credentials.
                client.ClientCredentials.UserName.UserName = request.ProcessorSettings["MerchantID"];
                client.ClientCredentials.UserName.Password = request.ProcessorSettings["MerchantPassword"];

                CreditCardType creditCardType = CreditCardPan.DetermineCreditCardType(request.Pan);

                PaymentRequest parameters = new PaymentRequest()
                {
                    merchantAccount = request.ProcessorSettings["MerchantAccount"],
                    amount = new Amount()
                    {
                        currency = request.ProcessorSettings["CurrencyCode"],
                        value = 199
                    },
                    reference = "TEST-PAYMENT " + DateTime.Now.ToString(),
                    card = new Card()
                    {
                        holderName = "Not Applicable", // Enforce no card holder name.
                        expiryMonth = request.ExpiryDateMMYY.Substring(0, 2),
                        expiryYear = "20" + request.ExpiryDateMMYY.Substring(2, 2),
                        number = request.Pan
                    }
                };

                if (isTestMode)
                {
                    // American Express
                    if (CreditCardType.AmericanExpress == creditCardType)
                    {
                        parameters.card.cvc = "7373";
                    }
                    // Anything else (e.g. Visa/MC).
                    else
                    {
                        parameters.card.cvc = "737";
                    }
                }

                PaymentResult result = client.authorise(parameters);

                AuthorizationResultCode rc = ("Authorised" == result.resultCode) ? AuthorizationResultCode.Approved : AuthorizationResultCode.Declined;

                response = new AuthorizationResponseFields()
                {
                    resultCode = rc, // resultCode
                    authorizationCode = result.authCode, // authorizationCode,
                    cardType = creditCardType.GetDescription(), // cardType,
                    receiptReference = result.pspReference, // receiptReference,
                    note = (AuthorizationResultCode.Approved == rc) ? null : result.refusalReason, //note,
                    //Ttid = (int) (transactionId % 10000000), // ttid, // Put the int version which may be cropped.
                    //BatchNum = (short)(transactionId / 10000000), //batchNum
                    //AdditionalCCFee = ccFee
                };
            }
            catch (Exception e)
            {
                IpsTmsEventSource.Log.LogError(String.Format("Generic exception caught: {0}", e.Message));

                // Rethrow to the caller.
                throw;
            }

            return response;
        }
    }
}
