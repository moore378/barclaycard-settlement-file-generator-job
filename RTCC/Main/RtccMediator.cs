using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RsaUtils;
using TransactionManagementCommon;
using CryptographicPlatforms;
using AuthorizationClientPlatforms;
using System.IO;
using System.Threading;
using Common;
using Rtcc.RtsaInterfacing;
using Rtcc.Database;
using Rtcc.PayByCell;
using System.Diagnostics;
using System.Net;

namespace Rtcc.Main
{

    /// <summary>
    /// The request processor reads in requests from a ClientInterface, and uses the authorizer 
    /// to respond to those requests
    /// </summary>
    // All logic is processed in this one mediator object, because it has access to everything 
    //  in the chain and the ability to log or record state changes from one unified position.
    public class RtccMediator : LoggingObject, IDisposable
    {
        public event EventHandler<TransactionDoneEventArgs> TransactionDone;

        private RtccRequestInterpreter rtccRequestInterpreter;
        private RtsaConnection rtsaConnection;
        private RtccDatabase rtccDatabase;
        private IAuthorizationPlatform monetra;
        private PayByCellClient payByCell;
        private RtccPerformanceCounters.SessionStats performanceCounterSession;
        private static System.Security.Cryptography.MD5 hasher = System.Security.Cryptography.MD5.Create();
        private IAuthorizationPlatform israelPremium;

        public RtccMediator(IAuthorizationPlatform monetra, IAuthorizationPlatform israelPremium,
            RtsaConnection rtsaConnection,
            RtccPerformanceCounters performanceCounters,
            RtccDatabase database = null,
            RtccRequestInterpreter requestInterpreter = null,
            PayByCellClient payByCell = null)
        {
            this.monetra = monetra;
            this.israelPremium = israelPremium;

            this.performanceCounterSession = performanceCounters.NewSession();

            this.rtsaConnection = rtsaConnection;
            this.rtccRequestInterpreter = requestInterpreter ?? SubscribeChild(new RtccRequestInterpreter());
            this.rtccDatabase = database ?? SubscribeChild(new RtccDatabase());
            this.payByCell = payByCell ?? SubscribeChild(new PayByCellClient());

            this.rtsaConnection.MessageReceivedEvent += Rtsa_MessageReceived;
        }

