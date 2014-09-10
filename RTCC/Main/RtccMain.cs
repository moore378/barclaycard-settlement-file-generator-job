using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using TransactionManagementCommon.ControllerBase;
using AuthorizationClientPlatforms;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Rtcc.RtsaInterfacing;
using Rtcc.PayByCell;

namespace Rtcc.Main
{
    class RtccMain : LoggingObject
    {
        public event EventHandler<TransactionDoneEventArgs> TransactionDone;
        private TcpListener tcpListener;
        private Thread listenThread;
        private RtccConfigs configs;
        private RtccPerformanceCounters rtccPerformanceCounters;

        public RtccMain(RtccConfigs configs)
        {
            this.configs = configs;
        }

        public void StartListening()
        {
            this.rtccPerformanceCounters = new RtccPerformanceCounters();
            this.tcpListener = new TcpListener(IPAddress.Any, Rtcc.Properties.Settings.Default.ListenPort);
            this.listenThread = new Thread(new ThreadStart(ListeningThreadProc));
            this.listenThread.IsBackground = true;
            this.listenThread.Start();
        }

        /// <summary>
        /// This is called as the start of the server thread to listen for clients
        /// </summary>
        private void ListeningThreadProc()
        {
            try
            {
                LogDetail("Connecting to Monetra server");

                // Use this authorizer for Monetra
                var monetraFactory = new SimpleServerFactory<IAuthorizationPlatform>(() =>
                {
                    LogImportant("Starting new connection to monetra. " + GetTimeWithMiliseconds());
                    // Create the monetra client
                    MonetraClient _monetraClient = new MonetraDotNetNativeClient(configs.MonetraHostName, configs.MonetraPort, MonetraLog);
                    // The logic that uses the client to make authorizations
                    IAuthorizationPlatform _monetraAuthorizer = new Monetra(_monetraClient, MonetraLog);
                    return _monetraAuthorizer;
                });
                var controller = new ServerController<IAuthorizationPlatform>(
                    serverFactory: monetraFactory,
                    updatedStatus: (status) => { },
                    failedRestart: (error, tries) => { Thread.Sleep(5000); return RestartFailAction.Retry; }
                );

                var authorizationFailAction = new ServerController<IAuthorizationPlatform>.ExceptionHandler((excep, tries) =>
                {
                    LogError("Authorization server error. Queueing restart. " + GetTimeWithMiliseconds(), excep);
                    if (excep is AuthorizerProcessingException)
                        if ((((AuthorizerProcessingException)excep).AllowRetry) && (tries < 5))
                            return OperationFailAction.RestartAndRetry;
                        else
                            return OperationFailAction.AbortAndRestart;
                    else
                        return OperationFailAction.AbortAndRestart;
                });

                // Wrap the controller
                IAuthorizationPlatform monetraAuthorizer = new DynamicAuthorizationPlatform(
                    (request, preAuth) => controller.Perform(
                        (platform) => platform.Authorize(request, preAuth),
                        authorizationFailAction
                        ));

                var israelPremiumFactory = new SimpleServerFactory<IAuthorizationPlatform>(() =>
                    {
                        LogImportant("Starting new connection to Israel Premium. " + GetTimeWithMiliseconds());
                        try
                        {
                            return new AuthorizationClientPlatforms.IsraelPremium(Properties.Settings.Default.IsraelMerchantNumber, Properties.Settings.Default.IsraelCashierNumber); 
                        }
                        catch (Exception e)
                        {
                            LogImportant(e.Message);
                            LogDetail(e.ToString());
                            throw;
                        }
                    });

                var israelController = new ServerController<IAuthorizationPlatform>(
                    serverFactory: israelPremiumFactory, 
                    updatedStatus: (status) => { },
                    failedRestart: (error, tries) => { Thread.Sleep(5000); return RestartFailAction.Retry; }
                );


                IAuthorizationPlatform israelPremium = new DynamicAuthorizationPlatform(
                    (request, preAuth) => israelController.Perform(
                        (platform) => platform.Authorize(request, preAuth),
                        authorizationFailAction
                        ));

                // Open the port to listen for connections
                this.tcpListener.Start();

                LogDetail("Listening for clients... (" + Rtcc.Properties.Settings.Default.ListenPort + ")");

                while (true)
                {
                    // Wait until we accept a client
                    TcpClient client = this.tcpListener.AcceptTcpClient();

                    LogDetail("Client accepted... " + client.Client.RemoteEndPoint.ToString());

                    // Create our wrapper object for the client
                    RtsaConnection rtsaConnection = new RtsaConnection(client);
                    rtsaConnection.Logged += ChildLogged;

                    // This ties everything together... reading from the interface and processing using the authorizer
                    RtccMediator requestProcessor = new RtccMediator(monetraAuthorizer, israelPremium, rtsaConnection, rtccPerformanceCounters);
                    requestProcessor.TransactionDone += TransactionDone;
                    requestProcessor.Logged += ChildLogged;
                    // Note: To see what happens next (when a request is received), go to RTCC.RtccMediator.ProcessRequest

                    rtsaConnection.Start();
                }
            }
            catch (Exception exception)
            {
                LogError("Fatal Exception! TCP server thread terminated. " + exception.Message, exception);
            }
        }

        private void OnTransactionDone(object sender, TransactionDoneEventArgs args)
        {
            var temp = TransactionDone;
            if (temp != null)
                temp(sender, args);
        }

        private void MonetraLog(string msg)
        {
            LogDetail(msg);
        }

        private static string GetTimeWithMiliseconds()
        {
            return DateTime.Now.ToString("HH:mm:ss.ffffff");
        }

    }
}
