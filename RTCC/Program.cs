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

[assembly: InternalsVisibleTo("TransactionManagementUnitTests")]

namespace Rtcc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var configs = new RtccConfigs();
            configs.LoadFromFile();
            var rtccMain = new RtccMain(configs);
            rtccMain.Logged += RtccLogged;
            var mainForm = new RtccForm(rtccMain, configs);
            Application.Run(mainForm);
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

        static LogFile exceptionLogFile = new LogFile("logs\\Exceptions_", ".txt");
        static LogFile normalLogFile = new LogFile("logs\\Transactions_", ".txt");
    }
}
