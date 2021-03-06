/* CCTM (Credit Card Transaction Manager) Design - Quick Overview
 * --------------------------------------------------------------
 * The CctmForm handles the main GUI, and also the runtime configuration (by virtue of  
 * RAD Windows Forms in VisualStudio). Logging is also managed here because it is 
 * considered strongly coupled to the GUI. The runtime object configuration is 
 * established in the CCTM form because it is coupled (although loosely) to the GUI -
 * how the GUI displays it. The CCTM form code is grouped using #regions, so browsing
 * in a collapsed view is easier.
 * A CctmMediator mediates between all of the components configured in the CctmForm,
 * and handles the LOGIC of what happens with a transaction. The way this logic is
 * implemented depends on the runtime configuration (set up by the CctmForm).
 * The mediator uses various modules (called "servers" because they represent various
 * remote servers), such as a Monetra Server, Database etc. 
 * In general, access to these servers is policed by ServerControllers which handle 
 * the logic behind server set-up and failure. 
 */

using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Configuration;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using AuthorizationClientPlatforms;
using TransactionManagementCommon;
using Cctm.Common;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Shell;
using TransactionManagementCommon.ControllerBase;
using Cctm.Database;
using Cctm.Behavior;
using Cctm.DualAuth;
using AutoDatabase;

using AuthorizationClientPlatforms.Settings;

namespace Cctm
{
    public partial class CctmForm : Form
    {
        #region Establishing runtime configuration

        public static Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private static CctmPerformanceCounters performanceCounters = new CctmPerformanceCounters(); 
        private bool hidden;
        private static string logFolder = Properties.Settings.Default.LogFolder;
        private static AutoDatabaseBuilder<ICctmDatabase2> dualAuthDatabaseBuilder = new AutoDatabaseBuilder<ICctmDatabase2>();

        /// <summary>
        /// Called when the form loads (beginning of application)
        /// </summary>
        private void CctmForm_Load(object sender, EventArgs e)
        {
            tickWatchDog();
            prepareLog();
            prepareGUI();
            loadSettings();
            prepareEnvironment();
            prepareCctmServers();
        }

        public void InitializeHidden()
        {
            hidden = true;
            prepareLog();
            loadSettings();
            prepareEnvironment();
            prepareCctmServers();
        }

        /// <summary>
        /// Sets up any runtime GUI elements
        /// </summary>
        private void prepareGUI()
        {
            listView2.Items.Clear();
            queuedCountDisplay = listView2.Items.Add("Queued").SubItems.Add("0");
            processingCountDisplay = listView2.Items.Add("Processing").SubItems.Add("0");
            successfulCountDisplay = listView2.Items.Add("Success").SubItems.Add("0");
            failedCountDisplay = listView2.Items.Add("Failed").SubItems.Add("0");
            queuedTimeDisplay = listView2.Items.Add("Avg queue time").SubItems.Add("0");
            processingTimeDisplay = listView2.Items.Add("Avg processing time").SubItems.Add("0");

            listView1.Items.Clear();
            mediatorStatus = listView1.Items.Add("CCTM Main").SubItems.Add("-");

            // NOTE: Removed unused authorization platforms.
            // NOTE: Also removed unused database entry since DB handling
            // is part of CCTM Main.
        }

        /// <summary>
        /// Reads the configuration files
        /// </summary>
        private void loadSettings()
        {
            string monetraHostName = "Monetra_HostName";
            string monetraServerSocket = "Monetra_ServerSocket";

            // Monetra
            if (null != configuration.AppSettings.Settings[monetraHostName])
            {
                Monetra_HostName = configuration.AppSettings.Settings[monetraHostName].Value;
            }

            if (null != configuration.AppSettings.Settings[monetraServerSocket])
            {
                Monetra_ServerSocket = Convert.ToUInt16(configuration.AppSettings.Settings[monetraServerSocket].Value);
            }

            PollIntervalSeconds = Convert.ToUInt16(configuration.AppSettings.Settings["Poll_Interval_Seconds"].Value);
                        
            generalLog("Monetra Server, socket = " + Monetra_HostName + ", " + Monetra_ServerSocket);
        }

