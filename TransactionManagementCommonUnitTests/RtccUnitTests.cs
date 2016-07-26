using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Rtcc;
using Rtcc.Main;
using CryptographicPlatforms;
using Common;
using AuthorizationClientPlatforms;
using TransactionManagementCommon;
using Rtcc.RtsaInterfacing;
using Rtcc.Dummy;
using Rtcc.Database;
using Rtcc.PayByCell;

using AuthorizationClientPlatforms.Settings;

using System.Threading;

namespace UnitTests
{

    public class UnitTestDatabase : RtccDatabase
    {
        private string _testDataName;

        public UnitTestDatabase(string testDataName)
        {
            _testDataName = testDataName;
        }

        public UnitTestDatabase()
            : this("Monetra DB5")
        {
            // Nothing on purpose.
        }

        public override decimal InsertLiveTransactionRecord(
            string TerminalSerNo,
            string ElectronicSerNo,
            decimal? TransactionType,
            DateTime? StartDateTime,
            decimal? TotalCreditCents,
            decimal? TimePurchased,
            decimal? TotalParkingTime,
            decimal? AmountCents,
            string CCTracks,
            string CCTransactionStatus,
            decimal? CCTransactionIndex,
            string CoinCount,
            decimal? EncryptionVer,
            decimal? KeyVer,
            string UniqueRecordNumber,
            long UniqueRecordNumber2,
            string CreditCallCardEaseReference,
            string CreditCallAuthCode,
            string CreditCallPAN,
            string CreditCallExpiryDate,
            string CreditCallCardScheme,
            string FirstSixDigits,
            string LastFourDigits,
            short mode,
            short status)
        {
            // Do nothing
            return 0;
        }

        public override void UpdateTransactionStatus(
            int transactionRecordID, TransactionStatus oldStatus, TransactionStatus newStatus, string newStatusStr)
        {

        }

        public override CCProcessorInfo GetRtccProcessorInfo(string terminalSerialNumber)
        {
            ClearingPlatform processorInfo = TestData.Processors[_testDataName];

            CCProcessorInfo data = new CCProcessorInfo()
            {
                CompanyName = "DummyCompany",
                MerchantID = processorInfo.MerchantId,
                MerchantPassword = processorInfo.MerchantPassword,
                TerminalSerialNumber = "DummyTerminal",
                PoleSerialNumber = "DummyPole",
                ClearingPlatform = processorInfo.Name
            };

            if (data.ClearingPlatform.ToLower() == "fis-paydirect")
            {
                // Hack. Todo need to be done better.
                data.ProcessorSettings["SettleMerchantCode"] = "50BNA-PUBWK-PARKG-00";
            }

            return data;
        }

