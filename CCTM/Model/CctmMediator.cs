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

namespace Cctm.Model
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
        public Semaphore throttle;
        private DetailedLogDelegate detailedLog;
        private static long totalStarted = 0;
        private ICctmDatabase database;
        private AuthorizationSuite authorizationSuite;
        private CctmPerformanceCounters performanceCounters;
        /// <summary>
        /// List of running or queued transaction processing tasks
        /// </summary>
        private List<Task> runningTasks;

        /// <summary>
        /// List of transaction records IDs for records which are still "New" in the database but are queued for processing.
        /// </summary>
        private List<int> queuedRecords = new List<int>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="authorizationSuite"></param>
        /// <param name="statisticsChanged">Delegate signalled when there is a change in the statistics record.</param>
        public CctmMediator(ICctmDatabase database,
            AuthorizationSuite authorizationSuite,
            Action<IStatistics> statisticsChanged,
            Action<string> log,
            Action<string> fileLog,
            Action<UpdatedTransactionRecord> processedRecord,
            DetailedLogDelegate detailedLog,
            int maxSimultaneous,
            CctmPerformanceCounters performanceCounters)
        {
            this.performanceCounters = performanceCounters;
            this.throttle = new Semaphore(maxSimultaneous, maxSimultaneous);
            this.database = database;
            this.authorizationSuite = authorizationSuite;
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
        
        public void ProcessTransaction(TransactionRecord transactionRecord, DateTime taskQueuedTime)
        {
            AuthorizationResponseFields authorizationResponse = new AuthorizationResponseFields(AuthorizationResultCode.UnknownError, "Not processed", "Not processed", "Not processed", "Not processed", 0, 0);
            CreditCardTrackFields creditCardFields = new CreditCardTrackFields() { ExpDateYYMM = "????", Pan = new CreditCardPan() { }, ServiceCode = ""};
            CreditCardStripe unencryptedStripe = new CreditCardStripe("");
            CreditCardPan obscuredPan = new CreditCardPan();
            
            // Check if we're supposed to be running new transactions
            if (!mediationThreadRun.WaitOne(0))
            {
                statistics.AbortQueued();
                // The transaction is no longer queued
                lock(queuedRecords)
                    queuedRecords.Remove(transactionRecord.ID);
                // Raise event that it was cancelled
                OnCompletedTransaction(ref transactionRecord, TransactionProcessResult.Cancelled);
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var tranasctionPerformanceCounters = performanceCounters.StartingTransaction())
            {
                AuthorizeMode mode;
                // What mode?
                if (transactionRecord.PreauthStatus == TransactionStatus.ReadyToProcessNew)
                    mode = AuthorizeMode.Preauth;
                else if (transactionRecord.PreauthStatus == TransactionStatus.Approved)
                    mode = AuthorizeMode.Finalize;
                else
                    mode = AuthorizeMode.Normal;
                bool isPreAuth = mode == AuthorizeMode.Preauth;

                Interlocked.Increment(ref totalStarted);
                //if (Interlocked.Read(ref totalStarted) > 4)
                //    Stop(); // Stop future transactions if this is the 5th one

                throttle.WaitOne();

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
                        UpdateTransactionStatus(ref transactionRecord, TransactionStatus.Processing, isPreAuth);
                    }
                    finally
                    {
                        // The transaction is no longer queued
                        lock (queuedRecords)
                            queuedRecords.Remove(transactionRecord.ID);
                    }

                    CreditCardTracks tracks;

                    // Finalize doesnt have a track
                    if (mode != AuthorizeMode.Finalize)
                    {
                        if (isPreAuth)
                        {
                            transactionRecord.TransactionIndex = transactionRecord.PreauthTransactionIndex ?? transactionRecord.TransactionIndex;
                            if (transactionRecord.PreauthAmountDollars == null)
                                throw new InvalidOperationException("Preauth amount is null");
                            transactionRecord.AmountDollars = transactionRecord.PreauthAmountDollars.Value;
                        }

                        // Validate request 
                        transactionRecord.Validate("Err: Invalid rec data");

                        // Decrypt the request
                        var decryptedStripe = DecryptStripe(transactionRecord, "Err: Decryption");

                        // Special cases (E.g. RSA track data shifted, sentinels missing etc)
                        var formattedStripe = TrackFormat.FormatSpecialStripeCases(decryptedStripe, transactionRecord.EncryptionMethod, "Err: Format Stripe");

                        unencryptedStripe = new CreditCardStripe(formattedStripe);

                        // Validate the credit card stripe
                        unencryptedStripe.Validate("Err: Invalid Track Data");

                        // Split the stripe into tracks
                        tracks = unencryptedStripe.SplitIntoTracks("Err: Stripe parse error");

                        // Validate the tracks
                        tracks.Validate("Err: Invalid Track Data");

                        // Split track two into fields
                        creditCardFields = tracks.ParseTrackTwo("Err: Track parse error");

                        // Validate the fields
                        creditCardFields.Validate("Err: Invalid Track Fields");

                        // If this isn't a normal card, then we have an exceptional circumstance of this being a special card
                        if (creditCardFields.CardType != CardType.Normal)
                            throw new SpecialCardException();
                    }
                    else
                        tracks = new CreditCardTracks();

                    // Choose the authorization platform
                    IAuthorizationPlatform authorizationPlatform = ChooseAuthorizationPlatform(transactionRecord, authorizationSuite, "Err: Clearing platform");

                    // Update the record to be authorizing
                    UpdateTransactionStatus(ref transactionRecord, TransactionStatus.Authorizing, isPreAuth);

                    // Perform the authorization
                    authorizationResponse = AuthorizeRequest(transactionRecord, tracks, unencryptedStripe, creditCardFields, authorizationPlatform, transactionRecord.UniqueRecordNumber, mode, transactionRecord.PreauthTtid);

                    // Decide the status according to the response and update the database
                    TransactionStatus newStatus = transactionRecord.Status;
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
                        //database.UpdateTrack(transactionRecord.ID, newTrackText);
                    }

                    // Send the updated record information to the database
                    UpdatedTransactionRecord updatedRecord = new UpdatedTransactionRecord()
                    {
                        AuthorizationCode = authorizationResponse.authorizationCode,
                        CardEaseReference = authorizationResponse.receiptReference,
                        CardScheme = authorizationResponse.cardType,
                        ExpiryDate = (mode != AuthorizeMode.Finalize) ? creditCardFields.ExpDateYYMM : "",
                        FirstSix = (mode != AuthorizeMode.Finalize) ? creditCardFields.Pan.FirstSixDigits : "",
                        LastFour = (mode != AuthorizeMode.Finalize) ? creditCardFields.Pan.LastFourDigits : "",
                        PAN = obscuredPan.ToString(),
                        TransactionRecordID = transactionRecord.ID,
                        BatchNum = authorizationResponse.BatchNum,
                        Ttid = authorizationResponse.Ttid,
                        Status = newStatus, //isPreAuth ? (transactionRecord.PreauthStatus ?? transactionRecord.Status) : transactionRecord.Status,
                        TrackText = newTrackText
                    };

                    // Record it into the database
                    /*switch (mode)
                    {
                        case AuthorizeMode.Preauth: database.UpdatePreauthRecord(transactionRecord, updatedRecord); break;
                        case AuthorizeMode.Finalize: database.UpdateFinalizeRecord(transactionRecord, updatedRecord); break;
                        default: database.UpdateTransactionRecord(updatedRecord); break;
                    }*/
                    if (!isPreAuth)
                        database.UpdateTransactionRecordCctm(transactionRecord, updatedRecord);
                    else
                        database.UpdatePreauthRecord(transactionRecord, updatedRecord);
                    transactionRecord.Status = updatedRecord.Status;
                    UpdatedTransaction(transactionRecord);

                    processedRecord(updatedRecord);

                    fileLog(
                        "ID: " + transactionRecord.ID
                        + "; Result: " + authorizationResponse.resultCode.ToString()
                        + "; Notes: " + authorizationResponse.note
                        );

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
                    fileLog(transactionRecord.ID + " " + exception.ToString());
                    UpdateTransactionStatus(ref transactionRecord, TransactionStatus.StripeError, isPreAuth);
                    tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);
                }
                catch (StripeErrorException exception)
                {
                    fileLog(transactionRecord.ID + " " + exception.ToString());
                    // Update the fail status
                    UpdateTransactionStatus(ref transactionRecord, TransactionStatus.StripeError, isPreAuth);
                    tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);

                }
                catch (AuthorizerProcessingException exception)
                {
                    fileLog(transactionRecord.ID + " " + exception.ToString());
                    UpdateTransactionStatus(ref transactionRecord, TransactionStatus.AuthError, isPreAuth);
                    tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);
                }
                catch (Exception exception)
                {
                    fileLog(transactionRecord.ID + " " + exception.ToString());
                    tranasctionPerformanceCounters.FailedTransaction(stopwatch.ElapsedTicks);
                }
                finally
                {
                    if (authorizationResponse.resultCode != AuthorizationResultCode.Approved)
                        detailedLog(DateTime.Now.ToString(), transactionRecord.Status.ToText(), Convert.ToBase64String(transactionRecord.EncryptedStripe), unencryptedStripe.ToString(), creditCardFields.ExpDateMMYY, transactionRecord.StartDateTime.ToString(), obscuredPan.ToString(), transactionRecord.ID.ToString(), "", transactionRecord.EncryptionMethod.ToString(), transactionRecord.KeyVersion.ToString(), transactionRecord.AmountDollars.ToString(), transactionRecord.TransactionIndex.ToString(), transactionRecord.UniqueRecordNumber, transactionRecord.TerminalSerialNumber, authorizationResponse.note);

                    if (result == TransactionProcessResult.Successful)
                        statistics.NewSuccessful(new TimeSpan(DateTime.Now.Ticks - taskStart.Ticks));
                    else
                        statistics.NewFailure(new TimeSpan(DateTime.Now.Ticks - taskStart.Ticks));
                    OnCompletedTransaction(ref transactionRecord, result);

                    throttle.Release();
                }
            }
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

        protected virtual void OnStartingTransaction(ref TransactionRecord record)
        {
            if (StartingTransaction != null)
                StartingTransaction(record);
        }

        protected virtual void OnCompletedTransaction(ref TransactionRecord record, TransactionProcessResult successful)
        {
            if (CompletedTransaction != null)
                CompletedTransaction(record, successful);
        }

        private void UpdateTransactionStatus(ref TransactionRecord record, TransactionStatus newStatus, bool preAuth)
        {
            if (!preAuth)
            {
                // Update the database
                if (database.UpdateRecordStatus(record.ID, record.Status, newStatus) == record.Status)
                {
                    record.Status = newStatus;
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
                if (database.UpdatePreauthStatus(record.ID, record.PreauthStatus, newStatus) == record.PreauthStatus)
                {
                    record.PreauthStatus = newStatus;
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
                List<TransactionRecord> newRecords;

                List<int> previouslyQueuedRecords = new List<int>(queuedRecords.Count); // List of queued records from before SelectNewTransactionRecords starts
                lock (queuedRecords)
                    previouslyQueuedRecords.AddRange(queuedRecords);

                IEnumerable<TransactionRecord> transactionRecords = database.SelectNewTransactionRecords();
                lock (queuedRecords)
                {
                    // List of transactions that are actually "new" (not in queue already)
                    newRecords = (from transaction in transactionRecords
                                  where (previouslyQueuedRecords.IndexOf(transaction.ID) == -1)
                                    && (queuedRecords.IndexOf(transaction.ID) == -1)
                                  select transaction).ToList();
                    if (newRecords.Count() > 0)
                        // Add the IDs of all the new transactions
                        queuedRecords.AddRange(from r in newRecords select r.ID);
                }

                // For each transaction that isn't already queued
                for (int i = 0; i < newRecords.Count; i++)
                {
                    var transactionRecord = newRecords[i];

                    statistics.NewQueued();

                    // Flag the record as queued (so we dont access it again before the status has been updated in the database)


                    // Add it to the task list
                    DateTime queueTime = DateTime.Now;
                    Task transactionTask = new Task(() => ProcessTransaction(transactionRecord, queueTime));
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

        private static IAuthorizationPlatform ChooseAuthorizationPlatform(
            TransactionRecord transaction,
            AuthorizationSuite authorizationSuite,
            string failStatus)
        {
            IAuthorizationPlatform authorizationPlatform = null;

            switch (transaction.ClearingPlatform.ToUpper())
            {
                case "LIVE":
                    throw new StripeErrorException("Credit-Call no longer supported");
                    //authorizationPlatform = authorizationSuite.CreditCallLive.Value; break;
                case "TEST":
                    //authorizationPlatform = authorizationSuite.CreditCallTest.Value; break;
                    throw new StripeErrorException("Credit-Call no longer supported");
                case "ICVFDMS":
                    //authorizationPlatform = authorizationSuite.ICVerifyLive.Value; break;
                    throw new StripeErrorException("ICVerifiy no longer supported");
                case "ICV_TEST":
                    throw new StripeErrorException("ICVerifiy no longer supported");
                    //authorizationPlatform = authorizationSuite.ICVerifyTest.Value; Break;
                case "MONETRA":
                    authorizationPlatform = authorizationSuite.Monetra.Value;
                    break;
                case "LIVE-MIGS":
                    // authorizationPlatform = authorizationSuite.MigsLive.Value; Break;
                    throw new StripeErrorException("MIGS no longer supported");
                case "TEST-MIGS":
                    //authorizationPlatform = authorizationSuite.MigsTest.Value; Break;
                    throw new StripeErrorException("MIGS no longer supported");
                default:
                    authorizationPlatform = null;
                    break;
            }

            if (authorizationPlatform == null)
                throw new StripeErrorException("Clearing platform not supported: \"" + transaction.ClearingPlatform + "\"", failStatus);

            return authorizationPlatform;
        }

        private AuthorizationResponseFields AuthorizeRequest(TransactionRecord transaction, CreditCardTracks tracks, CreditCardStripe unencryptedStripe, CreditCardTrackFields creditCardFields,
            IAuthorizationPlatform authorizationPlatform, string orderNumber, AuthorizeMode mode, int? preauthTtid)
        {
            
            AuthorizationRequest authorizationRequest = new AuthorizationRequest(
                transaction.TerminalSerialNumber,
                transaction.StartDateTime,
                transaction.MerchantID,
                transaction.MerchantPassword,
                creditCardFields.Pan.ToString(),
                creditCardFields.ExpDateMMYY,
                transaction.AmountDollars,
                "",
                transaction.ID.ToString(),
                transaction.ID.ToString(),
                tracks.TrackTwo.ToString(),
                unencryptedStripe.Data,
                transaction.ID.ToString(),
                orderNumber,
                preauthTtid);

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
        private UnencryptedStripe DecryptStripe(TransactionRecord transaction, string errorStatus)
        {
            StripeDecryptor decryptor = new StripeDecryptor();
            TransactionInfo info = new TransactionInfo
            (
                amountDollars: transaction.AmountDollars,
                meterSerialNumber: transaction.TerminalSerialNumber,
                startDateTime: transaction.StartDateTime,
                transactionIndex: transaction.TransactionIndex,
                refDateTime: transaction.RefDateTime
            );

            string decryptedStripe = decryptor.decryptStripe(transaction.EncryptedStripe, transaction.EncryptionMethod, transaction.KeyVersion, info, errorStatus);
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

        public delegate void TransactionEventHandler(TransactionRecord record);

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

        public delegate void CompletedTransactionEventHandler(TransactionRecord transaction, TransactionProcessResult success);

        private CompletedTransactionEventHandler completedTransaction;

        public CompletedTransactionEventHandler CompletedTransaction
        {
            get { return completedTransaction; }
            set { completedTransaction = value; }
        }
    }
}