        /// <summary>
        /// Starts off the CCTM logging
        /// </summary>
        private void prepareLog()
        {
            generalLog(DateTime.Now + " - System Started.  ");
            fileLog(DateTime.Now + " - System Started.  " + Environment.NewLine);

            // Write headings to detailed file log
            detailedLog("LogDateTime", "Status", "EncryptedTrack", "DecryptedTrack", "ExpDate", "Transaction StartDateTime", "PAN",
                "TransactionID", "AuthCode", "EncVer", "KerVer", "CCAmount", "CCTransactionIndex", "UniqueRecNum", "TerminalSerNo", "Note");
        }

        /// <summary>
        /// Sets up misc .net environment changes
        /// </summary>
        private void prepareEnvironment()
        {
            // Unhandled task exceptions shouldnt crash the application. These occur if a server is initialized in a seperate 
            // task and an error is raised in the initialization, but the result is not used before the task is garbage collected.
            TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(
                (obj, args) =>
                {
                    generalLog(args.Exception.ToString());
                    fileLog(args.Exception.ToString());
                    args.SetObserved();
                });
            ThreadPool.SetMaxThreads(20, 20);
        }

        /// <summary>
        /// Prepares the CCTM servers for use (such as the database and monetra).
        /// </summary>
        private void prepareCctmServers()
        {
            // NOTE: Removed initialization of older database initialization
            // that uses data sets since the DB interface is now handled by
            // AutoDatabase and ICctmDatabase2.

            // Initialize the authorization suite, now in dictionary form.
            Dictionary<string, Lazy<IAuthorizationPlatform>> platforms = prepareAuthorizationSuite(testMode: false);

            int maxSimultaneous = Properties.Settings.Default.MaxSimultaneous;

            // Create a mediator factory to specify how the mediator must be created
            ServerFactory<CctmMediator> cctmMediatorFactory = new ThreadedServerFactory<CctmMediator>(
                "CctmMediatorFactory",
                () => 
                {
                    var connectionSource = new ConnectionSource(Properties.Settings.Default.SSPM_DBConnectionString);
                    var databaseTracker = new DatabaseTracker(fileLog, extendedDatabaseLogging);
                    var cctmDatabase2 = dualAuthDatabaseBuilder.CreateInstance(connectionSource, databaseTracker);
                    // Create the mediator
                    CctmMediator mediator = new CctmMediator(cctmDatabase2, platforms, statisticsChanged, generalLog, fileLog, (x) => tickWatchDog(), detailedLog, maxSimultaneous, performanceCounters);
                    mediator.PollIntervalSeconds = PollIntervalSeconds;
                    // Set up the events for the mediator
                    mediator.StartingTransaction += StartingTransaction;
                    mediator.UpdatedTransaction += UpdatingTransaction;
                    mediator.CompletedTransaction += CompletedTransaction;
                    return mediator;
                }
                );

            // Create the mediator to tie everything together (created using a controller just to standardise it with all the other "servers")
            this.mediator = new ServerController<CctmMediator>(
                cctmMediatorFactory,
                (status) => updateServerStatus(mediatorStatus, status),
                failedRestart: (exception, retryCount) => RestartFailAction.Retry
            );
        }

        private IAuthorizationPlatform prepareMonetraServer(bool testMode)
        {
            MonetraClient client = null;
            // Create the monetra DLL client as the client for the monetra server to use (consider this a switch to tell the CCTM to use the third-party monetra DLL)
            if (testMode)
                client = new MonetraDotNetNativeClient("216.155.101.82", 8444, generalLog);
            else
                client = new MonetraDotNetNativeClient(Monetra_HostName, Monetra_ServerSocket, generalLog);
            // Create the monetra server to use the specified client
            Monetra monetra = new Monetra(client, generalLog);
            //monetra.Statistics.Changed += ;
            return monetra;
        }

        private IAuthorizationPlatform prepareIsraelServer(bool testMode)
        {
            return new IsraelPremium(Properties.Settings.Default.IsraelMerchantNumber, Properties.Settings.Default.IsraelCashierNumber); 
        }

