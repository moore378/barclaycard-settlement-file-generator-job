﻿using AutoDatabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refunder
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Options options = new Options();

                if (!CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    //Console.WriteLine(options.GetUsage());
                    return;
                }
                
                //var connectionString = "Data";// args[2];

                var monetra = new libmonetra.Monetra();

                monetra.SetSSL(options.MonetraHost, options.MonetraPort);
                monetra.SetBlocking(true);
                var connected = monetra.Connect();

                if (!connected)
                    throw new Exception("Could not connect to Monetra at " + options.MonetraHost + ":" + options.MonetraPort);

                IDatabase database = AutoDatabaseBuilder.CreateInstance<IDatabase>(new ConnectionSource(options.ConnectionString), new Tracker(Log));
                Run(database, monetra, !options.NonInteractive).Wait();

                monetra.DestroyConn();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine();
            }
        }

        private async static Task Run(IDatabase database, libmonetra.Monetra monetra, bool interactive)
        {
            var pendingRefunds = await database.SelPendingRefundsCctm();
            
            foreach (var pendingRefund in pendingRefunds)
            {
                if (!pendingRefund.TTID.HasValue)
                    continue;

                if (interactive)
                {
                    Console.WriteLine("Do you want to refund TTID=" + pendingRefund.TTID + " for " + pendingRefund.CCAmount + "? (y/n/a/x)");
                    switch (Char.ToUpper(Console.ReadKey().KeyChar))
                    {
                        case 'N': continue;
                        case 'X': return;
                        case 'Y': break;
                        case 'A': interactive = false; break;
                    }
                }

                var trans = monetra.TransNew();
                monetra.TransKeyVal(trans, "username", pendingRefund.CCTerminalID.Trim());
                monetra.TransKeyVal(trans, "password", pendingRefund.CCTransactionKey);
                monetra.TransKeyVal(trans, "ttid", pendingRefund.TTID.ToString());
                monetra.TransKeyVal(trans, "amount", Math.Abs(pendingRefund.CCAmount).ToString());
                monetra.TransKeyVal(trans, "action", "return");

                var sent = monetra.TransSend(trans);
                if (!sent)
                    throw new Exception("Failed sending transaction to Monetra");

                Log("----------ttid: " + pendingRefund.TTID);

                Action<string> f = s => Log(s + ": " + monetra.ResponseParam(trans, s));
                Log("TransactionRecordID: " + pendingRefund.TransactionRecordID);
                Log("Amount: " + pendingRefund.CCAmount);
                f("code");
                f("phard_code");
                f("msoft_code");
                f("verbiage");
                f("auth");
                f("ttid");
                f("batch");
                f("ordernum");

                await database.UpdTransactionrecordProcessedRefund(
                    pendingRefund.TransactionRecordID,
                    decimal.Parse(monetra.ResponseParam(trans, "ttid")),
                    short.Parse(monetra.ResponseParam(trans, "batch")),
                    monetra.ResponseParam(trans, "code").ToUpper() == "AUTH" ? (short)7 : (short)8,
                    DateTime.Now
                    );

                monetra.DeleteTrans(trans);
            }
        }

        public static void Log(string msg)
        {
            Console.WriteLine(msg);
            File.AppendAllText("Log.txt", DateTime.Now.ToString() + ": " + msg + Environment.NewLine);
        }
    }

    
}
