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

        private static bool mapToTestAccount;

        // Only allow TLS 1.1 and higher.
        private static System.Net.SecurityProtocolType _protocolsAccepted = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

        private static CreditCard[] testCards = new CreditCard[]
        {
            new CreditCard()
            {
                CreditCardType = CreditCardType.Visa,
                Pan = "4646464646464644",
                ExpiryDateMMYY = "0818"
            },
            new CreditCard()
            {
                CreditCardType = CreditCardType.AmericanExpress,
                Pan = "370000000000002",
                ExpiryDateMMYY = "0818"
            },
            new CreditCard()
            {
                CreditCardType = CreditCardType.MasterCard,
                Pan = "5555444433331111",
                ExpiryDateMMYY = "0818"
            },
            new CreditCard()
            {
                CreditCardType = CreditCardType.Discover,
                Pan = "36006666333344",
                ExpiryDateMMYY = "0818"
            }
        };

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
                mapToTestAccount = true;
            }

            // MapToTestAccount
            if (configuration.ContainsKey("maptotestaccount"))
            {
                mapToTestAccount = bool.Parse(configuration["maptotestaccount"]);
            }

            IpsTmsEventSource.Log.LogInformational(String.Format("Looking at endpoint of {0}, test mode of {1}, map to test account {2}", endpoint, isTestMode, mapToTestAccount));
        }

        public void ModuleShutdown()
        {
            // Nothing on purpose.
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

                CreditCard requestCard;

                if ((isTestMode)
                    && (mapToTestAccount))
                {
                    requestCard = testCards.Where(t => t.CreditCardType == creditCardType).SingleOrDefault();

                    // If nothing matches, then use the first test card defined.
                    if (null == requestCard)
                    {
                        requestCard = testCards[0];
                    }

                    // Use the right credit card type now that it's a test card.
                    creditCardType = requestCard.CreditCardType;
                }
                else
                {
                    // Use the card from the request.
                    requestCard = new CreditCard()
                    {
                        CreditCardType = creditCardType,
                        Pan = request.Pan,
                        ExpiryDateMMYY = request.ExpiryDateMMYY
                    };
                }

                PaymentRequest parameters = new PaymentRequest()
                {
                    merchantAccount = request.ProcessorSettings["MerchantAccount"],
                    amount = new Amount()
                    {
                        currency = request.ProcessorSettings["CurrencyCode"],
                        value = (long) (request.AmountDollars * 100)
                    },
                    reference = request.IDString,
                    card = new Card()
                    {
                        holderName = "Not Applicable", // Enforce no card holder name.
                        expiryMonth = requestCard.ExpiryDateMMYY.Substring(0, 2),
                        expiryYear = "20" + requestCard.ExpiryDateMMYY.Substring(2, 2),
                        number = requestCard.Pan
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

        class CreditCard
        {
            public CreditCardType CreditCardType;
            public string Pan;
            public string ExpiryDateMMYY;
        }
    }
}