        /// <summary>
        /// Helper method for setting up a authorization platform using the authorization processor interface.
        /// </summary>
        /// <param name="processor">Name of authorization processor to load</param>
        /// <param name="testMode">unused</param>
        /// <returns></returns>
        private IAuthorizationPlatform prepareAuthorizationProcessorServer(ProcessorElement processor, bool testMode)
        {
            Dictionary<string, string> configuration = processor.GetConfiguration();

            return new AuthorizationClientPlatforms.AuthorizationPlatform(processor.Server, processor.Name, configuration);
        }

        // NOTE: Remove prepareDatabase() class since it utilizes the older 
        // data set model has been replaced by ICctmDatabase2. Database
        // initialization is being done as part of prepareCctmServers().

        /// <summary>
        /// Initializes each of the processing platforms using ServerInitializer
        /// </summary>
        /// <returns>
        /// A suite of authorization servers
        /// </returns>
        private Dictionary<string, Lazy<IAuthorizationPlatform>> prepareAuthorizationSuite(bool testMode)
        {
            Dictionary<string, Lazy<IAuthorizationPlatform>> platforms = new Dictionary<string, Lazy<IAuthorizationPlatform>>(StringComparer.CurrentCultureIgnoreCase);

            /* This creates a factory for creating authorization-platform wrappers.
             * Using a factory to create wrappers is convenient because there many 
             * possible authorization platforms, and each will be created in a similar
             * way: using a thread factory (the server is established in the background 
             * and not the GUI thread), and using the AuthorizationControllerWrapper. 
             */
            ControllerWrapperFactory<IAuthorizationPlatform> authorizationPlatformFactory =
                new ControllerWrapperFactory<IAuthorizationPlatform>(
                    // Say that we want controllers to use a threaded server factory
                    (name, factoryMethod) => new ThreadedServerFactory<IAuthorizationPlatform>(name, factoryMethod),
                    // Specify the controller wrapper to use
                    (controller) => new ServerControllers.AuthorizerControllerWrapper(controller),
                    failedRestart: (exception, retryCount) => RestartFailAction.Retry
                    );

            // Only set up monetra if it's configured.
            if (0 != Monetra_ServerSocket)
            {
                // Monetra
                platforms["monetra"] = authorizationPlatformFactory.CreateControllerWrapper(
                    "MontraFactory",
                    // How do we create the monetra server?
                    factoryMethod: () => prepareMonetraServer(testMode),
                    // What does it use to update the monetra server status?
                    statusUpdate: (status) => updateServerStatus(monetraStatus, status)
                    );

                monetraStatus = listView1.Items.Add("Monetra").SubItems.Add("-");
            }

            // Only setup Israel Premium if it's configured.
            if (!String.IsNullOrEmpty(Properties.Settings.Default.IsraelMerchantNumber))
            {
                // Israel Premium
                platforms["israel-premium"] = authorizationPlatformFactory.CreateControllerWrapper(
                    "IsraelPremium",
                    // How do we create the Israel premium server?
                    factoryMethod: () => prepareIsraelServer(testMode),
                    // What does it use to update the server status?
                    statusUpdate: (status) => updateServerStatus(israelPremiumStatus, status)
                        );

                israelPremiumStatus = listView1.Items.Add("Israel Premium").SubItems.Add("-");
            }

            // Programmatically add new processors via configuration.
            AuthorizationClientPlatformsSection acpSection = (AuthorizationClientPlatformsSection)ConfigurationManager.GetSection("authorizationClientPlatforms");

            // Loop through each processor entry.
            foreach (ProcessorElement entry in acpSection.AuthorizationProcessors)
            {
                // Add the processor to the status display... only if it's running in GUI mode.
                ListViewItem.ListViewSubItem processorStatus = null;
                if (!hidden)
                {
                    processorStatus = listView1.Items.Add(entry.Description).SubItems.Add("-"); ;
                }

                // Create controller for the configured authorization processor.
                platforms[entry.Name] = authorizationPlatformFactory.CreateControllerWrapper(
                    entry.Description,
                    // How do we create the Israel premium server?
                    factoryMethod: () => prepareAuthorizationProcessorServer(entry, testMode),
                    // What does it use to update the server status?
                    statusUpdate: (status) => updateServerStatus(processorStatus, status)
                        );
            }

            return platforms;
        }