        public override void UpdateLiveTransactionRecord(decimal transactionRecordID, string tracks, string statusString, string authCode, string cardType, string obscuredPan, short batchNum, int ttid, short status, decimal ccFee)
        {
            // Do nothing
        }
    }


    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TrackDecryptor
    {
        DateTime someDateTime;

        public TrackDecryptor()
        {
            someDateTime = DateTime.Now;
            // Write the ini file for RsaUtils.dll
            string[] settings = 
            { 
                "[Folders]",
                "publicKeyFolder  = C:\\Keys\\Pub\\",
                "privateKeyFolder = C:\\Keys\\Priv\\"
            };
            System.IO.File.WriteAllLines("RsaUtils.ini", settings);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        private byte[] asciiEncodeStripe(string stripe)
        {
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            return asciiEncoding.GetBytes(stripe);
        }

        private string testStripe1
        {
            get
            {
                return "%-this represents track 1-?;123456=1504000-descretionary data-?";
            }
        }

        private TransactionManagementCommon.TransactionInfo testInfo
        {
            get
            {
                TransactionManagementCommon.TransactionInfo result = new TransactionManagementCommon.TransactionInfo(
                    startDateTime: someDateTime,
                    transactionIndex: 43,
                    meterSerialNumber: "000044",
                    amountDollars: 0,
                    refDateTime: someDateTime
                    );
                return result;
            }
        }

        private byte[] ipsEncryptStripe(string stripe, TransactionManagementCommon.TransactionInfo info, int keyVersion)
        {
            return CryptographicPlatforms.IPSTrackCipher.Encrypt(stripe, info.StartDateTime, (int)info.TransactionIndex,
                Int32.Parse(info.MeterSerialNumber), keyVersion);
        }

        private UnencryptedStripe ipsDecryptStripe(EncryptedStripe stripe, TransactionManagementCommon.TransactionInfo info, int keyVersion)
        {
            return CryptographicPlatforms.IPSTrackCipher.Decrypt(stripe, info.RefDateTime??info.StartDateTime, (int)info.TransactionIndex,
                Int32.Parse(info.MeterSerialNumber), keyVersion);
        }

        private byte[] rsaEncryptStripe(string stripe, int keyVersion)
        {
            return CryptographicPlatforms.RsaCipher.Encrypt(stripe, keyVersion);
        }


        [TestMethod]
        public void TestCCCryptoHashPan()
        {
            string pan = "4444333322221111";

            string hash = CreditCardHashing.HashPAN(pan);

            Console.WriteLine(hash);

            System.Security.Cryptography.MD5 hasher = System.Security.Cryptography.MD5.Create();

            byte[] newhash = hasher.ComputeHash(Encoding.ASCII.GetBytes(pan));

            System.Diagnostics.Debug.WriteLine("testsad");
            System.Diagnostics.Debug.WriteLine("hello");

            System.Diagnostics.Debug.WriteLine(hasher);

            System.Diagnostics.Debug.WriteLine("world");
        }

        [TestMethod]
        public void TestHash()
        {
            string pan = "4012000033330026";
            string expiry = "2012";


            ThreadLocal<System.Security.Cryptography.MD5> hasher = new ThreadLocal<System.Security.Cryptography.MD5>(() => System.Security.Cryptography.MD5.Create());

            string hash = String.Concat(hasher.Value.ComputeHash(Encoding.ASCII.GetBytes(";" + pan + "=" + expiry + "?")).Select(b => b.ToString("X2")));

            System.Diagnostics.Debug.WriteLine(hash);

            string newhash = String.Concat(hasher.Value.ComputeHash(Encoding.ASCII.GetBytes("4444333322221111")).Select(b => b.ToString("X2")));

            System.Diagnostics.Debug.WriteLine(newhash);

        }

        [TestMethod]
        public void UnencryptedStripe()
        {
            // Create test track
            byte[] testStripe = asciiEncodeStripe(testStripe1);
            // Decrypt it
            TransactionManagementCommon.StripeDecryptor testDecryptor = new TransactionManagementCommon.StripeDecryptor();
            string decryptedTrack = testDecryptor.decryptStripe(testStripe, EncryptionMethod.Unencrypted, 0, testInfo, "");
            Assert.AreEqual(testStripe1, decryptedTrack);
        }

        enum TrackEncodingMethod
        {
            Ascii, // 1 byte for 1 char
            Base16 // Hex-ascii (1 byte for 2 chars)
        };

        /// <summary>
        /// This method decodes (not decrypts) a track from a specific encoding into binary bytes
        /// </summary>
        /// <param name="encodedEncryptedTrack"></param>
        /// <param name="encodingMethod"></param>
        /// <returns>Decoded, encrypted track</returns>
        private byte[] decodeTrack(byte[] encodedEncryptedTrack, TrackEncodingMethod encodingMethod)
        {
            switch (encodingMethod)
            {
                case TrackEncodingMethod.Ascii:
                    return encodedEncryptedTrack;

                case TrackEncodingMethod.Base16:
                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                    string encodedEncryptedTrackString = encoding.GetString(encodedEncryptedTrack);

                    // Convert hex sequence to string
                    int asciiHexStringLen = encodedEncryptedTrack.Length / 2;
                    byte[] result = new byte[asciiHexStringLen];
                    for (int i = 0; i < asciiHexStringLen; i++)
                    {
                        result[i] = Convert.ToByte(encodedEncryptedTrackString.Substring(i * 2, 2), 16);
                    }
                    return result;

                default:
                    throw new FormatException("Invalid encoding track scheme");
            }
        }

        [TestMethod]
        public void IpsEncryption()
        {
            TransactionManagementCommon.TransactionInfo info = new TransactionManagementCommon.TransactionInfo(
                startDateTime: DateTime.Parse("2010/04/17 12:09:13"),
                meterSerialNumber: "0",
                transactionIndex: 6790,
                amountDollars: 0,
                refDateTime: DateTime.Parse("2010/04/17 12:09:13"));

            // Create test track
            string plainStripe = ";234567890123=3412567?".PadLeft(128, '\0');

            // Encrypt it
            byte[] encryptedStripe = ipsEncryptStripe(plainStripe, info, 1);


            // NOTE: The following TransactionInfo properties need to be 
            // the same value between encryption and decryption:
            // - MeterSerialNumber
            // - TransactionIndex
            // - RefDateTime

            //info.AmountDollars += 50;
            //info.MeterSerialNumber = "2323";
            //info.StartDateTime = DateTime.Now;
            //info.TransactionIndex += 2323;
            //info.RefDateTime = DateTime.Now;
            
            // Decrypt it
            string decryptedStripe = ipsDecryptStripe(encryptedStripe, info, 1);

            // Is it the same as the original?
            Assert.AreEqual<string>(plainStripe, decryptedStripe);
        }
                
        [TestMethod]
        public void RsaEncryptedStripe()
        {
            // Create test track
            byte[] testStripe = rsaEncryptStripe(testStripe1 + "012345678901234567890123456789012345678901234567890123", 1122);
            // Decrypt it
            TransactionManagementCommon.StripeDecryptor testDecryptor = new TransactionManagementCommon.StripeDecryptor();
            string decryptedTrack = testDecryptor.decryptStripe(testStripe, EncryptionMethod.RsaEncryption,
                1122, testInfo, "");
            // Is it the same as the original?
            Assert.AreEqual(testStripe1.Substring(0, decryptedTrack.Length), decryptedTrack);
        }

        private void Log(object sender, LogEventArgs args)
        {
            Console.WriteLine(args.Message);
        }

        [TestMethod]
        public void XMLDecode()
        {
            System.Text.ASCIIEncoding encoding = new ASCIIEncoding();
            string xml = "<AuthRequest><StructureVersion>03</StructureVersion>\r<MeterID>35189</MeterID>\r<LocalStartDateTime>2010-04-19T11:07:09+00:00</LocalStartDateTime>\r<CCTransactionIndex>6</CCTransactionIndex>\r<EncryptionMethod>1</EncryptionMethod>\r<KeyVer>1</KeyVer>\r<RequestType>0</RequestType>\r<UniqueRecNo>000003518920100419110709</UniqueRecNo>\r<MerchantID>test</MerchantID>\r<TransactionDesc></TransactionDesc>\r<Invoice></Invoice>\r<CCTrack>50XIzZeEdzG07Er4meKc057l3sWg8Sax7Aug3H/l44m3+lGg4+Nu7ZLL3ZZm2ZPQslEdqirkW+modxQ8M7KyYbASjCjDrqE2DdqApMicH0ao5TEaUAV5+k1zK22b6UT1w8s1k0cA2dPp3pN3xW6PvVhG4cHlFmuoX12CSpODcWc=</CCTrack>\r<Amount>0.75</Amount>\r<MerchantPassword>test_server</MerchantPassword>\r</AuthRequest>";
            
            RtccRequestInterpreter interpreter = new RtccRequestInterpreter();
            interpreter.Logged += Log;
            
            ClientAuthRequest request = interpreter.ParseMessage(encoding.GetBytes(xml), "");

            // Check the results
            Assert.AreEqual(request.AmountDollars, 0.75m);
            byte[] originalTrackBytes = Convert.FromBase64String("50XIzZeEdzG07Er4meKc057l3sWg8Sax7Aug3H/l44m3+lGg4+Nu7ZLL3ZZm2ZPQslEdqirkW+modxQ8M7KyYbASjCjDrqE2DdqApMicH0ao5TEaUAV5+k1zK22b6UT1w8s1k0cA2dPp3pN3xW6PvVhG4cHlFmuoX12CSpODcWc=");
            Assert.IsTrue(request.EncryptedTrack.Data.SequenceEqual(originalTrackBytes));
        }

        private ClientAuthResponse dummyResponse;

        private void dummyResponseReceived(ClientAuthResponse response)
        {
            this.dummyResponse = response;
        }

        [TestMethod]
        public void RtccMediator()
        {
            string testName = "FIS PayDirect Test"; // Monetra DB5
            string testData = "FIS Certification"; // Monetra

            ClearingPlatform processorInfo = TestData.Processors[testName];

            AuthorizationRequestEntry entry = TestData.AuthRequests[testData][0];

            //byte[] original = Convert.FromBase64String("50XIzZeEdzG07Er4meKc057l3sWg8Sax7Aug3H/l44m3+lGg4+Nu7ZLL3ZZm2ZPQslEdqirkW+modxQ8M7KyYbASjCjDrqE2DdqApMicH0ao5TEaUAV5+k1zK22b6UT1w8s1k0cA2dPp3pN3xW6PvVhG4cHlFmuoX12CSpODcWc=");

            DateTime dtNow = DateTime.Now; // DateTime.SpecifyKind(DateTime.Parse("2016-04-19T11:07:09+00:00").ToUniversalTime(), DateTimeKind.Unspecified);

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


            // Forcing only track 2.
            //tracksUnformatted = "".PadRight(88, '\0') + entry.CreditCard.Track2.Substring(1, entry.CreditCard.Track2.Length - 2).PadRight(40, '\0');

            // Forcing only Track 1.
            //tracksUnformatted = entry.CreditCard.Track1.Substring(1, entry.CreditCard.Track1.Length - 2).PadRight(88, '\0') + "".PadRight(40, '\0');

            // Cheap test code for verifying / validating the data being sent out.
            {
                byte[] encodedBytes = System.Text.Encoding.ASCII.GetBytes(tracksUnformatted);

                string encodedBase64 = Convert.ToBase64String(encodedBytes);

                // Track 1 and Track 2
                //encodedBase64 = "QjQwNTUwMTExMTExMTExMTFeVEVTVCBDQVJEL1ZTXjE4MDYxMDE1NDMyMTEyMzQ1Njc4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADQwNTUwMTExMTExMTExMTE9MTgwNjEwMTU0MzIxMTIzNDU2NwAAAAA=";

                // Track 2 only
                //encodedBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADQwNTUwMTExMTExMTExMTE9MTgwNjEwMTU0MzIxMTIzNDU2NwAAAAA=";

                // Track 1 only
                encodedBase64 = "QjU0NTQ1NDU0NTQ1NDU0NTReVEVTVCBDQVJEL01DXjE4MDYxMDE1NDMyMTEyMzQ1Njc4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADU0NTQ1NDU0NTQ1NDU0NTQ9MTgwNjEwMTU0MzIxMTIzNDU2NwAAAAA=";

                System.Diagnostics.Debug.WriteLine("Base 64 encoded {0}", encodedBase64);

                byte[] decodedBytes = Convert.FromBase64String(encodedBase64);

                string decodedBase64 = new String(System.Text.Encoding.ASCII.GetChars(decodedBytes));
            }

            byte[] encryptedData = ipsEncryptStripe(tracksUnformatted, info, 1);

            // Create a dummy message source
            var rtsaConnection = new DummyRtsaConnection();
            // Create a dummy interpretter
            DummyInterpreter interpreter = new DummyInterpreter();
            interpreter.Request = new ClientAuthRequest(
                info.MeterSerialNumber,
                dtNow,
                0,
                (int) info.TransactionIndex,
                EncryptionMethod.IpsEncryption,
                1,
                0,
                "000003518920100419110709",
                info.AmountDollars,
                "",
                TestData.GenerateUniqueId(),
                encryptedData,
                0,
                0,
                0
            );
            interpreter.onResponse = dummyResponseReceived;
            // Create a dummy database
            //RtccDatabase dummyDatabase = new DummyDatabase();
            RtccDatabase dummyDatabase = new UnitTestDatabase(testName);
            // Create a dummy authorization platform
            AuthorizationClientPlatforms.IAuthorizationPlatform dummyPlatform = new DummyAuthorizationPlatform();
            // Create a dummy PayByCell
            PayByCellClient dummyPayByCell = new DummyPayByCell();
            // Create dummy performance counters
            var dummyPerformanceCounters = new DummyPerformanceCounters();
            
            Dictionary<string, IAuthorizationPlatform> platforms = new Dictionary<string,IAuthorizationPlatform>();

            switch (processorInfo.Name.ToLower())
            {
                case "monetra":
                    // Create the monetra platform
                    MonetraClient _monetraClient = new MonetraDotNetNativeClient(processorInfo.Server, (ushort)processorInfo.Port, (log) => { System.Diagnostics.Debug.WriteLine(log); });
                    // The logic that uses the client to make authorizations
                    IAuthorizationPlatform monetraAuthorizer = new Monetra(_monetraClient, (log) => { System.Diagnostics.Debug.WriteLine(log); });
                    platforms["monetra"] = monetraAuthorizer;
                    break;

                case "fis-paydirect":
                    AuthorizationClientPlatformsSection acpSection = (AuthorizationClientPlatformsSection)System.Configuration.ConfigurationManager.GetSection("authorizationClientPlatforms");

                    AuthorizationProcessorsCollection apCollection = acpSection.AuthorizationProcessors;

                    ProcessorElement fisPayDirect = apCollection["fis-paydirect"];

                    Dictionary<string, string> configuration = new Dictionary<string, string>();
                    configuration["endpoint"] = fisPayDirect.Endpoint;

                    platforms["fis-paydirect"] = new AuthorizationPlatform(System.Net.Dns.GetHostName(), fisPayDirect.Name, configuration);
                    break;

                default:
                    break;
            }
            
            // Create the mediator
            RtccMediator mediator = new RtccMediator(platforms, rtsaConnection, dummyPerformanceCounters,
                dummyDatabase, interpreter, dummyPayByCell);
            mediator.Logged += Log;

            // Simulate a blank request from the client (the dummy interpreter will pretend that this blank request contains useful information)
            rtsaConnection.SimulateMessageReceived(new byte[0]);
            
            Assert.IsTrue(dummyResponse.Accepted == 1);
            //Assert.AreEqual(dummyResponse.ResponseCode, "DummyAuthCode");
        }


        private class DummyInterpreter : RtccRequestInterpreter
        {
            public ClientAuthRequest Request;

            public delegate void Response(ClientAuthResponse reply);

            public Response onResponse;

            /// <summary>
            /// Pretends to parse the message, but in fact just returns the request object specified by this.Request.
            /// </summary>
            /// <param name="message"></param>
            /// <returns></returns>
            public override ClientAuthRequest ParseMessage(byte[] message, string failStatus)
            {
                return Request;
            }
            public override RawDataMessage SerializeResponse(ClientAuthResponse reply)
            {
                onResponse(reply);
                return new RawDataMessage(new System.IO.MemoryStream());
            }
        }
        
        private class DummyAuthorizationPlatform : AuthorizationClientPlatforms.IAuthorizationPlatform
        {
            private AuthorizationClientPlatforms.AuthorizationStatistics stats = new AuthorizationClientPlatforms.AuthorizationStatistics();

            public AuthorizationClientPlatforms.AuthorizationResponseFields Authorize(AuthorizationClientPlatforms.AuthorizationRequest request, AuthorizeMode mode)
            {
                // Do nothing
                return new AuthorizationClientPlatforms.AuthorizationResponseFields(AuthorizationClientPlatforms.AuthorizationResultCode.Approved,
                    "DummyAuthCode", "DummyCardType", "DummyReference", "DummyNote", 0, 0);
            }


            public AuthorizationClientPlatforms.IAuthorizationStatistics Statistics
            {
                get { return stats; }
            }


            public AuthorizationClientPlatforms.FinalizeResponse Finalize(FinalizeRequest request)
            {
                return new AuthorizationClientPlatforms.FinalizeResponse();
            }
        }
    }

    
}
