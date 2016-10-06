using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;
using System.Net.Sockets;
using System.IO;

using Common;
using TransactionManagementCommon;
using CryptographicPlatforms;
using Rtcc.RtsaInterfacing;
using UnitTests;

using System.Threading;
using System.Net;


namespace RtccClient
{
    [XmlRootAttribute("AuthRequestReply")]
    public struct ClientAuthResponseXml
    {
        [XmlElement(ElementName = "Accepted")]
        public int Accepted;

        [XmlElement(ElementName = "ReceiptReference")]
        public string ReceiptReference;

        [XmlElement(ElementName = "RespCode")]
        public string ResponseCode;

        [XmlElement(ElementName = "Amount")]
        public decimal AmountDollars;
    }

    public class RtccClient
    {
        string address;
        int port;

        private static readonly Object lockObject = new Object();

        private static ThreadLocal<XmlSerializer> authRequestSerializer = new ThreadLocal<XmlSerializer>(() => new XmlSerializer(typeof(ClientAuthRequestXML)));
        private static ThreadLocal<XmlSerializer> authResponseSerializer = new ThreadLocal<XmlSerializer>(() => new XmlSerializer(typeof(ClientAuthResponseXml)));

        //Function to get random number
        private static readonly Random getrandom = new Random();
        private static readonly object syncLock = new object();
        public static int GetRandomNumber(int min, int max)
        {
            lock (syncLock)
            { 
                // synchronize
                return getrandom.Next(min, max);
            }
        }

        public RtccClient(string address, int port)
        {
            this.address = address;
            this.port = port;
        }

        public void RunSimulation(int duration, int sleepDelay, int concurrent)
        {
            string testName = "Monetra DB5"; // Monetra DB5
            string testData = "Monetra"; // Monetra

            ClearingPlatform processorInfo = TestData.Processors[testName];

            AuthorizationRequestEntry entry = TestData.AuthRequests[testData][0];

            DateTime dtNow = DateTime.Now;

            DateTime dtStop = dtNow.AddSeconds(duration);

            int i = 0;

            using (var sem = new SemaphoreSlim(concurrent))
            { 
                var tasks = new List<Task>();

                do
                {
                    sem.Wait();

                    Log(String.Format("Adding task #{0}", i));
                    var task = SimulateAuthorization(i++, entry, sleepDelay);
                    //var task = SimulateTask(entry, sleepDelay);

                    task.ContinueWith((t) => 
                    { 
                        sem.Release();

                        lock (lockObject)
                        {
                            tasks.Remove(t);
                        }
                    });

                    lock (lockObject)
                    {
                        tasks.Add(task);
                    }
                } while (DateTime.Now < dtStop);

                Task.WaitAll(tasks.ToArray());
            }

            Log("Exiting...");
        }

        private byte[] ipsEncryptStripe(string stripe, TransactionInfo info, int keyVersion)
        {
            return CryptographicPlatforms.IPSTrackCipher.Encrypt(stripe, info.StartDateTime, (int)info.TransactionIndex,
                Int32.Parse(info.MeterSerialNumber), keyVersion);
        }

        private async Task SimulateTask(AuthorizationRequestEntry entry, int sleepDelay)
        {
            int sleepMilliseconds = GetRandomNumber(0, sleepDelay) * 1000;

            //sleepMilliseconds = 10000;

            Log(String.Format("Sleeping for {0} (1)", sleepMilliseconds, Thread.CurrentThread.ManagedThreadId));

            await Task.Delay(sleepMilliseconds);
        }

