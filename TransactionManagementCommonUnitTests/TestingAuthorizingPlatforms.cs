using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AuthorizationClientPlatforms;
using System.Diagnostics;

using System.Threading.Tasks;
using System.Net;

using AuthorizationClientPlatforms.Settings;
using System.Configuration;

namespace UnitTests
{
    public class ClearingPlatform
    {
        public string Name { get; set; }

        /// <summary>
        /// Server location
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Port for the Server
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Merchant ID
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// Merchant Password
        /// </summary>
        public string MerchantPassword { get; set; }

        /// <summary>
        /// Settlement Merchant Code
        /// FIS requires a different merchant code for settlement.
        /// </summary>
        public string SettleMerchantCode { get; set; }
    }

    public class CreditCard
    {
        private string _account;
        private string _expiry;
        private string _discretionary;
        private string _name;

        private string _track1;
        private string _track2;
        private string _fullTracks;

        private int _expiryYear;
        private int _expiryMonth;

        public CreditCard(string account)
            : this(account, null)
        {
            // Nothing to do.
        }

        public CreditCard(string account, string expiry)
            : this(account, expiry, "TEST NAME", "")
        {
            // Nothing to do.
        }

        public CreditCard(string account, string expiry, string name, string discretionary)
        {
            _account = account;

            // If the expiry is null, make the expiry be december of two years from now.
            if (String.IsNullOrEmpty(expiry))
            {
                DateTime expiryFuture = DateTime.Now.AddYears(2);

                _expiry = expiryFuture.ToString("yyMM");

                _expiryYear = expiryFuture.Year % 100;
                _expiryMonth = expiryFuture.Month;
            }
            else
            {
                int expiryFuture = int.Parse(expiry);

                // Anything else use as-is.
                _expiry = expiry;

                _expiryYear = expiryFuture / 100;
                _expiryMonth = expiryFuture % 100;
            }

            if (String.IsNullOrEmpty(name))
            {
                name = "";
            }
            _name = name;

            if (String.IsNullOrEmpty(discretionary))
            {
                discretionary = "";
            }
            _discretionary = discretionary;

            // Create Track 1
            _track1 = String.Format("%B{0}^{1}^{2}{3}?",
                _account,
                _name,
                _expiry,
                _discretionary
                );

            // Create Track 2
            _track2 = String.Format(";{0}={1}{2}?",
                _account,
                _expiry,
                _discretionary);

            _fullTracks = String.Format("{0}{1}", _track1, _track2);
        }

        public string FullTracks
        {
            get { return _fullTracks; }
        }

        public string Track1
        {
            get { return _track1; }
        }

        public string Track2
        {
            get { return _track2; }
        }

        public string AccountNumber
        {
            get { return _account; }
        }

        public string Expiry
        {
            get { return _expiry; }
        }

        public int ExpiryYear
        {
            get { return _expiryYear; }
        }

        public int ExpiryMonth
        {
            get { return _expiryMonth; }
        }
    }

    public class AuthorizationRequestEntry
    {
        private string _meterId = "0201501"; // "0000042";
        public string MeterId { get { return _meterId; } }

        public CreditCard CreditCard { get; set; }

        public decimal Amount { get; set; }

        public AuthorizationResultCode ResultCode { get; set; }
    }

    public static class TestData
    {
        private static Dictionary<string, List<AuthorizationRequestEntry>> _authRequests;

        private static int _uniqueIdCounter = 0;
        private static string _uniqueIdPrefix;

        private static void CreateAuthorizationRequestEntries()
        {
            List<AuthorizationRequestEntry> data;

            _authRequests = new Dictionary<string, List<AuthorizationRequestEntry>>();

            // Set up the data for Monetra
            data = new List<AuthorizationRequestEntry>();
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5454545454545454", null, "TEST CARD/MC", "1015432112345678"),
                Amount = 4.90m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5454545454545454", null, "TEST CARD/MC", "1015432112345678"),
                Amount = 6.01m,
                ResultCode = AuthorizationResultCode.Declined
            }
            );
            _authRequests["Monetra"] = data;


