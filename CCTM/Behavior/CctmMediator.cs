using System;
using System.Collections.Generic;
using System.Text;
using TransactionManagementCommon;
using AuthorizationClientPlatforms;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using CryptographicPlatforms;
using Common;
using System.Linq;
using Cctm.Common;
using Cctm.Database;
using System.Diagnostics;
using Cctm.DualAuth;
using MjhGeneral;
using System.Net;
using System.IO;

namespace Cctm.Behavior
{
    public class CctmMediator
    {
        private Action<UpdatedTransactionRecord> processedRecord;
        private Thread mediationThread;
        private ManualResetEvent mediationThreadRun = new ManualResetEvent(true);
        private Action<string> log;
        private Action<string> fileLog;
        private Mutex cycleMutex = new Mutex();
        private Exception mediationException;
        public enum TransactionProcessResult{Successful, Error, Cancelled};
        public Action<IStatistics> StatisticsChanged { get; set; }
        public int PollIntervalSeconds = 5;
        public AsyncSemaphore throttle;
        private DetailedLogDelegate detailedLog;
        private static long totalStarted = 0;
        // NOTE: Removed older data set model for database interfacing since
        // it has been replaced fully by ICctmDatabase2.
        private Dictionary<string, Lazy<IAuthorizationPlatform>> platforms; // Holds all the supported authorization platforms.
        private CctmPerformanceCounters performanceCounters;
        // Removed hasher to use the standard function.

        /// <summary>
        /// List of running or queued transaction processing tasks
        /// </summary>
        private List<Task> runningTasks;

        /// <summary>
        /// List of transaction records IDs for records which are still "New" in the database but are queued for processing.
        /// </summary>
        private List<decimal> queuedRecords = new List<decimal>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database2"></param>
        /// <param name="platforms"></param>
        /// <param name="statisticsChanged">Delegate signalled when there is a change in the statistics record.</param>
        // NOTE: Removed older data set model for database interfacing since
        // it has been replaced fully by ICctmDatabase2.
        public CctmMediator(ICctmDatabase2 cctmDatabase2,
            Dictionary<string, Lazy<IAuthorizationPlatform>> platforms,
            Action<IStatistics> statisticsChanged,
            Action<string> log,
            Action<string> fileLog,
            Action<UpdatedTransactionRecord> processedRecord,
            DetailedLogDelegate detailedLog,
            int maxSimultaneous,
            CctmPerformanceCounters performanceCounters)
        {
            this.database2 = cctmDatabase2;
            this.performanceCounters = performanceCounters;
            this.throttle = new AsyncSemaphore(maxSimultaneous);
            this.platforms = platforms; // collection of authorization platforms.
            this.statistics = new CctmStatistics(statisticsChanged);
            this.statistics.Changed += new Action<IStatistics>((stats) => { if (StatisticsChanged != null) StatisticsChanged(stats); });
            this.runningTasks = new List<Task>();
            this.processedRecord = processedRecord;
            this.log = log;
            this.fileLog = fileLog;
            this.detailedLog = detailedLog;

            // Create a background thread to cycle the mediator
            CreateMediatorThread();
        }
        
