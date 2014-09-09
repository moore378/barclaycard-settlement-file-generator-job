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

namespace UnitTests
{
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
            Assert.AreEqual(request.AmountDollars, 0.75);
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
            // Create a dummy message source
            var rtsaConnection = new DummyRtsaConnection();
            // Create a dummy interpretter
            DummyInterpreter interpreter = new DummyInterpreter();
            interpreter.Request = new ClientAuthRequest(
                "35189",
                DateTime.SpecifyKind(DateTime.Parse("2010-04-19T11:07:09+00:00").ToUniversalTime(), DateTimeKind.Unspecified),
                0,
                6,
                EncryptionMethod.IpsEncryption,
                1,
                0,
                "000003518920100419110709",
                0.75m,
                "",
                "",
                Convert.FromBase64String("50XIzZeEdzG07Er4meKc057l3sWg8Sax7Aug3H/l44m3+lGg4+Nu7ZLL3ZZm2ZPQslEdqirkW+modxQ8M7KyYbASjCjDrqE2DdqApMicH0ao5TEaUAV5+k1zK22b6UT1w8s1k0cA2dPp3pN3xW6PvVhG4cHlFmuoX12CSpODcWc="),
                0,
                0,
                0
            );
            interpreter.onResponse = dummyResponseReceived;
            // Create a dummy database
            RtccDatabase dummyDatabase = new DummyDatabase();
            // Create a dummy authorization platform
            AuthorizationClientPlatforms.IAuthorizationPlatform dummyPlatform = new DummyAuthorizationPlatform();
            // Create a dummy PayByCell
            PayByCellClient dummyPayByCell = new DummyPayByCell();
            // Create dummy performance counters
            var dummyPerformanceCounters = new DummyPerformanceCounters();
            
            // Create the mediator
            RtccMediator mediator = new RtccMediator(dummyPlatform, dummyPlatform, rtsaConnection, dummyPerformanceCounters,
                dummyDatabase, interpreter, dummyPayByCell);
            mediator.Logged += Log;

            // Simulate a blank request from the client (the dummy interpreter will pretend that this blank request contains useful information)
            rtsaConnection.SimulateMessageReceived(new byte[0]);
            
            Assert.IsTrue(dummyResponse.Accepted == 1);
            Assert.AreEqual(dummyResponse.ResponseCode, "DummyAuthCode");
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