            // Set up the data for FIS
            data = new List<AuthorizationRequestEntry>();
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("343434343434343", null, "TEST CARD/AE", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("4055011111111111", null, "TEST CARD/VS", "1015432112345678"),
                Amount = 1.00m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5405222222222226", null, "TEST CARD/MC", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("6011000990139424", null, "TEST CARD/DS", "1015432112345678"),
                Amount = 1.00m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("4402130410000005", null, "TEST CARD/VS", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            _authRequests["FIS Certification"] = data;

            // Extra test cards from FIS
            data = new List<AuthorizationRequestEntry>();
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("4402130410000005"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("371449635398431"),
                Amount = 1.00m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5454545454545454"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5132850000000008"),
                Amount = 1.00m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("4055011111111111"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("6011000995500000"),
                Amount = 1.00m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            _authRequests["FIS Extra"] = data;
        }

        static TestData()
        {
            CreateClearingData();
            CreateAuthorizationRequestEntries();

            DateTime dtNow = DateTime.Now;
            _uniqueIdPrefix = String.Format("{0:00}{1:00}{2:00}", dtNow.Hour, dtNow.Minute, dtNow.Second);
        }

        private static Dictionary<string, ClearingPlatform> _processors;

        private static void CreateClearingData()
        {
            _processors = new Dictionary<string, ClearingPlatform>();

            _processors.Add("Monetra Test Box", new ClearingPlatform()
                {
                    Name = "monetra",
                    Server = "testbox.monetra.com",
                    Port = 8444,
                    MerchantId = "test_retail:public",
                    MerchantPassword = "publ1ct3st"
                });
            _processors.Add("Monetra DB5", new ClearingPlatform()
                {
                    Name = "monetra",
                    Server = "DB5",
                    Port = 8665,
                    MerchantId = "TEST123",
                    MerchantPassword = "test123"
                });
            _processors.Add("FIS PayDirect Test", new ClearingPlatform()
                {
                    Name = "fis-paydirect",
                    Server = Dns.GetHostName(),
                    Port = 8665,
                    MerchantId = "50BNA-PUBWK-PARKG-G",
                    MerchantPassword = "test2345",
                    SettleMerchantCode = "50BNA-PUBWK-PARKG-00"
                });
        }

        public static Dictionary<string, ClearingPlatform> Processors
        {
            get
            {
                return _processors;
            }
        }

        public static Dictionary<string, List<AuthorizationRequestEntry>> AuthRequests
        {
            get 
            {
                return _authRequests;
            }
        }

        public static string GenerateUniqueId()
        {
            string uniqueId;

            uniqueId = String.Format("{0}{1:000000}", _uniqueIdPrefix, _uniqueIdCounter++);

            return uniqueId;
        }

    }

    [TestClass]
    public class TestingMonetra
    {
        [TestMethod]
        public void MonetraTestServer()
        {
            ClearingPlatform processorInfo = TestData.Processors["Monetra DB5"];

            MonetraClient monetraClient = new MonetraDotNetNativeClient(processorInfo.Server, (ushort) processorInfo.Port, (log) => { Debug.WriteLine(log); });

            //MonetraClient monetraClient = new MonetraDotNetNativeClient("testbox.monetra.com", 8444, (log) => { Debug.WriteLine(log); });
            IAuthorizationPlatform monetra = new Monetra(monetraClient, (log) => { Debug.WriteLine(log); });

            List<Task> taskList = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                foreach (AuthorizationRequestEntry entry in TestData.AuthRequests["Monetra"])
                {
                    //var task = Task.Factory.StartNew(() =>
                    {
                        AuthorizationRequest request = new AuthorizationRequest(entry.MeterId,
                                    DateTime.Now, processorInfo.MerchantId, processorInfo.MerchantPassword, "", "",
                                    entry.Amount, "", "", "", entry.CreditCard.Track2,
                                    entry.CreditCard.FullTracks, "", "", null);
                        AuthorizationResponseFields response = monetra.Authorize(request, AuthorizeMode.Normal);
                        Assert.AreEqual(entry.ResultCode, response.resultCode);
                    }
                    //);

                    //taskList.Add(task);
                }
            }

            Task.WaitAll(taskList.ToArray());

            Debug.WriteLine("Completed");
        }
    }

    [TestClass]
    public class TestingFisPayDirect
    {
        [TestMethod]
        public void AuthorizationProcessorTest()
        {
            ClearingPlatform processorInfo = TestData.Processors["FIS PayDirect Test"];

            // Forcefully use the FIS PayDirect plugin instead of using any configuration defined in the app.config.
            IProcessorPlugin plugin = new AuthorizationClientPlatforms.Plugins.FisPayDirectPlugin();

            AuthorizationProcessor processor = new AuthorizationProcessor(plugin);

            Dictionary<string, string> configuration = new Dictionary<string, string>();

            configuration["endpoint"] = "https://paydirectapi.ca.link2gov.com/ApiService.svc/ApiService.svc";

            processor.Initialize(configuration);

            List<Task> taskList = new List<Task>();

            //for (int i = 0; i < 10; i++)
            {
                foreach (AuthorizationRequestEntry entry in TestData.AuthRequests["FIS Certification"])
                {
                    string uniqueId = TestData.GenerateUniqueId();

                    //var task = Task.Factory.StartNew(() =>
                    {
                        AuthorizationRequest request = new AuthorizationRequest(entry.MeterId,
                                    DateTime.Now, processorInfo.MerchantId, processorInfo.MerchantPassword, "", "",
                                    entry.Amount, "", "", "", entry.CreditCard.Track2,
                                    entry.CreditCard.FullTracks, uniqueId, "", null);
                        request.ProcessorSettings["SettleMerchantCode"] = processorInfo.SettleMerchantCode;

                        AuthorizationResponseFields response = processor.AuthorizePayment(request, AuthorizeMode.Normal);
                        Assert.AreEqual(entry.ResultCode, response.resultCode);
                    }
                    //);

                    //taskList.Add(task);
                    break;
                }
            }
            Task.WaitAll(taskList.ToArray());

            Console.WriteLine("Finished");
        }