        public async Task ProcessTransaction(DbTransactionRecord dbTransactionRecord, DateTime taskQueuedTime)
        {
            //TransactionRecord transactionRecord = DbTransactionRecordToTransactionRecord(dbTransactionRecord);

            AuthorizationResponseFields authorizationResponse = new AuthorizationResponseFields(AuthorizationResultCode.UnknownError, "Not processed", "Not processed", "Not processed", "Not processed", 0, 0);
            CreditCardTrackFields creditCardFields = new CreditCardTrackFields() { ExpDateYYMM = "????", Pan = new CreditCardPan() { }, ServiceCode = "", CardType = CardType.Unknown };
            CreditCardStripe unencryptedStripe = new CreditCardStripe("");
            CreditCardPan obscuredPan = new CreditCardPan();
            
            // Check if we're supposed to be running new transactions
            if (!mediationThreadRun.WaitOne(0))
            {
                statistics.AbortQueued();
                // The transaction is no longer queued
                lock(queuedRecords)
                    queuedRecords.Remove(dbTransactionRecord.TransactionRecordID);
                // Raise event that it was cancelled
                OnCompletedTransaction(ref dbTransactionRecord, TransactionProcessResult.Cancelled);
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var tranasctionPerformanceCounters = performanceCounters.StartingTransaction())
            {
                AuthorizeMode mode;
                // What mode?
                if (dbTransactionRecord.AuthStatus == (short)TransactionStatus.ReadyToProcessNew)
                    mode = AuthorizeMode.Preauth;
                else if (dbTransactionRecord.AuthStatus == (short)TransactionStatus.Approved)
                    mode = AuthorizeMode.Finalize;
                else
                    mode = AuthorizeMode.Normal;
                bool isPreAuth = mode == AuthorizeMode.Preauth;

                Interlocked.Increment(ref totalStarted);
                //if (Interlocked.Read(ref totalStarted) > 4)
                //    Stop(); // Stop future transactions if this is the 5th one

                await throttle.WaitAsync();

                TransactionProcessResult result = TransactionProcessResult.Error; // By default the transaction is unsuccessful... if it completes it will be set to successful
                //string originalTransactionStatus = transactionRecord.Status; // Keep the status because it affects which processor to use

                DateTime taskStart = DateTime.Now;
                try
                {
                    // Update the statistics
                    statistics.NewProcessing(new TimeSpan(DateTime.Now.Ticks - taskQueuedTime.Ticks));

                    try
                    {
                        // Update the record to be "processing"
                        await UpdateTransactionStatus(dbTransactionRecord, TransactionStatus.Processing, isPreAuth);
                    }
                    finally
                    {
                        // The transaction is no longer queued
                        lock (queuedRecords)
                            queuedRecords.Remove(dbTransactionRecord.TransactionRecordID);
                    }

                    CreditCardTracks tracks;
                    Int64 hash = 0;

                    // Finalize doesnt have a track
                    if (mode != AuthorizeMode.Finalize)
                    {
                        if (isPreAuth)
                        {
                            dbTransactionRecord.CCTransactionIndex = dbTransactionRecord.AuthCCTransactionIndex ?? dbTransactionRecord.CCTransactionIndex;
                            //dbTransactionRecord.ReferenceDateTime = dbTransactionRecord.
                            if (dbTransactionRecord.AuthCCAmount == null)
                                throw new InvalidOperationException("Preauth amount is null");
                            dbTransactionRecord.CCAmount = dbTransactionRecord.AuthCCAmount.Value;
                        }

                        EncryptedStripe encryptedDecodedStripe;
                        try
                        {
                            string s = dbTransactionRecord.CCTracks;
                            if (s.Contains("Processing") | s.Contains("CCTM"))
                                encryptedDecodedStripe = new EncryptedStripe(new byte[0]);
                            else
                                encryptedDecodedStripe = DatabaseFormats.DecodeDatabaseStripe(s);
                        }
                        catch
                        {
                            encryptedDecodedStripe = new EncryptedStripe(new byte[0]);
                        }

                        // Decrypt the request
                        var decryptedStripe = DecryptStripe(dbTransactionRecord, encryptedDecodedStripe, "Err: Decryption");

                        // Special cases (E.g. RSA track data shifted, sentinels missing etc)
                        var formattedStripe = TrackFormat.FormatSpecialStripeCases(decryptedStripe, (EncryptionMethod)dbTransactionRecord.EncryptionVer, "Err: Format Stripe");

                        unencryptedStripe = new CreditCardStripe(formattedStripe);

                        // Validate the credit card stripe
                        unencryptedStripe.Validate("Err: Invalid Track Data");

                        // Split the stripe into tracks
                        tracks = unencryptedStripe.SplitIntoTracks("Err: Stripe parse error");

                        // Validate the tracks
                        tracks.Validate("Err: Invalid Track Data");

                        // Split track two into fields
                        if (dbTransactionRecord.CCClearingPlatform.ToUpper() != "ISRAEL-PREMIUM"
                            || !TrackFormat.ParseTrackTwoIsraelSpecial(tracks.TrackTwo, "Err: Israel track parse error", out creditCardFields))
                            creditCardFields = TrackFormat.ParseTrackTwoIso7813(tracks.TrackTwo, "Err: Track parse error");

                        // Validate the fields
                        creditCardFields.Validate("Err: Invalid Track Fields");

                        // If this isn't a normal card, then we have an exceptional circumstance of this being a special card
                        if (creditCardFields.CardType != CardType.Normal && creditCardFields.CardType != CardType.IsraelSpecial)
                            throw new SpecialCardException();

                        // Calculate the hash for the receipt server
                        // Get the hash from the account number.
                        hash = CCCrypt.HashPANToInt64(creditCardFields.Pan.ToString());
                    }
                    else
                    {
                        tracks = new CreditCardTracks();

                        // Calculate the hash for the receipt server
                        // Get the hash as it was from the DB.
                        if (dbTransactionRecord.CCHash.HasValue)
                        {
                            hash = dbTransactionRecord.CCHash.Value;
                        }
                    }

                    // Choose the authorization platform
                    IAuthorizationPlatform authorizationPlatform = ChooseAuthorizationPlatform(dbTransactionRecord, "Err: Clearing platform");

                    // Update the record to be authorizing
                    await UpdateTransactionStatus(dbTransactionRecord, TransactionStatus.Authorizing, isPreAuth);

                    // Perform the authorization
                    authorizationResponse = AuthorizeRequest(dbTransactionRecord, tracks, unencryptedStripe, creditCardFields, authorizationPlatform, dbTransactionRecord.UniqueRecordNumber, mode, dbTransactionRecord.AuthTTID.Transform(a => (int?)a));

                    // Decide the status according to the response and update the database
                    TransactionStatus newStatus = (TransactionStatus)dbTransactionRecord.Status;
                    switch (authorizationResponse.resultCode)
                    {
                        case AuthorizationResultCode.Approved: newStatus = TransactionStatus.Approved; break;
                        case AuthorizationResultCode.ConnectionError: newStatus = TransactionStatus.Processing; break;
                        case AuthorizationResultCode.Declined: newStatus = TransactionStatus.Declined; break;
                        case AuthorizationResultCode.UnknownError: newStatus = TransactionStatus.AuthError; break;
                    }
                    //UpdateTransactionStatus(ref transactionRecord, newStatus, isPreAuth);

                    obscuredPan = new CreditCardPan();

                    if (mode != AuthorizeMode.Finalize) // Pan is not valid in a finalization
                    {
                        // Obscure the credit card PAN
                        switch (authorizationResponse.resultCode)
                        {
                            case AuthorizationResultCode.Approved:
                                obscuredPan = creditCardFields.Pan.Obscure(CreditCardPan.ObscurationMethod.FirstSixLastFour);
                                break;
                            case AuthorizationResultCode.Declined:
                                obscuredPan = creditCardFields.Pan.Obscure(CreditCardPan.ObscurationMethod.Hash);
                                break;
                            default:
                                obscuredPan = creditCardFields.Pan.Obscure(CreditCardPan.ObscurationMethod.FirstSixLastFour);
                                break;
                        }
                    }

                    string newTrackText = null;
                    // Update the track only if a definite response from authorizer
                    if ((authorizationResponse.resultCode == AuthorizationResultCode.Approved) || (authorizationResponse.resultCode == AuthorizationResultCode.Declined))
                    {
                        newTrackText = "CCTM2 - " + authorizationResponse.note;
                    }

                    // Send the updated record information to the database
                    UpdatedTransactionRecord updatedRecord = new UpdatedTransactionRecord()
                    {
                        AuthorizationCode = authorizationResponse.authorizationCode,
                        CardEaseReference = authorizationResponse.receiptReference, // Optional. Some processors do not support sending these for declines.
                        CardScheme = authorizationResponse.cardType, // Optional. Some processors do not support sending these for declines.
                        ExpiryDate = (mode != AuthorizeMode.Finalize) ? creditCardFields.ExpDateYYMM : "",
                        FirstSix = (mode != AuthorizeMode.Finalize) ? creditCardFields.Pan.FirstSixDigits : "",
                        LastFour = (mode != AuthorizeMode.Finalize) ? creditCardFields.Pan.LastFourDigits : "",
                        PAN = obscuredPan.ToString(),
                        TransactionRecordID = (int)dbTransactionRecord.TransactionRecordID,
                        BatchNum = authorizationResponse.BatchNum,
                        Ttid = authorizationResponse.Ttid,
                        Status = newStatus, //isPreAuth ? (transactionRecord.PreauthStatus ?? transactionRecord.Status) : transactionRecord.Status,
                        TrackText = newTrackText ?? dbTransactionRecord.CCTracks
                    };

                    // Record it into the database
                    switch (mode)
                    {
                        case AuthorizeMode.Preauth:
                            await database2.UpdTransactionauthorization(new DbUpdTransactionauthorizationParams
                                {
                                    TransactionRecordID = dbTransactionRecord.TransactionRecordID,
	                                SettlementDateTime = DateTime.Now,
	                                CreditCallCardEaseReference = updatedRecord.CardEaseReference,
	                                CreditCallAuthCode = updatedRecord.AuthorizationCode,
	                                CreditCallPAN = updatedRecord.PAN,
	                                CreditCallExpiryDate = updatedRecord.ExpiryDate,
	                                CreditCallCardScheme = updatedRecord.CardScheme,
	                                CCFirstSix = updatedRecord.FirstSix,
	                                CCLastFour = updatedRecord.LastFour,
	                                BatNum = updatedRecord.BatchNum,
	                                TTID = updatedRecord.Ttid,
	                                Status = (short)updatedRecord.Status,
                                    CCHash = hash
                                }); 
                            break;
                        case AuthorizeMode.Finalize: 
                            await database2.UpdTransactionrecordCctmFinalization(new DbUpdTransactionrecordcctmFinalizationParams
                                {
                                    BatNum = authorizationResponse.BatchNum,
                                    CCTrackStatus = newTrackText,
                                    CCTransactionStatus = newStatus.ToText(),
                                    CreditCallAuthCode = authorizationResponse.authorizationCode,
                                    CreditCallCardEaseReference = authorizationResponse.receiptReference,
                                    OldStatus = (short)dbTransactionRecord.Status, 
                                    Status = (short)newStatus,
                                    TransactionRecordID = dbTransactionRecord.TransactionRecordID,
                                    TTID = authorizationResponse.Ttid
                                });

                            break;
                        default:
                            await database2.UpdTransactionrecordCctm(new DbUpdTransactionrecordCctmParams
                            {
                                TransactionRecordID = dbTransactionRecord.TransactionRecordID,
                                CreditCallCardEaseReference = updatedRecord.CardEaseReference,
                                CCTrackStatus = updatedRecord.TrackText,
                                CreditCallAuthCode = updatedRecord.AuthorizationCode,
	                            CreditCallPAN = updatedRecord.PAN,
	                            CreditCallExpiryDate = updatedRecord.ExpiryDate,
	                            CreditCallCardScheme = updatedRecord.CardScheme,
	                            CCFirstSix = updatedRecord.FirstSix,
	                            CCLastFour = updatedRecord.LastFour,
	                            CCTransactionStatus = updatedRecord.Status.ToText(),
	                            BatNum = updatedRecord.BatchNum,
	                            TTID = updatedRecord.Ttid,
	                            Status = (short)updatedRecord.Status,
	                            OldStatus = dbTransactionRecord.Status,
                                // When the processor charges an extra credit 
                                // card fee and that value was not reflected
                                // with the requested amount, pass in the value
                                // as a negative number to trigger the 
                                // database to update the total card and credit
                                // charge values.
                                CCFee = (short) (authorizationResponse.AdditionalCCFee * -100m),
                                CCHash = hash
                            });
                            break;
                    }

                    dbTransactionRecord.Status = (short)updatedRecord.Status;
                    UpdatedTransaction(dbTransactionRecord);

                    processedRecord(updatedRecord);

                    fileLog(
                        "ID: " + dbTransactionRecord.TransactionRecordID
                        + "; Result: " + authorizationResponse.resultCode.ToString()
                        + "; Notes: " + authorizationResponse.note
                        );

                    if ((authorizationResponse.resultCode == AuthorizationResultCode.Approved)
                        && (0 != hash)) // Only send to the receipt server if there is a hash value.
                    {
                        try
                        {
                            //ReceiptService.Transaction objTrans = new ReceiptService.Transaction();
                            //ReceiptService.ValidateCardClient objClient = new ReceiptService.ValidateCardClient();
                            //objTrans.CCHash = ;
                            //objTrans.TransactionRecordID = transactionRecordID.GetValueOrDefault().ToString();
                            //objClient.SubmitReqest(objTrans);
                            SendToReceiptServer(hash.ToString(), dbTransactionRecord.TransactionRecordID.ToString(), "1");
                        }
                        catch (Exception e)
                        {
                            fileLog(e.ToString());
                        }
                    }

                    switch (authorizationResponse.resultCode)
                    {
                        case AuthorizationResultCode.Approved:
                        case AuthorizationResultCode.Declined:
                            result = TransactionProcessResult.Successful;

                            break;
                        default:
                            result = TransactionProcessResult.Error;
                            break;
                    }

                    if (result == TransactionProcessResult.Error)
                        tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);
                    else
                        tranasctionPerformanceCounters.SuccessfulTransaction(stopwatch.ElapsedTicks, authorizationResponse.resultCode == AuthorizationResultCode.Approved);
                }
                catch (SpecialCardException exception)
                {
                    fileLog(dbTransactionRecord.TransactionRecordID + " " + exception.ToString());
                    UpdateTransactionStatus(dbTransactionRecord, TransactionStatus.StripeError, isPreAuth).Wait();
                    tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);
                }
                catch (StripeErrorException exception)
                {
                    fileLog(dbTransactionRecord.TransactionRecordID + " " + exception.ToString());
                    // Update the fail status
                    UpdateTransactionStatus(dbTransactionRecord, TransactionStatus.StripeError, isPreAuth).Wait();
                    tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);

                }
                catch (AuthorizerProcessingException exception)
                {
                    fileLog(dbTransactionRecord.TransactionRecordID + " " + exception.ToString());
                    UpdateTransactionStatus(dbTransactionRecord, TransactionStatus.AuthError, isPreAuth).Wait();
                    tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);
                }
                catch (Exception exception)
                {
                    fileLog(dbTransactionRecord.TransactionRecordID + " " + exception.ToString());
                    tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);
                }
                finally
                {
                    if (authorizationResponse.resultCode != AuthorizationResultCode.Approved)
                        detailedLog(DateTime.Now.ToString(), ((TransactionStatus)dbTransactionRecord.Status).ToText(), dbTransactionRecord.CCTracks, unencryptedStripe.ToString(), creditCardFields.ExpDateMMYY, dbTransactionRecord.StartDateTime.ToString(), obscuredPan.ToString(), dbTransactionRecord.TransactionRecordID.ToString(), "", dbTransactionRecord.EncryptionVer.ToString(), dbTransactionRecord.KeyVer.ToString(), dbTransactionRecord.CCAmount.ToString(), dbTransactionRecord.CCTransactionIndex.ToString(), dbTransactionRecord.UniqueRecordNumber, dbTransactionRecord.TerminalSerNo, authorizationResponse.note);

                    if (result == TransactionProcessResult.Successful)
                        statistics.NewSuccessful(new TimeSpan(DateTime.Now.Ticks - taskStart.Ticks));
                    else
                        statistics.NewFailure(new TimeSpan(DateTime.Now.Ticks - taskStart.Ticks));
                    OnCompletedTransaction(ref dbTransactionRecord, result);

                    throttle.Release();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="transactionRecordID"></param>
        /// <param name="mode">1 for RTCC, 2 for CCTM</param>
        private static void SendToReceiptServer(string hash, string transactionRecordID, string mode)
        {
            string url = Properties.Settings.Default.ReceiptServer;
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

        public delegate void DetailedLogDelegate(string datetime, string status, string encryptedTrack,
            string track, string expDate, string startDateTime, string pan, string transactionId, string authCode,
            string encVer, string keyVer, string ccAmount, string ccTransactionIndex, string uniqueRecordNum,
            string terminalserno, string note);

        

        private void CreateMediatorThread()
        {
            mediationThread = new Thread(
                () =>
                {
                    try
                    {
                        while (true)
                        {
                            mediationThreadRun.WaitOne();
                            RunCycle();
                            Thread.Sleep(PollIntervalSeconds * 1000);
                        }
                    }
                    catch (Exception exception)
                    {
                        mediationException = exception;
                        this.log("Fatal Exception! Mediation thread terminated! " + exception.ToString());
                    }
                }
            );
            mediationThread.Name = "MediatorBackgroundThread";
            mediationThread.IsBackground = true;
            mediationThread.Priority = ThreadPriority.BelowNormal;
            mediationThread.Start();

            Thread dequeueTasksThread = new Thread(new ThreadStart(DequeueTasksThread));
            dequeueTasksThread.Name = "TransactionDequeueThread";
            dequeueTasksThread.IsBackground = true;
            dequeueTasksThread.Start();
        }

        /// <summary>
        /// Allow continuation of the mediation thread (which polls the database)
        /// </summary>
        public void Start()
        {
            mediationThreadRun.Set();
        }

        /// <summary>
        /// Stop the mediation thread (which polls the database)
        /// </summary>
        public void Stop()
        {
            mediationThreadRun.Reset();
        }

        private CctmStatistics statistics;
        public IStatistics Statistics { get { return statistics; } }

        public void Initialize()
        {
            LoadSettings();
        }

        protected virtual void OnStartingTransaction(ref DbTransactionRecord record)
        {
            if (StartingTransaction != null)
                StartingTransaction(record);
        }

        protected virtual void OnCompletedTransaction(ref DbTransactionRecord record, TransactionProcessResult successful)
        {
            if (CompletedTransaction != null)
                CompletedTransaction(record, successful);
        }

        private async Task UpdateTransactionStatus(DbTransactionRecord record, TransactionStatus newStatus, bool preAuth)
        {
            if (!preAuth)
            {
                // Update the database
                if (await database2.UpdTransactionrecordStatus(record.TransactionRecordID,  newStatus.ToText(), (short)newStatus, (short)record.Status) == (short)record.Status)
                {
                    record.Status = (short)newStatus;
                    // Raise the event
                    if (UpdatedTransaction != null)
                        UpdatedTransaction(record);
                }
                else
                    throw new InvalidOperationException("Could not update record status");
            }
            else
            {
                // Update the database
                if (await database2.UpdTransactionauthorizationStatus(record.TransactionRecordID, (short)newStatus, record.AuthStatus??0) == record.AuthStatus)
                {
                    record.AuthStatus = (short)newStatus;
                    // Raise the event
                    if (UpdatedTransaction != null)
                        UpdatedTransaction(record);
                }
                else
                    throw new InvalidOperationException("Could not update record status");
            }
        }

        private Queue<Task> transactionQueue = new Queue<Task>();
        private ManualResetEvent transactionQueueNotEmpty = new ManualResetEvent(false);

        public static int transactionCount = 0;
        public static List<Task> transactionTasks = new List<Task>();
        public static Semaphore transactionLimitor = new Semaphore(30, 30);

        /// <summary>
        /// This runs in a separate thread, and takes tasks out of the queue, and creates tasks for them
        /// </summary>
        public void DequeueTasksThread()
        {
            
            while (true)
            {
                while (!transactionLimitor.WaitOne(5000));
                
                Interlocked.Increment(ref transactionCount);
                // Wait for transactions
                transactionQueueNotEmpty.WaitOne();
                lock (transactionQueue)
                {
                    if (transactionQueue.Count > 0)
                    {
                        Task transactionTask = transactionQueue.Dequeue();
                        transactionTasks.Add(transactionTask);
                        transactionTask.ContinueWith((task) => { Interlocked.Decrement(ref transactionCount); transactionTasks.Remove(task); transactionLimitor.Release(); });
                        transactionTask.Start();
                    }
                    else
                    {
                        transactionLimitor.Release();
                        transactionQueueNotEmpty.Reset();
                    }
                }
            }
        }

        /// <summary>
        /// Acquires transactions from the database and queues them for processing
        /// </summary>
        public void RunCycle()
        {
            cycleMutex.WaitOne();
            try
            {
                List<DbTransactionRecord> newRecords;

                List<decimal> previouslyQueuedRecords = new List<decimal>(queuedRecords.Count); // List of queued records from before SelectNewTransactionRecords starts
                lock (queuedRecords)
                    previouslyQueuedRecords.AddRange(queuedRecords);

                IEnumerable<DbTransactionRecord> transactionRecords = database2.SelNewTransactionrecords().Result;
                lock (queuedRecords)
                {
                    // List of transactions that are actually "new" (not in queue already)
                    newRecords = (from transaction in transactionRecords
                                  where (previouslyQueuedRecords.IndexOf(transaction.TransactionRecordID) == -1)
                                    && (queuedRecords.IndexOf(transaction.TransactionRecordID) == -1)
                                  select transaction).ToList();
                    if (newRecords.Count() > 0)
                        // Add the IDs of all the new transactions
                        queuedRecords.AddRange(from r in newRecords select r.TransactionRecordID);
                }

                // For each transaction that isn't already queued
                for (int i = 0; i < newRecords.Count; i++)
                {
                    var transactionRecord = newRecords[i];

                    statistics.NewQueued();

                    // Flag the record as queued (so we dont access it again before the status has been updated in the database)


                    // Add it to the task list
                    DateTime queueTime = DateTime.Now;
                    // Use async / await lambda expression to allow the task to only complete when the method finishes.
                    Task transactionTask = new Task(async () => await ProcessTransaction(transactionRecord, queueTime));
                    lock (runningTasks) { runningTasks.Add(transactionTask); }
                    // It'll need to be removed when it's done
                    transactionTask.ContinueWith((task) =>
                    {
                        lock (runningTasks)
                        {
                            runningTasks.Remove(task);
                        }
                    });
                    // Raise the event
                    OnStartingTransaction(ref transactionRecord);
                    // Queue the task for starting
                    lock (transactionQueue)
                    {
                        transactionQueue.Enqueue(transactionTask);
                        transactionQueueNotEmpty.Set();
                    }
                }
            }
            catch (Exception exception)
            {
                log(exception.ToString());
            }
            finally
            {
                cycleMutex.ReleaseMutex();
            }
        }
        
        public void WaitForTransactionsToComplete()
        {
            Task.WaitAll(runningTasks.ToArray());
            if (mediationException != null)
                throw mediationException;
        }

        /// <summary>
        /// Determine the authorization platform for a single transaction.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="failStatus"></param>
        /// <returns>Authorization Platform associated to the customer for the transaction.</returns>
        private IAuthorizationPlatform ChooseAuthorizationPlatform(
            DbTransactionRecord transaction,
            string failStatus)
        {
            IAuthorizationPlatform authorizationPlatform = null;

            // Holder for an authorization platform since it's not created until it's first used.
            Lazy<IAuthorizationPlatform> authorizationPlatformEntry;

            // Verify that the authorization platform is configured in the system.
            platforms.TryGetValue(transaction.CCClearingPlatform, out authorizationPlatformEntry);
            if (null == authorizationPlatformEntry)
            {
                throw new StripeErrorException("Clearing platform not supported: \"" + transaction.CCClearingPlatform + "\"", failStatus);
            }

            // Get the actual authorization platform.
            authorizationPlatform = authorizationPlatformEntry.Value;

            return authorizationPlatform;
        }

        private AuthorizationResponseFields AuthorizeRequest(DbTransactionRecord transaction, CreditCardTracks tracks, CreditCardStripe unencryptedStripe, CreditCardTrackFields creditCardFields,
            IAuthorizationPlatform authorizationPlatform, string orderNumber, AuthorizeMode mode, int? preauthTtid)
        {
            Dictionary<string, string> processorSettings = new Dictionary<string, string>();

            AuthorizationRequest authorizationRequest = new AuthorizationRequest(
                transaction.TerminalSerNo,
                transaction.StartDateTime,
                transaction.CCTerminalID,
                transaction.CCTransactionKey,
                creditCardFields.Pan.ToString(),
                creditCardFields.ExpDateMMYY,
                transaction.CCAmount,
                "",
                transaction.TransactionRecordID.ToString(),
                transaction.TransactionRecordID.ToString(),
                tracks.TrackTwo.ToString(),
                unencryptedStripe.Data,
                transaction.TransactionRecordID.ToString(),
                orderNumber,
                preauthTtid);

            // Normalize the extra processor settings since they are 
            // overloaded and have different meanings based on the processor.
            switch (transaction.CCClearingPlatform.ToLower())
            {
                case "israel-premium":
                    authorizationRequest.ProcessorSettings["MerchantNumber"] = transaction.MerchantNumber;
                    authorizationRequest.ProcessorSettings["CashierNumber"] = transaction.CashierNumber;
                    break;
                case "fis-paydirect":
                    authorizationRequest.ProcessorSettings["SettleMerchantCode"] = transaction.MerchantNumber;
                    break;
                case "barclaycard-smartpay":
                    authorizationRequest.ProcessorSettings["MerchantAccount"] = transaction.MerchantNumber;
                    authorizationRequest.ProcessorSettings["CurrencyCode"] = transaction.CashierNumber;
                    break;
                case "monetra":
                    // Falls through on purpose.
                default:
                    break;
            }

            // Perform the authorization and get a response
            AuthorizationResponseFields authorizationResponse = authorizationPlatform.Authorize(authorizationRequest, mode);

            return authorizationResponse;
        }

        /// <summary>
        /// Decrypts a stripe
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="errorStatus"></param>
        /// <returns></returns>
        /// <exception cref="StripeErrorException"></exception>
        private UnencryptedStripe DecryptStripe(DbTransactionRecord transaction, EncryptedStripe stripe, string errorStatus)
        {
            StripeDecryptor decryptor = new StripeDecryptor();
            TransactionInfo info = new TransactionInfo
            (
                amountDollars: transaction.CCAmount,
                meterSerialNumber: transaction.TerminalSerNo,
                startDateTime: transaction.StartDateTime,
                transactionIndex: (int)transaction.CCTransactionIndex,
                refDateTime: transaction.ReferenceDateTime
            );

            string decryptedStripe = decryptor.decryptStripe(stripe, (EncryptionMethod)transaction.EncryptionVer, (int)transaction.KeyVer, info, errorStatus);
            return new UnencryptedStripe(decryptedStripe);
        }

        private void LoadSettings()
        {
            throw new NotImplementedException();
        }

        private void ReadNewAuthRequestsFromDB()
        {
            throw new NotImplementedException();
        }
        
        public interface IStatistics
        {
            TimeSpan AverageQueueTime { get; }
            TimeSpan AverageTaskTime { get; }
            int FailedCount { get; }
            int ProcessingCount { get; }
            int QueuedCount { get; }
            int SuccessfulCount { get; }
            TimeSpan TotalQueueTime { get; }
            TimeSpan TotalTaskTime { get; }
        }

        private class CctmStatistics: IStatistics
        {
            public CctmStatistics(Action<CctmStatistics> changed)
            {
                this.Changed = changed;
            }
            public Action<CctmStatistics> Changed { get; set; }

            private int CompletedCount { get { return failedCount + successfulCount; } }

            /// <summary>
            /// The number of transactions queued for processing
            /// </summary>
            public int QueuedCount { get { return queuedCount; } }
            private int queuedCount = 0;

            /// <summary>
            /// The number of transactions busy processing
            /// </summary>
            public int ProcessingCount { get { return processingCount; } }
            private int processingCount = 0;

            /// <summary>
            /// Number of transactions completed successfully
            /// </summary>
            public int SuccessfulCount { get { return successfulCount; } }
            private int successfulCount = 0;

            /// <summary>
            /// Number of transactions completed unsuccessfully
            /// </summary>
            public int FailedCount { get { return failedCount; } }
            private int failedCount = 0;

            /// <summary>
            /// Total amount of time spent doing the transactions. Measured as sum of individual periods. 
            /// </summary>
            public TimeSpan TotalTaskTime { get { return totalTaskTime; } }
            private TimeSpan totalTaskTime = new TimeSpan(0);

            /// <summary>
            /// Average amount of time spent doing the transactions.
            /// </summary>
            public TimeSpan AverageTaskTime { get { return TimeSpan.FromTicks(CompletedCount > 0 ? TotalTaskTime.Ticks / CompletedCount : 0); } }


            /// <summary>
            /// Total amount of time transactions have spent in the queue. Measured as sum of individual periods. 
            /// </summary>
            public TimeSpan TotalQueueTime { get { return totalQueueTime; } }
            private TimeSpan totalQueueTime = new TimeSpan(0);

            /// <summary>
            /// Average time a transaction has waited to be processed.
            /// </summary>
            public TimeSpan AverageQueueTime { get { return TimeSpan.FromTicks((CompletedCount + ProcessingCount) > 0 ? TotalQueueTime.Ticks / (CompletedCount + ProcessingCount) : 0); } }

            #region IStatisticsEditable Members

            /// <summary>
            /// This should be called when a task is first queued. This will increase teh queued count.
            /// </summary>
            public void NewQueued()
            {
                lock (this)
                {
                    queuedCount++;
                }
                Changed(this);
            }

            /// <summary>
            /// This should be called when a task becomes unqueued and starts processing.
            /// </summary>
            /// <param name="queuedTime">The length of time that the task was queued for.</param>
            public void NewProcessing(TimeSpan queuedTime)
            {
                lock (this)
                {
                    queuedCount--;
                    processingCount++;
                    totalQueueTime += queuedTime;
                }
                Changed(this);
            }

            /// <summary>
            /// This should be called when a task finishes processing successfully.
            /// </summary>
            /// <param name="taskTime">The length of time that the task took to process</param>
            public void NewSuccessful(TimeSpan taskTime)
            {
                lock (this)
                {
                    processingCount--;
                    successfulCount++;
                    totalTaskTime += taskTime;
                }
                Changed(this);
            }

            /// <summary>
            /// This should be called when a task finishes processing in failure.
            /// </summary>
            /// <param name="taskTime">The length of time that the task took to process</param>
            public void NewFailure(TimeSpan taskTime)
            {
                lock (this)
                {
                    processingCount--;
                    failedCount++;
                    totalTaskTime += taskTime;
                }
                Changed(this);
            }

            public void AbortQueued()
            {
                lock (this)
                {
                    queuedCount--;
                }
                Changed(this);
            }

            #endregion
        }

        public delegate void TransactionEventHandler(DbTransactionRecord record);

        private TransactionEventHandler updatedTransaction;

        public TransactionEventHandler UpdatedTransaction
        {
            get { return updatedTransaction; }
            set { updatedTransaction = value; }
        }

        private TransactionEventHandler startingTransaction;

        public TransactionEventHandler StartingTransaction
        {
            get { return startingTransaction; }
            set { startingTransaction = value; }
        }

        public delegate void CompletedTransactionEventHandler(DbTransactionRecord transaction, TransactionProcessResult success);

        private CompletedTransactionEventHandler completedTransaction;
        private ICctmDatabase2 database2;

        public CompletedTransactionEventHandler CompletedTransaction
        {
            get { return completedTransaction; }
            set { completedTransaction = value; }
        }
    }
}