        #endregion

        #region CCTM Events and Logging

        private Mutex fileLogLock = new Mutex();
        private Mutex detailedLogLock = new Mutex();

        /// <summary>
        /// Calling this logs a message to the text box
        /// </summary>
        public void generalLog(string message)
        {
            if (!hidden)
                eventLogForm.Value.log(message);

            // Persist in log file too.
            fileLog(message);
        }

        /// <summary>
        /// This is for heavy debugging and recording of transactions (generally failed) to a file
        /// </summary>
        public void detailedLog(string datetime, string status, string encryptedTrack,
            string track, string expDate, string startDateTime, string pan, string transactionId, string authCode,
            string encVer, string keyVer, string ccAmount, string ccTransactionIndex, string uniqueRecordNum,
            string terminalserno, string note)
        {
            return;/*
            detailedLogLock.WaitOne();
            try
            {
                string dateString = DateTime.Now.ToString("yyyy'_'MM'_'dd");
                string fileName = "logs\\Detailed_Transactions_" + dateString + ".txt";
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");
                File.AppendAllText(fileName,
                    datetime +   // Now
                    '\t' + status +
                    '\t' + expDate +
                    '\t' + startDateTime +  // Transaction startDateTime
                    '\t' + pan +
                    '\t' + transactionId +
                    '\t' + authCode +
                    '\t' + encVer +
                    '\t' + keyVer +
                    '\t' + ccAmount +
                    '\t' + ccTransactionIndex +
                    '\t' + uniqueRecordNum +
                    '\t' + terminalserno +
                    '\t' + "obscured" +
                    '\t' + "obscured" +
                    '\t' + note +           // Depending on caller
                    Environment.NewLine
                    );
            }
            finally
            {
                detailedLogLock.ReleaseMutex();
            }*/
        }

        public void fileLog(string msg)
        {
            fileLogLock.WaitOne();
            try
            {
                var now = DateTime.Now;
                string dateString = now.ToString("yyyy'_'MM'_'dd'_'HH");
                string timeString = now.ToString("HH:mm:ss");
                string fileName = Path.Combine(logFolder, "Transactions_" + dateString + ".txt");
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");
                File.AppendAllText(fileName, timeString + " :: " + msg + Environment.NewLine);
            }
            finally
            {
                fileLogLock.ReleaseMutex();
            }
        }

        /// <summary>
        /// Called when a server's status changes
        /// </summary>
        /// <param name="item">The list item representing the server</param>
        /// <param name="text">The new text to use for the server's status</param>
        public void updateServerStatus(ListViewItem.ListViewSubItem item, string text)
        {
            if (!hidden)
                listView1.Invoke(new Action(() => { item.Text = text; }));
        }

        /// <summary>
        /// Event called when the Mediator statistics have changed
        /// </summary>
        /// <param name="statistics"></param>
        private void statisticsChanged(CctmMediator.IStatistics statistics)
        {
            if (!hidden)
            {
                listView2.Invoke(new Action(() =>
                {
                    queuedCountDisplay.Text = statistics.QueuedCount.ToString();
                    processingCountDisplay.Text = statistics.ProcessingCount.ToString();
                    successfulCountDisplay.Text = statistics.SuccessfulCount.ToString();
                    failedCountDisplay.Text = statistics.FailedCount.ToString();
                    queuedTimeDisplay.Text = statistics.AverageQueueTime.ToString("g");
                    processingTimeDisplay.Text = statistics.AverageTaskTime.ToString("g");
                }));
            }
        }

