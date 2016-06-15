using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AuthorizationClientPlatforms;
using AuthorizationClientPlatforms.Plugins.PayDirect;
using AuthorizationClientPlatforms.Logging;

using System.Diagnostics;

namespace AuthorizationClientPlatforms.Plugins
{
    public class FisPayDirectPlugin : IProcessorPlugin
    {
        private ApiServiceClient _client;

        private static string _endpoint;

        // Only allow TLS 1.1 and higher.
        private static System.Net.SecurityProtocolType _protocolsAccepted = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

        public FisPayDirectPlugin()
        {
            // Need to force the TLS 1.1+ since the server does not accept anything lesser.
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
            // Only need the endpoint.
            _endpoint = configuration["endpoint"];

            IpsTmsEventSource.Log.LogInformational(String.Format("Looking at endpoint of {0}", _endpoint));
        }

        public void ModuleShutdown()
        {
            if (null != _client)
            {
                _client.Close();

                _client = null;
            }
        }

        #region Authorize Payment

        private AssessFeesResponse AsessFeesForCard(AuthorizationRequest request)
        {
            // Duplicate checking is on amount, Account number last 4, and user part 1.
            // Put the UserPart1.
            string uniqueId = request.IDString;

            // TODO: Put the settlement merchant Code in the "dictionary".
            string settleMerchantCode = request.ProcessorSettings["SettleMerchantCode"];

            string accountUsed;

            // Create the request parameters.
            AssessFeesForCardRequest processorRequest = new AssessFeesForCardRequest()
            {
                MerchantCode = request.ProcessorSettings["MerchantID"],

                ClientTransactionId = uniqueId,

                LineItems = new LineItem[]
                {
                    new LineItem()
                    {
                        ItemAmount = request.AmountDollars,
                        ItemQuantity = 1,
                        LineItemNumber = 1,
                        MerchantAmount = request.AmountDollars,
                        SettleMerchantCode = settleMerchantCode
                    }
                },

                MerchantPassword = request.ProcessorSettings["MerchantPassword"],
                SettleMerchantCode = settleMerchantCode,

                // Set this as the duplicate.
                UserPart1 = uniqueId
            };

            // Set up the card data.
            if (!string.IsNullOrEmpty(request.TrackTwoData))
            {
                processorRequest.TrackTwo = request.TrackTwoData;

                accountUsed = "Track 2";
            }
            else if (!string.IsNullOrEmpty(request.Pan))
            {
                // NOTE: Should never be in this situation due to a meter not allowing manual entry.
                // Only here for testing purposes.
                processorRequest.AccountNumber = request.Pan;

                // Get the month of the expiry.
                processorRequest.ExpirationMonth = int.Parse(request.ExpiryDateMMYY.Substring(0, 2));
                processorRequest.ExpirationYear = int.Parse(request.ExpiryDateMMYY.Substring(2, 2));

                accountUsed = "Account Number";
            }
            else
            {
                // NOTE: this code would never be exercised since RTCC and 
                // CCTM will throw away cards that do not have Track 2.
                // If it ever comes to this situation, then FIS PayDirect
                // should return a decline due to track/account not being
                // populated on the request message.
                accountUsed = "Track 1...";
            }

            // Make the web service call to gather the fee details.
            AssessFeesResponse processorResponse = _client.AssessFeesForCard(processorRequest);

            IpsTmsEventSource.Log.LogInformational(String.Format("Assessing feees returned Token ID {0} for entry method {1}", (null == processorResponse) ? "null" : processorResponse.TokenId, accountUsed));

            return processorResponse;
        }

        private SubmitPaymentResponse SubmitPayment(AuthorizationRequest request, AssessFeesResponse assessFeesResponse)
        {
            SubmitPaymentResponse processorResponse;

            SubmitPaymentRequest processorRequest = new SubmitPaymentRequest()
            {
                MerchantCode = request.ProcessorSettings["MerchantID"],
                ClientTransactionId = request.IDString,
                PaymentChargeTypeCode = assessFeesResponse.PaymentChargeTypePrimary.PaymentChargeTypeCode,
                TokenId = assessFeesResponse.TokenId
            };

            processorResponse = _client.SubmitPayment(processorRequest);

            IpsTmsEventSource.Log.LogInformational(String.Format("Payment ID {0}", processorResponse.PaymentId));

            return processorResponse;
        }

