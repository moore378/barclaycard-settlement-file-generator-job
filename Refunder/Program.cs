using AutoDatabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptographicPlatforms;
using Common;
using RsaUtils;

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
                    Console.WriteLine("Do you want to refund TTID=" + pendingRefund.TTID + " for " + pendingRefund.CCAmount + "? (Yes/No/Auto/eXit)");
                    switch (Char.ToUpper(Console.ReadKey().KeyChar))
                    {
                        case 'N': continue;
                        case 'X': return;
                        case 'Y': break;
                        case 'A': interactive = false; break;
                    }
                }

                Log("----------ttid: " + pendingRefund.TTID);

                var trans = monetra.TransNew();

                monetra.TransKeyVal(trans, "username", pendingRefund.CCTerminalID.Trim());
                monetra.TransKeyVal(trans, "password", pendingRefund.CCTransactionKey);
                
                if (pendingRefund.CCTracks.Length == 0) // still in DB?
                {
                    monetra.TransKeyVal(trans, "ttid", pendingRefund.TTID.ToString());
                    Log("Found - using TTID");
                }
                else //need to decrypt and give PAN
                {
                    Log("Encrypted - processing PAN");

                    EncryptedStripe encryptedDecodedStripe;
                    string s = pendingRefund.CCTracks;

                    encryptedDecodedStripe = DatabaseFormats.DecodeDatabaseStripe(s);
                    string encryptedString = BytesToHexStr(encryptedDecodedStripe).ToUpper();

                    RsaUtils.RsaUtility RsaUtl = new RsaUtility();
                    string decryptedAsciiHexString = RsaUtl.RsaDecrypt(encryptedString, (ushort)pendingRefund.KeyVer);

                    //monetra Expects mmyy not yymm as expiry Date - need to manipulate

                    string ModifiedCCExpiryDate = pendingRefund.CCExpiryDate.Substring(2, 1) + pendingRefund.CCExpiryDate.Substring(3, 1) + pendingRefund.CCExpiryDate.Substring(0, 1) + pendingRefund.CCExpiryDate.Substring(1, 1);

                    Log("PAN: " + pendingRefund.CreditCallPAN + " exp:" + pendingRefund.CCExpiryDate + " => " + ModifiedCCExpiryDate);
                    //PCI - DON'T LOG DECRYPTED PAN - CreditCallPAN is first 6 and last 4
                    //Log("PAN: " + decryptedAsciiHexString + "  -  " + pendingRefund.CreditCallPAN + " exp:" + pendingRefund.CCExpiryDate + " => " + ModifiedCCExpiryDate);

                    monetra.TransKeyVal(trans, "account", decryptedAsciiHexString);
                    monetra.TransKeyVal(trans, "expdate", ModifiedCCExpiryDate);
                }


                monetra.TransKeyVal(trans, "amount", Math.Abs(pendingRefund.CCAmount).ToString());
                monetra.TransKeyVal(trans, "action", "return");

                //actually send it to Monetra
                var sent = monetra.TransSend(trans);
                
                if (!sent)
                    throw new Exception("Failed sending transaction to Monetra");

                Action<string> f = s => Log(s + ": " + monetra.ResponseParam(trans, s));
                Log("TransactionRecordID: " + pendingRefund.TransactionRecordID);
                Log("Amount: " + pendingRefund.CCAmount);
                Log("ttid_rq: " + pendingRefund.TTID.ToString());
                Log("CCTracks: " + pendingRefund.CreditCallPAN);
                f("code");
                f("phard_code");
                f("msoft_code");
                f("verbiage");
                f("auth");
                f("ttid");
                f("batch");
                f("ordernum");

                //Update the Database - if transaction fails batch is null and errors out the DB update below - ok
                Console.WriteLine("database update");
                await database.UpdTransactionrecordProcessedRefund(
                    pendingRefund.TransactionRecordID,
                    decimal.Parse(monetra.ResponseParam(trans, "ttid")),
                    short.Parse(monetra.ResponseParam(trans, "batch")),
                    monetra.ResponseParam(trans, "code").ToUpper() == "AUTH" ? (short)7 : (short)8,
                    DateTime.Now
                    );

                //Clear Transaction
                //  monetra.DeleteTrans(trans);
                Log("==========ttid: " + pendingRefund.TTID);
                     
            }
        }

        public static void Log(string msg)
        {
            Console.WriteLine(msg);
            File.AppendAllText("Log.txt", DateTime.Now.ToString() + ": " + msg + Environment.NewLine);
        }



            private static string BytesToHexStr(byte[] bytes)
            {
                string result = "";
                foreach (byte b in bytes)
                    result = result + b.ToString("x2");
                return result;
            }

            private static byte[] HexStrToBytes(string s)
            {
                byte[] result = new byte[s.Length / 2];
                for (int i = 0; i < s.Length / 2; i++)
                {
                    string tmp = s.Substring(i * 2, 2);
                    result[i] = byte.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
                }
                return result;
            }
        }

    }