        private void StartingTransaction(DbTransactionRecord transaction)
        {
            if (!hidden)
            {
                listView3.Invoke(new Action(() =>
                    {
                        ListViewItem transactionListItem;
                        if (displayedTransactions.ContainsKey(transaction.TransactionRecordID))
                        {
                            transactionListItem = displayedTransactions[transaction.TransactionRecordID];
                            transactionListItem.SubItems[1].Text = ((TransactionStatus)transaction.Status).ToText();
                        }
                        else
                        {
                            transactionListItem = listView3.Items.Add(transaction.TransactionRecordID.ToString());
                            transactionListItem.SubItems.Add(((TransactionStatus)transaction.Status).ToText());
                        }
                        transactionListItem.StateImageIndex = 0;
                        displayedTransactions[transaction.TransactionRecordID] = transactionListItem;
                    }
                ));
            }
        }

        private void UpdatingTransaction(DbTransactionRecord transaction)
        {
            if (!hidden)
            {

                listView3.Invoke(new Action(() =>
                    {
                        if (displayedTransactions.ContainsKey(transaction.TransactionRecordID))
                        {
                            displayedTransactions[transaction.TransactionRecordID].SubItems[1].Text = ((TransactionStatus)transaction.Status).ToText();
                            displayedTransactions[transaction.TransactionRecordID].StateImageIndex = 0;

                            listView3AutoScrollToItem(displayedTransactions[transaction.TransactionRecordID]);
                        }
                    }
                ));
            }
        }

        private void listView3AutoScrollToItem(ListViewItem item)
        {
            if (!hidden)
            {
                listView3.Invoke(new Action(() =>
                    {
                        // Check if the user has scrolled the listView - if so, then we disable auto-scrolling
                        if (autoScrollTarget != null)
                        {
                            if ((autoScrollTarget.Bounds.Bottom < listView3.ClientRectangle.Top)
                              || (autoScrollTarget.Bounds.Top > listView3.ClientRectangle.Bottom))
                                listViewAutoScroll = false;
                            else
                                listViewAutoScroll = true;
                        }

                        // Scroll down if auto scrolling is enabled
                        if ((listViewAutoScroll) && (item.Bounds.Top > listView3.ClientRectangle.Top))
                            listView3.EnsureVisible(item.Index);

                        autoScrollTarget = item;
                    }));
            }
        }

        private void CompletedTransaction(DbTransactionRecord transaction, CctmMediator.TransactionProcessResult result)
        {
            if (!hidden)
            {
                if (!displayedTransactions.ContainsKey(transaction.TransactionRecordID))
                    return;

                listView3AutoScrollToItem(displayedTransactions[transaction.TransactionRecordID]);


                if (result != CctmMediator.TransactionProcessResult.Error)
                {
                    if (result == CctmMediator.TransactionProcessResult.Successful)
                    {
                        listView3.Invoke(new Action(() =>
                            displayedTransactions[transaction.TransactionRecordID].StateImageIndex = 1));
                    }
                    else if (result == CctmMediator.TransactionProcessResult.Cancelled)
                    {
                        listView3.Invoke(new Action(() =>
                        {
                            if (displayedTransactions.ContainsKey(transaction.TransactionRecordID))
                            {
                                displayedTransactions[transaction.TransactionRecordID].SubItems[1].Text = "Cancelled";
                                displayedTransactions[transaction.TransactionRecordID].StateImageIndex = 3;
                            }
                        }));
                    }
                    ListViewItem listItem = displayedTransactions[transaction.TransactionRecordID];
                    displayedTransactions.Remove(transaction.TransactionRecordID);

                    DelayedDoer.DoLater(60, () =>
                        {
                            listView3.Invoke(new Action(() =>
                                listView3.Items.Remove(listItem)
                                ));
                        });

                }
                else
                {
                    listView3.Invoke(new Action(() =>
                        {
                            displayedTransactions[transaction.TransactionRecordID].StateImageIndex = 2;
                            displayedTransactions[transaction.TransactionRecordID].SubItems[1].Text = ((TransactionStatus)transaction.Status).ToText();
                        }
                        ));
                }
            }
        }

        #endregion

        #region GUI Events
        private void eventLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            eventLogForm.Value.Show();
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            aboutForm.Value.Show();
        }

        public void CctmForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if we need to wait for transactions to complete
            int waitCount = 0;
            mediator.Perform((m) => waitCount = m.Statistics.ProcessingCount);
            if (waitCount <= 0)
                return;