        private async Task SimulateAuthorization(int id, AuthorizationRequestEntry entry, int sleepDelay)
        {
            DateTime dtNow = DateTime.Now;

            // Generate the track data.

            TransactionInfo info = new TransactionInfo(
                amountDollars: 0.75m,
                meterSerialNumber: entry.MeterId,
                startDateTime: dtNow,
                transactionIndex: 6,
                refDateTime: dtNow);

            // After all that work to add the starting and ending sentinels... remove them for this track format.
            string tracksUnformatted = entry.CreditCard.Track1.Substring(1, entry.CreditCard.Track1.Length - 2).PadRight(88, '\0')
                + entry.CreditCard.Track2.Substring(1, entry.CreditCard.Track2.Length - 2).PadRight(40, '\0');

            byte[] encryptedData = ipsEncryptStripe(tracksUnformatted, info, 1);

            byte[] encodedBytes = System.Text.Encoding.ASCII.GetBytes(tracksUnformatted);

            string encodedBase64 = Convert.ToBase64String(encryptedData);

            ClientAuthRequestXML request = new ClientAuthRequestXML()
            {
                StructureVersion = "03",
                meterID = info.MeterSerialNumber,
                LocalStartDateTime = DateTime.SpecifyKind(dtNow, DateTimeKind.Utc),
                requestType = 0,
                ccTransactionIndex = (int)info.TransactionIndex,
                encryptionMethod = (int)EncryptionMethod.IpsEncryption,
                keyVer = 1,
                uniqueRecNo = TestData.GenerateUniqueId(),
                amount = info.AmountDollars.ToString(),
                transactionDesc = "",
                invoice = TestData.GenerateUniqueId(),
                ccTrackBase64 = encodedBase64,
                purchasedTime = 0,
                UniqueNumber2 = long.Parse(TestData.GenerateUniqueId()),
                Flags = 0
            };

            await Authorize(id, request);

            int sleepMilliseconds = GetRandomNumber(0, sleepDelay) * 1000;

            //sleepMilliseconds = 10000;

            Log(String.Format("Request #{0} sleeping for {1}", id, sleepMilliseconds));

            await Task.Delay(sleepMilliseconds);
        }

        private async Task Authorize(int id, ClientAuthRequestXML request)
        {
            byte[] tempBuf = new byte[4000];

            MemoryStream packetData = new MemoryStream(tempBuf);
            // Skip the length (come back to it later
            packetData.Seek(4, SeekOrigin.Begin);
            // Write all the fields
            authRequestSerializer.Value.Serialize(packetData, request);
            // Count bytes in xml
            int count = (int)(packetData.Position - 4);
            byte[] countBytes = BitConverter.GetBytes(count);
            // Write the count back to the beginning
            packetData.Seek(0, SeekOrigin.Begin);
            packetData.Write(countBytes, 0, countBytes.Length);
            // Send the data
            packetData.Seek(0, SeekOrigin.Begin);

            packetData.Close();

            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(address, port);

                NetworkStream stream = client.GetStream();

                Log(String.Format("Request #{0} sending to RTCC...", id));
                // Send the message to the connected TcpServer. 
                stream.Write(tempBuf, 0, count + 4);

                // Buffer to store the response bytes.
                byte[] data = new byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the response
                ClientAuthResponseXml response;

                try
                {
                    response = (ClientAuthResponseXml)authResponseSerializer.Value.Deserialize(stream);
                }
                catch (Exception e)
                {
                    Log(String.Format("Request #{0} Error parsing response {1}", id, e.Message));

                    // Return back a decline.
                    response = new ClientAuthResponseXml();
                }

                //responseFromRtcc = (ClientAuthResponseXml)authResponseSerializer.Value.Deserialize(logStream);
                //int read = stream.Read(data, 0, data.Length);

                //int length = BitConverter.ToInt32(data, 0);
                Log(String.Format("Request #{0} Accepted {1} code {2} reference {3}",
                    id,
                    response.Accepted,
                    response.ResponseCode,
                    response.ReceiptReference));

                client.Close();
            }
        }

        private void Log(string message)
        {
            Console.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss.fff"), message));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string address;
            int port;
            int duration = 60;
            int delay = 2;
            int threads = 1;

            if (2 > args.Length)
            {
                Console.WriteLine("Error, arguments needed: <address> <port> [duration] [delay] [threads]");
            }
            else
            {

                address = args[0];
                port = int.Parse(args[1]);

                if (3 <= args.Length)
                {
                    duration = int.Parse(args[2]);
                }

                if (4 <= args.Length)
                {
                    delay = int.Parse(args[3]);
                }

                if (5 <= args.Length)
                {
                    threads = int.Parse(args[4]);
                }

                RtccClient client = new RtccClient(address, port);

                client.RunSimulation(duration, delay, threads);
                //client.RunReceiptSimulationNormal(duration, delay, threads);
            }
        }
    }
}
