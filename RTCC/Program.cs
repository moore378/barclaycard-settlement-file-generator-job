using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Rtcc.Main;
using TransactionManagementCommon;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.ServiceProcess;

using CryptographicPlatforms;


[assembly: InternalsVisibleTo("TransactionManagementUnitTests")]

namespace Rtcc
{
    static class Program
    {
        private static string logFolder = Properties.Settings.Default.LogFolder;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
          //  StripeDecryptor decryptor = new StripeDecryptor();
          //  var request = new Common.EncryptedStripe(new byte[0]);
          //  var req=""; 
          //  var sysdt = new DateTime();

          //  sysdt = Convert.ToDateTime("02/22/2016 17:55:00");

         //   request =    DatabaseFormats .encoding.GetBytes("3F07E554F9182ECAD577527286FE3BCFCD7EC81E4FD1C506D1515F0517E0A62776DB630970094A041D34324C07C64651CA1204CD57513B0548E3125151DC5F20796F24AA7A337312F1BF098846F94B0AD5B69E911010C312841FB9523C33A49E4C13336839BEE20D4A91B8F3A5D958DAE8B9FE087ED5615A5FAE02D797737846");

         //   req = IPSTrackCipher.Decrypt(request, sysdt, 28, 12345, 1);

          //  MessageBox.Show(req);
            
            
            
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

            var configs = new RtccConfigs();
            configs.LoadFromFile();
            var rtccMain = new RtccMain(configs);
            rtccMain.Logged += RtccLogged;

            if (args.Length == 1 && args[0] == "gui")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                
                var mainForm = new RtccForm(rtccMain, configs);
                Application.Run(mainForm);
            }
            else
            {
                ServiceBase.Run(new RtccService(rtccMain, configs));
            }
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            File.AppendAllText("ThreadExceptions.txt",
                Environment.NewLine +
                DateTime.Now.ToString() +
                Environment.NewLine +
                e.Exception.ToString() + Environment.NewLine);
            Environment.FailFast("RTCC failed with unhandled thread exception. See ThreadExceptions.txt");
        }

        [HandleProcessCorruptedStateExceptions] // Forces it to run even if its a stack or heap problem
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.AppendAllText("UnhandledExceptions.txt", 
                Environment.NewLine + 
                DateTime.Now.ToString() + 
                Environment.NewLine +
                e.ExceptionObject.ToString() + Environment.NewLine);
            Environment.FailFast("RTCC failed with unhandled exception. See UnhandledExceptions.txt");
        }

        static void RtccLogged(object sender, LogEventArgs args)
        {
            // Log to file
            normalLogFile.Log(args.Message);

            // Log to exception file
            if (args.Level == LogLevel.Error)
            {
                if (args.Exception != null)
                    exceptionLogFile.Log(args.Message + Environment.NewLine + args.Exception.ToString());
                else
                    exceptionLogFile.Log(args.Message);
            }
        }

        static LogFile exceptionLogFile = new LogFile(Path.Combine(logFolder, "Exceptions_"), ".txt");
        static LogFile normalLogFile = new LogFile(Path.Combine(logFolder, "Transactions_"), ".txt");
    }

     

}