        [TestMethod]
        public void AuthorizationPlatformTest()
        {
            ClearingPlatform processorInfo = TestData.Processors["FIS PayDirect Test"];

            Dictionary<string, string> configuration = new Dictionary<string, string>();

            AuthorizationClientPlatformsSection acpSection = (AuthorizationClientPlatformsSection)ConfigurationManager.GetSection("authorizationClientPlatforms");

            AuthorizationProcessorsCollection apCollection = acpSection.AuthorizationProcessors;

            ProcessorElement fisPayDirect = apCollection["fis-paydirect"];

            configuration["endpoint"] = fisPayDirect.Endpoint;

            string server = Dns.GetHostName();
            string processor = fisPayDirect.Name;

            IAuthorizationPlatform platform = new AuthorizationPlatform(server, processor, configuration);

            List<Task> taskList = new List<Task>();

            foreach (AuthorizationRequestEntry entry in TestData.AuthRequests["FIS Certification"])
            {
                //var task = Task.Factory.StartNew(() =>
                {
                    string uniqueId = TestData.GenerateUniqueId();

                    AuthorizationRequest request = new AuthorizationRequest(entry.MeterId,
                                DateTime.Now, processorInfo.MerchantId, processorInfo.MerchantPassword, "", "",
                                entry.Amount, "", "", "", entry.CreditCard.Track2,
                                entry.CreditCard.FullTracks, uniqueId, "", null);
                    request.ProcessorSettings["SettleMerchantCode"] = processorInfo.SettleMerchantCode;

                    AuthorizationResponseFields response = platform.Authorize(request, AuthorizeMode.Normal);
                    Assert.AreEqual(entry.ResultCode, response.resultCode);
                }
                //);

                //taskList.Add(task);

                //break;
            }

            Task.WaitAll(taskList.ToArray());
        }
    }


    /*
    [TestClass]
    public class TestingCreditCall
    {
        [TestMethod]
        public void CreditCallTestServer()
        {
            IAuthorizationPlatform creditCall = new CreditCall(CreditCall.AuthorizationMode.Test, "CCTMUnitTests", "0");
            AuthorizationRequest request = new AuthorizationRequest("0000042",
                        DateTime.Now, "test_retail:public", "publ1ct3st", "", "",
                        4.90m, "", "", "", ";5454545454545454=15121015432112345678?",
                        "%B5454545454545454^TEST CARD/MC^15121015432112345678?;5454545454545454=15121015432112345678?", "");

            AuthorizationResponseFields response = creditCall.Authorize(request);
            Assert.AreEqual(AuthorizationResultCode.Approved, response.resultCode);

            request = new AuthorizationRequest("0000042",
                        DateTime.Now, "test_retail:public", "publ1ct3st", "", "",
                        5.00m, "", "", "", ";5454545454545454=15121015432112345678?",
                        "%B5454545454545454^TEST CARD/MC^15121015432112345678?;5454545454545454=15121015432112345678?", "");

            response = creditCall.Authorize(request);
            Assert.AreEqual(AuthorizationResultCode.Declined, response.resultCode);
        }
    }

    [TestClass]
    public class TestingIcVerify
    {
        [TestMethod]
        public void IcVerifyTestServer()
        {
            IAuthorizationPlatform icVerify = new IcvAuthorizer("localhost", "54322", (log) => { Debug.WriteLine(log); });
            AuthorizationRequest request = new AuthorizationRequest("0000042",
                        DateTime.Now, "test_retail:public", "publ1ct3st", "", "",
                        4.90m, "", "", "", ";5454545454545454=15121015432112345678?",
                        "%B5454545454545454^TEST CARD/MC^15121015432112345678?;5454545454545454=15121015432112345678?", "");

            AuthorizationResponseFields response = icVerify.Authorize(request);
            Assert.AreEqual(AuthorizationResultCode.Approved, response.resultCode);
        }
    }
    */
}