        /// <summary>
        /// This is called when an authorization request comes in from the client and needs to be processed.
        /// </summary>
        /// <param name="request">The request record to process</param>
        public bool ProcessRequest(RawDataMessage requestMessage)
        {
            Stopwatch stopwatch = new Stopwatch();
            ClientAuthRequest request = null;
            UnencryptedStripe unencryptedUnformattedStripe = null;
            TransactionStatus? status = null;
            int? transactionRecordID = null;

            try
            {
                stopwatch.Start();
                LogDetail("Received message");

                // Interpret request message
                request = rtccRequestInterpreter.ParseMessage(requestMessage, "Err: XML InterpretError");

                // Validate request 
                request.Validate("Err: Invalid Authorization Request");

                bool isPreauth = ((request.Flags & 1) == 1);

                // Decrypt the request
                unencryptedUnformattedStripe = decryptStripe(request, "Err: Decryption");

                // Format 
                CreditCardStripe unencryptedStripe = new CreditCardStripe(TrackFormat.FormatSpecialStripeCases(unencryptedUnformattedStripe, request.EncryptionMethod, "Err: Format Stripe"));

                // Validate the credit card stripe
                unencryptedStripe.Validate("Err: Invalid Track Data");

                CreditCardTrackFields creditCardFields;
                CreditCardTracks tracks;
                try
                {
                    // Split the stripe into tracks
                    tracks = unencryptedStripe.SplitIntoTracks("Err: Stripe parse error");

                    // Validate the tracks
                    tracks.Validate("Err: Invalid Track Data");

                    // Split track two into fields
                    creditCardFields = tracks.ParseTrackTwo("Err: Track parse error");

                    // Validate the fields
                    creditCardFields.Validate("Err: Invalid Track Fields");
                }
                catch (ParseException)
                {
                    // Send "decline" to meter
                    SendReplyToClient(new ClientAuthResponse() { Accepted = 0, AmountDollars = 0, ResponseCode="Parse error"});
                    throw;
                }

                // Check the blacklist here

                // Get processor information
                CCProcessorInfo processorInfo = rtccDatabase.GetRtccProcessorInfo(request.TerminalSerialNumber);

                // Insert the record
                status = TransactionStatus.Authorizing;
                transactionRecordID = (int)InsertTransactionRecord("Processing_Live", request, creditCardFields, "RTCC Processing", isPreauth ? TransactionMode.RealtimeDualAuth : TransactionMode.RealtimeNormal, status.Value);

                LogDetail("Sending transaction " + transactionRecordID.ToString() + " to processor");

                IAuthorizationPlatform platform = processorInfo.ClearingPlatform == "ISRAEL-PREMIUM" ? israelPremium : monetra;

                // Add the convenience fee
                request.AmountDollars += processorInfo.CCFee;

                // Perform the authorization
                AuthorizationResponseFields authorizationResponse = AuthorizeRequest(transactionRecordID.Value, request, tracks, unencryptedStripe, creditCardFields, processorInfo, request.UniqueRecordNumber, isPreauth, platform);

                LogImportant("Sending response result for " + transactionRecordID.ToString() + ": " + authorizationResponse.resultCode.ToString());

                // Decide the status according to the response
                TransactionStatus newStatus = StatusFromRespose(authorizationResponse);

                // Record it into the database
                var obscuredPan = creditCardFields.Pan.Obscure(authorizationResponse.resultCode == AuthorizationResultCode.Declined ? CreditCardPan.ObscurationMethod.Hash : CreditCardPan.ObscurationMethod.FirstSixLastFour);
                
                // --- Send a reply to the parking meter ---
                SendReplyToClient(request, authorizationResponse, "Err: Sending client response");

                LogDetail("Updating database for " + transactionRecordID.ToString() + ": " + newStatus.ToString());
                status = newStatus;
                if (isPreauth)
                    rtccDatabase.UpdatePreauth(transactionRecordID.Value, DateTime.Now, authorizationResponse.receiptReference, authorizationResponse.authorizationCode, obscuredPan.ToString(), creditCardFields.ExpDateMMYY, authorizationResponse.cardType, creditCardFields.Pan.FirstSixDigits, creditCardFields.Pan.LastFourDigits, authorizationResponse.BatchNum, authorizationResponse.Ttid, (short)status.Value);
                else
                    rtccDatabase.UpdateLiveTransactionRecord(transactionRecordID.Value, "Processed_Live", newStatus.ToString(), authorizationResponse.authorizationCode, authorizationResponse.cardType, obscuredPan.ToString(), authorizationResponse.BatchNum, authorizationResponse.Ttid, (short)status.Value);

                try
                {
                    //ReceiptService.Transaction objTrans = new ReceiptService.Transaction();
                    //ReceiptService.ValidateCardClient objClient = new ReceiptService.ValidateCardClient();
                    //objTrans.CCHash = ;
                    //objTrans.TransactionRecordID = transactionRecordID.GetValueOrDefault().ToString();
                    //objClient.SubmitReqest(objTrans);

                    string hash = String.Concat(hasher.ComputeHash(Encoding.ASCII.GetBytes(";" + creditCardFields.Pan + "=" + creditCardFields.ExpDateYYMM + "?")).Select(b => b.ToString("X2")));
                    SendToReceiptServer(hash, transactionRecordID.GetValueOrDefault().ToString(), "1");
                }
                catch (Exception e)
                {
                    LogError(e.Message, e);
                }

                // Register the terminal with the Pay-by-cell system if the transaction was approved
                /*if (authorizationResponse.resultCode == AuthorizationResultCode.Approved)
                {
                    if (processorInfo.PoleID != 0)
                    {
                        log("Processing PbC for " + transactionRecordID.ToString(), LogLevel.PerConnectionDebug);

                        payByCell.RegisterTransaction(long.Parse(creditCardFields.Pan.ToString()), processorInfo.PoleID, processorInfo.PoleSerialNumber, request.StartDateTime,
                            (double)request.AmountDollars, request.PurchasedTime, authorizationResponse.authorizationCode, processorInfo.PhoneNumber);
                    }
                    else
                        log("Invalid pole, not registering Pay-by-Cell transaction", LogLevel.PerConnectionImportant);
                }
                else
                    log("Not approved - Skipping Pay-by-Cell transaction", LogLevel.PerConnectionDebug);*/

                if (authorizationResponse.resultCode == AuthorizationResultCode.Approved)
                    performanceCounterSession.ApprovedTransaction();
                else
                    performanceCounterSession.DeclinedTransaction();

                performanceCounterSession.SuccessfulSession(stopwatch.ElapsedTicks);
                return true;
            }
            catch (ParseException exception) // If there is a problem parsing at one of the steps
            {
                LogError("Parse error: " + exception.Message, exception);
                if (status != null && transactionRecordID != null)
                    rtccDatabase.UpdateTransactionStatus(transactionRecordID.Value, status.Value, TransactionStatus.StripeError, exception.FailStatus);
                performanceCounterSession.FailedSession(stopwatch.ElapsedTicks);
                return false;
            }
            catch (Exception exception)
            {
                LogError("Unhandled error during real-time transaction: " + exception.Message, exception);
                performanceCounterSession.FailedSession(stopwatch.ElapsedTicks);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="transactionRecordID"></param>
        /// <param name="mode">1 for RTCC, 2 for CCTM</param>
        private void SendToReceiptServer(string hash, string transactionRecordID, string mode)
        {
            string url = Rtcc.Properties.Settings.Default.ReceiptServer;
            try
            {
                //string url = "http://receipt.ipsmetersystems.com/ValidateCard.svc/SubmitRequest";

                var req = (HttpWebRequest)WebRequest.Create(url);

                req.Method = "POST";

                req.ContentType = "application/xml; charset=utf-8";

                req.Timeout = 30000;

                req.Headers.Add("SOAPAction", url);



                string sXML = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";

                sXML += "<Transaction xmlns=\"http://www.ipsmetersystems.com/ReceiptingSystem\">";

                sXML += "<CCHash>" + hash + "</CCHash>";
                //sXML += "<Mode>" + mode + "</Mode>";

                sXML += "<TransactionRecordID>" + transactionRecordID + "</TransactionRecordID>";

                sXML += "</Transaction>";



                req.ContentLength = sXML.Length;

                System.IO.StreamWriter sw = new System.IO.StreamWriter(req.GetRequestStream());
                
                sw.Write(sXML);

                sw.Close();

                HttpWebResponse webResponse = (HttpWebResponse)req.GetResponse();

                Stream str = webResponse.GetResponseStream();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error sending to receipting server " + url, e);
            }
        }

        private static TransactionStatus StatusFromRespose(AuthorizationResponseFields authorizationResponse)
        {
            switch (authorizationResponse.resultCode)
            {
                case AuthorizationResultCode.Approved: return TransactionStatus.Approved;
                case AuthorizationResultCode.Declined: return TransactionStatus.Declined;
                default: return TransactionStatus.AuthError;
            }
        }

        private void SendReplyToClient(ClientAuthRequest request, AuthorizationResponseFields authorizationResponse, string failStatus)
        {
            // Generate the response object
            ClientAuthResponse response = new ClientAuthResponse
            {
                Accepted = (authorizationResponse.resultCode == AuthorizationResultCode.Approved) ? 1 : 0,
                AmountDollars = request.AmountDollars,
                ReceiptReference = "",
                ResponseCode = authorizationResponse.authorizationCode
            };

            SendReplyToClient(response);
        }

        private void SendReplyToClient(ClientAuthResponse response)
        {
            try
            {
                // Serialize the response to send to the client
                RawDataMessage responseMessage = rtccRequestInterpreter.SerializeResponse(response);

                // Send the response to the client
                rtsaConnection.SendMessage(responseMessage);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Error sending response to client", exception);
            }
        }
        
        private decimal InsertTransactionRecord(
            string tracks,
            ClientAuthRequest request,
            CreditCardTrackFields creditCardFields,
            string statusString,
            TransactionMode mode,
            TransactionStatus status)
        {
            try
            {
                decimal AmountDollars = request.AmountDollars;
                decimal AmountCents = AmountDollars * 100; // Convert to cents

                CreditCardPan.ObscurationMethod panObscurationMethod = CreditCardPan.ObscurationMethod.FirstSixLastFour;
                
                return rtccDatabase.InsertLiveTransactionRecord(
                        request.TerminalSerialNumber, //string TerminalSerNo, 
                        null, //string ElectronicSerNo, 
                        request.TransactionType,  //global::System.Nullable<decimal> TransactionType, 
                        request.StartDateTime,  //global::System.Nullable<global::System.DateTime> StartDateTime, 
                        AmountCents,  //global::System.Nullable<decimal> TotalCredit, 
                        0,  //global::System.Nullable<decimal> TimePurchased, 
                        0,  //global::System.Nullable<decimal> TotalParkingTime, 
                        AmountCents,  //global::System.Nullable<decimal> CCAmount, 
                        tracks,  //string CCTracks, 
                        statusString,  //string CCTransactionStatus, 
                        request.TransactionIndex,  //global::System.Nullable<decimal> CCTransactionIndex, 
                        "0",  //string CoinCount, 
                        (decimal)request.EncryptionMethod,  //global::System.Nullable<decimal> EncryptionVer, 
                        request.KeyVersion,  //global::System.Nullable<decimal> KeyVer, 
                        request.UniqueRecordNumber,  //string UniqueRecordNumber, 
                        request.UniqueNumber2, // long UniqueRecordNumber2, 
                        request.UniqueRecordNumber,  //string CreditCallCardEaseReference, 
                        "",  //string CreditCallAuthCode, 
                        creditCardFields.Pan.Obscure(panObscurationMethod).ToString(), // request.truncatedPan,  //string CreditCallPAN, 
                        creditCardFields.ExpDateYYMM,  //string CreditCallExpiryDate, 
                        "",//string CreditCallCardScheme
                        creditCardFields.Pan.FirstSixDigits,
                        creditCardFields.Pan.LastFourDigits,
                        (short)mode,
                        (short)status
                    );
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Error updating database", exception);
            }
        }

        private AuthorizationResponseFields AuthorizeRequest(
            decimal transactionRecordID, 
            ClientAuthRequest request,
            CreditCardTracks tracks,
            CreditCardStripe stripe,
            CreditCardTrackFields creditCardFields,
            CCProcessorInfo processorInfo,
            string orderNumber,
            bool isPreauth,
            IAuthorizationPlatform platform)
        {
            AuthorizationRequest authorizationRequest = new AuthorizationRequest(
                request.TerminalSerialNumber,
                request.StartDateTime,
                processorInfo.MerchantID,
                processorInfo.MerchantPassword,
                creditCardFields.Pan.ToString(),
                creditCardFields.ExpDateMMYY,
                request.AmountDollars,
                request.TransactionDesc,
                request.Invoice,
                transactionRecordID.ToString(),
                tracks.TrackTwo.ToString(),
                stripe.Data,
                transactionRecordID.ToString(),
                orderNumber,
                null);

            // Perform the authorization and get a response
            AuthorizationResponseFields authorizationResponse = platform.Authorize(authorizationRequest, isPreauth ? AuthorizeMode.Preauth : AuthorizeMode.Normal);

            return authorizationResponse;
        }

        /// <summary>
        /// Decrypts a stripe using the common utilities provided by the TransactionManagementCommon namespace
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The decrypted stripe</returns>
        private static UnencryptedStripe decryptStripe(ClientAuthRequest request, string errorStatus)
        {

            StripeDecryptor decryptor = new StripeDecryptor();
            TransactionInfo info = new TransactionInfo(
                amountDollars: request.AmountDollars,
                meterSerialNumber: request.TerminalSerialNumber,
                startDateTime: request.StartDateTime,
                transactionIndex: request.TransactionIndex,
                refDateTime: request.StartDateTime
            );

            UnencryptedStripe decryptedStripe = decryptor.decryptStripe(request.EncryptedTrack, request.EncryptionMethod, request.KeyVersion, info, errorStatus);
            return new UnencryptedStripe(decryptedStripe);
        }

        private void Rtsa_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            this.rtsaConnection.MessageReceivedEvent -= Rtsa_MessageReceived; // Only one message per mediator

            bool success = ProcessRequest(args.Message);
            var temp = TransactionDone;
            if (temp != null)
                temp(this, new TransactionDoneEventArgs(success));
            Thread.Sleep(2000);
            rtsaConnection.Disconnect(); 
        }
        
        /// <summary>
        /// Given a stripe with track two in a known location, this function will insert the 
        /// beginning and end sentinels for track two. The beginning sentinel will be placed
        /// just before the index specified by trackTwoStart. The end sentinel will be placed
        /// at the first null character after the start.
        /// </summary>
        /// <param name="unencryptedStripe"></param>
        /// <param name="trackTwoStart">The index of the first character in track two</param>
        /// <returns>The stripe with sentinels inserted</returns>
        private UnencryptedStripe InsertSentinels(UnencryptedStripe unencryptedStripe, int trackTwoStart)
        {
            if ((trackTwoStart < 1) || (trackTwoStart > unencryptedStripe.Data.Length - 1))
                throw new ArgumentOutOfRangeException("Cannot insert sentinels, " + trackTwoStart.ToString() + " is not a valid track start");

            if (unencryptedStripe.Data.Length != 128)
                throw new ArgumentException("Invalid stripe");

            string result = unencryptedStripe.Data;

            // Start sentinel
            result = result.Remove(trackTwoStart - 1, 1);
            result = result.Insert(trackTwoStart - 1, ";");

            // End sentinel
            int firstNull = result.IndexOf('\0', trackTwoStart);

            if (firstNull < 0)
                throw new ArgumentException("No place for end sentinel");

            result = result.Remove(firstNull, 1);
            result = result.Insert(firstNull, "?");

            return new UnencryptedStripe(result);
        }

        /// <summary>
        /// This shifts the right part of the string to the right by "shiftBy" chars, padding 
        /// the left with null chars and removing the chars at the right in order to keep the
        /// length the same
        /// </summary>
        /// <param name="unencryptedStripe"></param>
        /// <param name="trackStart">Current start of the track. The new start will be (trackStart + shiftBy)</param>
        /// <param name="shiftBy">Positive integer specifying the amount to shift the track by</param>
        /// <returns>The stripe containing the shifted track</returns>
        private UnencryptedStripe ShiftTrack(UnencryptedStripe unencryptedStripe, int trackStart, uint shiftBy)
        {
            if (unencryptedStripe.Data.Length != 128)
                throw new ArgumentException("Invalid stripe");

            if (trackStart < 0)
                throw new ArgumentOutOfRangeException("Cannot shift track, " + trackStart.ToString() + " is not within stripe");

            if (trackStart + shiftBy > unencryptedStripe.Data.Length - 1)
                throw new ArgumentOutOfRangeException("Cannot shift track beyond end of stripe");

            string result = unencryptedStripe.Data;

            // Insert nulls
            for (int i = 0; i < shiftBy; i++)
                result = result.Insert(trackStart, "\0");

            // Chop off the end
            return new UnencryptedStripe(result.Remove(128));
        }

        public void Dispose()
        {
        }
    }
}