        public AuthorizationResponseFields AuthorizePayment(AuthorizationRequest request, AuthorizeMode mode)
        {
            AuthorizationResponseFields response;

            try
            {
                // Create connection to the server.
                _client = new ApiServiceClient();

                // Set up the URL based on the configuration.
                _client.Endpoint.Address = new System.ServiceModel.EndpointAddress(_endpoint);

                // Cannot be a pre-auth/finalize. Must be a normal sale charge.
                if (AuthorizeMode.Normal != mode)
                {
                    IpsTmsEventSource.Log.LogInformational("Invalid message mode for this processor");

                    // Do not authorize. Send this as a decline.
                    response = new AuthorizationResponseFields
                    (
                    AuthorizationResultCode.Declined,
                    null,
                    null,
                    null,
                    String.Format("Invalid mode {0}", mode),
                    0,
                    0);

                    // For the time being, return back immediately.
                    // NOTE: Maybe throw this as an exception instead...
                    return response;
                }

                // Cheap hack for hardcoding of values for testing purposes.
                /*
                request.ProcessorSettings["MerchantID"] = "50BNA-PUBWK-PARKG-G";
                request.ProcessorSettings["MerchantPassword"] = "test2346";
                request.ProcessorSettings["SettleMerchantCode"] = "50BNA-PUBWK-PARKG-00";
                */
                //request.AmountDollars = 9.98m;
                 

                // Do the first pass.
                AssessFeesResponse assess = AsessFeesForCard(request);

                // There is a token ID when the assess call is successful.
                if (!String.IsNullOrEmpty(assess.TokenId))
                {
                    // Save off the response code.
                    int rc = 0;
                    string paymentMethodCode = "";
                    string reference = "";
                    long transactionId = 0;
                    string authNum = "";
                    decimal ccFee = 0;

                    // Do the second pass immediately after getting the response.
                    SubmitPaymentResponse processorResponse = SubmitPayment(request, assess);

                    // Payment ID identifies the whole transaction.
                    reference = processorResponse.PaymentId.ToString();

                    //Console.WriteLine("Payment ID {0}", processorResponse.PaymentId);

                    foreach (Transaction entry in processorResponse.Transactions)
                    {
                        IpsTmsEventSource.Log.LogInformational(String.Format("Convenience Fee {0} Local Transaction Date Stamp {1} Merchant Amount {2} Message {3} Return Code {4}",
                            entry.ConvenienceFee,
                            entry.LocalTransDateStamp,
                            entry.MerchantAmount,
                            entry.Message,
                            entry.RC));

                        // There _should_ only be one response back, but if any one of the transactions
                        // are declined, then that this should be a full decline.
                        if (0 == rc)
                        {
                            rc = entry.RC;

                            // Cast this to a short.
                            transactionId = entry.TransID;

                            authNum = entry.AuthorizationNumber;

                            ccFee = entry.ConvenienceFee;
                        }
                    }

                    if (0 == rc)
                    {
                        // Note that there should always be a primary value.

                        // Log out the different fees for any future troubleshooting purposes.
                        foreach (PaymentLineItem entry in assess.PaymentChargeTypePrimary.OriginalPaymentLineItems)
                        {
                            IpsTmsEventSource.Log.LogInformational(String.Format("Fee Amount {0} Amount {1} Merchant Amount {2}",
                                entry.FeeAmount,
                                entry.Amount,
                                entry.MerchantAmount));
                        }

                        // Get the card type.
                        // NOTE: Is this supposed to be normalized to the IPS internal format?
                        paymentMethodCode = assess.PaymentChargeTypePrimary.PaymentMethodCode;
                    }

                    // Parse out the response handling.
                    response = new AuthorizationResponseFields() 
                    { 
                        resultCode = (0 == rc) ? AuthorizationResultCode.Approved : AuthorizationResultCode.Declined, // resultCode
                        authorizationCode = authNum, // authorizationCode,
                        cardType = paymentMethodCode, // cardType,
                        receiptReference = reference, // receiptReference,
                        note = transactionId.ToString(), //note,
                        Ttid = (int) (transactionId % 10000000), // ttid, // Put the int version which may be cropped.
                        BatchNum = (short)(transactionId / 10000000), //batchNum
                        AdditionalCCFee = ccFee
                    };
                }
                else
                {
                    // This is a decline for some reason. Log this.
                    IpsTmsEventSource.Log.LogError(String.Format("Decline from Assess Call... {0}", assess.ErrorMessages[0]));

                    // Unknown problem, this is a decline.
                    response = new AuthorizationResponseFields
                        (
                        AuthorizationResultCode.UnknownError,
                        null,
                        null,
                        null,
                        assess.ErrorMessages[0],
                        0,
                        0
                        );
                }
            }
            catch (System.ServiceModel.FaultException e)
            {
                IpsTmsEventSource.Log.LogError(String.Format("FIS PayDirect webservice encountered some error {0}", e.Message));

                response = new AuthorizationResponseFields
                    (
                    AuthorizationResultCode.UnknownError,
                    null,
                    null,
                    null,
                    e.Message,
                    0,
                    0
                    );

            }
            catch (Exception e)
            {
                IpsTmsEventSource.Log.LogError(String.Format("Generic exception caught: {0}", e.Message));

                // Rethrow to the caller.
                throw;
            }
            finally
            {
                if (null != _client)
                {
                    _client.Close();
                }
            }

            return response;
        }

        #endregion
    }
}