            // Stop the mediator cycle from getting more transactions from the database
            mediator.Perform((m) => m.Stop());
            // Create a dialog to show the progress on the shut-down
            WaitForFinishForm waitForm = new WaitForFinishForm();
            // The number of items we're waiting for is the number of items processing + the number of queued items
            mediator.Perform((m) => waitForm.MaxWaitCount = m.Statistics.ProcessingCount);

            // This must be called when the when the mediator stats change, which will update the display on the dialog
            Action<CctmMediator.IStatistics> statisticsChanged = new Action<CctmMediator.IStatistics>((stats) =>
            {
                waitForm.WaitCountChanged(stats.ProcessingCount);
            });

            // Get notified about stats changes
            mediator.Perform((m) => { m.StatisticsChanged += statisticsChanged; });
            try
            {
                // Enable auto-scrolling
                if (autoScrollTarget != null)
                    autoScrollTarget.EnsureVisible();

                // Show the dialog... this will only return when all the transactions have completed or when the user specifies
                DialogResult waitResult = waitForm.ShowDialog(this);

                // If the user doesnt want to close
                if (waitResult == DialogResult.Cancel)
                {
                    // Start the mediation again
                    mediator.Perform((m) => m.Start());
                    // Cancel the close
                    e.Cancel = true;
                }
            }
            finally
            {
                // Unhook the event
                mediator.Perform((m) => { m.StatisticsChanged -= statisticsChanged; });
                // Free the dialog
                waitForm.Dispose();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!hidden)
            {
                double processorTime = performanceCounter.NextValue();
                CpuLoadState1 = (CpuLoadState1 * 0.9 + processorTime * 0.1);
                CpuLoadState2 = (CpuLoadState2 * 0.9 + CpuLoadState1 * 0.1);
                toolStripStatusLabel1.Text = "CPU Load: " + Math.Round(CpuLoadState2).ToString() + " %";
            }
        }
        #endregion

        #region Runtime GUI Elements
        // List elements
        private ListViewItem.ListViewSubItem queuedCountDisplay,
            processingCountDisplay,
            successfulCountDisplay,
            failedCountDisplay,
            queuedTimeDisplay,
            processingTimeDisplay,
            mediatorStatus,
            monetraStatus,
            israelPremiumStatus;
            // NOTE: Removed unused authorization platforms.

        // Forms
        private Lazy<EventLogForm> eventLogForm = new Lazy<EventLogForm>();
        private Lazy<AboutForm> aboutForm = new Lazy<AboutForm>(new Func<AboutForm>(() => new AboutForm(launchedAt)));
        #endregion

        #region Configuration Members
        string Monetra_HostName;
        ushort Monetra_ServerSocket;
        int PollIntervalSeconds;
        #endregion

        #region General Fields
        private ServerController<CctmMediator> mediator;
        private static string launchedAt = DateTime.Now.ToString();
        private Dictionary<decimal, ListViewItem> displayedTransactions = new Dictionary<decimal, ListViewItem>();
        System.Diagnostics.PerformanceCounter performanceCounter = new System.Diagnostics.PerformanceCounter(
                "Process",
                "% Processor Time",
                Process.GetCurrentProcess().ProcessName);
        double CpuLoadState1 = 0;
        double CpuLoadState2 = 0;
        bool listViewAutoScroll = true;
        ListViewItem autoScrollTarget = null;
        private bool extendedDatabaseLogging;
                    
        #endregion

        public CctmForm(bool initializeVisual, bool extendedDatabaseLogging)
        {
            if (initializeVisual)
                InitializeComponent();
            this.extendedDatabaseLogging = extendedDatabaseLogging;
        }

        public void tickWatchDog()
        {
            if (!hidden)
            {
                this.Invoke(new Action(() =>
                    {
                        string fileName = "WatchDog.txt";
                        File.WriteAllText(fileName, "Updated" + DateTime.Now.ToString() + Environment.NewLine);
                    }
                    ));
            }
        }

    } //  =====================  public partial class CctmForm   ======================  
}

