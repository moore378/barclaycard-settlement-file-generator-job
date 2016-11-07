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

using MjhGeneral;
using TransactionManagementCommon;

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
        /// FIS PayDirect - Settlement Merchant Code for 
        ///     FIS requires a different merchant code for settlement.
        /// Barclaycard SmartPay - Merchant Account
        ///     This is where the charges are posted to.
        /// </summary>
        public string MerchantNumber { get; set; }

        /// <summary>
        /// Barclaycard SmartPay - Currency Code
        ///    Defining that the amount is in GBP or EUR.
        /// </summary>
        public string CashierNumber { get;set; }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <param name="expiry">YYMM</param>
        /// <param name="name"></param>
        /// <param name="discretionary"></param>
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
        private string _meterId = "0201501"; //0201349";// "0201501"; // "0000042";
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
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("2223000048400011", "2512", "US CARD/MC", "1011111199911111"),
                Amount = 4.90m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("2222400041240011", "2512", "BRAZIL CARD/MC", "1011111199911111"),
                Amount = 4.90m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("2222420040560011", "2512", "BELGIUM CARD/MC", "1011111199911111"),
                Amount = 4.90m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("2223000148400010", "2512", "US DEBIT/MC", "1011111199911111"),
                Amount = 4.90m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("2222400061240016", "2512", "CANADA DEBIT/MC", "1011111199911111"),
                Amount = 4.90m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("2222630061560019", "2512", "CHINA DEBIT/MC", "1011111199911111"),
                Amount = 4.90m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            _authRequests["Monetra"] = data;


            // Set up the data for FIS
            data = new List<AuthorizationRequestEntry>();
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5263227808124952", null, "TEST CARD/MC", "1011111199911111"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
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



            // Can only be this future date.
            string barclaycardExpiry = "1808";

            // Set up the data for Barclaycard
            data = new List<AuthorizationRequestEntry>();
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5555444433331111", barclaycardExpiry, "SMARTPAY CARD/MC", "1011111199911111"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("370000000000002", barclaycardExpiry, "SMARTPAY CARD/AE", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Declined
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("4646464646464644", barclaycardExpiry, "SMARTPAY CARD/VS", "1015432112345678"),
                Amount = 1.00m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5500000000000004", barclaycardExpiry, "SMARTPAY DEBIT/MC", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            // Meter not in BIN range?
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("36006666333344", barclaycardExpiry, "SMARTPAY CARD/DN", "1015432112345678"),
                Amount = 1.00m,
                ResultCode = AuthorizationResultCode.Declined
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("4400000000000008", barclaycardExpiry, "SMARTPAY DEBIT/VS", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Approved
            }
            );
            // Meter not in BIN range?
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("6731012345678906", barclaycardExpiry, "SMARTPAY CARD/ME", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Declined
            }
            );
            // Meter not in BIN range?
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("6759649826438453", barclaycardExpiry, "SMARTPAY CARD/MEUK", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Declined
            }
            );
            // Meter not in BIN range?
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("6222023602899998371", barclaycardExpiry, "SMARTPAY DEBIT/CUP", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Declined
            }
            );
            data.Add(new AuthorizationRequestEntry()
            {
                CreditCard = new CreditCard("5309900599078555", barclaycardExpiry, "TEST CARD/CUP", "1015432112345678"),
                Amount = 0.50m,
                ResultCode = AuthorizationResultCode.Declined
            }
            );
            _authRequests["Barclaycard SmartPay"] = data;
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
                    MerchantNumber = "50BNA-PUBWK-PARKG-00"
            });
            _processors.Add("Barclaycard SmartPay IntelligentParkingSolutions UK", new ClearingPlatform()
            {
                Name ="barclaycard-smartpay",
                Server = Dns.GetHostName(),
                Port = 8665,
                MerchantId ="ws@Company.IntelligentParkingSolutions",
                MerchantPassword = @"KRy&\Kj9Y5Arn2j#8>!d(tDbJ",
                MerchantNumber = "IPSUKeCom",
                CashierNumber = "GBP"
            });
            _processors.Add("Barclaycard SmartPay IntelligentParkingSolutions Italy", new ClearingPlatform()
            {
                Name = "barclaycard-smartpay",
                Server = Dns.GetHostName(),
                Port = 8665,
                MerchantId = "ws@Company.IntelligentParkingSolutions",
                MerchantPassword = @"KRy&\Kj9Y5Arn2j#8>!d(tDbJ",
                MerchantNumber = "IPSITeCom",
                CashierNumber = "EUR"
            });
            _processors.Add("Barclaycard SmartPay IPSEuropeSRL Italy", new ClearingPlatform()
            {
                Name = "barclaycard-smartpay",
                Server = Dns.GetHostName(),
                Port = 8665,
                MerchantId = "ws_623363@Company.IPSEuropeSRL",
                MerchantPassword = @"6uf737q2gIqJS5R2L^Nd)(qGQ",
                MerchantNumber = "IPSitaly",
                CashierNumber = "EUR"
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

    public class CreditCardTypeResult
    {
        public CreditCardType CreditCardType;
        public string AccountNumber;
    }

    [TestClass]
    public class TestingBarclaycardSmartPay
    {
        [TestMethod]
        public void ValidateDetermineCardType()
        {
            CreditCardTypeResult[] tests = new CreditCardTypeResult[]
            {
                new CreditCardTypeResult() { CreditCardType = CreditCardType.Visa, AccountNumber = "4444333322221111" },
                new CreditCardTypeResult() { CreditCardType = CreditCardType.MasterCard, AccountNumber = "5555444433331111" },
                new CreditCardTypeResult() { CreditCardType = CreditCardType.AmericanExpress, AccountNumber = "370000000000002" },
                new CreditCardTypeResult() { CreditCardType = CreditCardType.Discover, AccountNumber = "6011000990139424" },
            };

            foreach (var entry in tests)
            {
                CreditCardType creditCardType = CreditCardPan.DetermineCreditCardType(entry.AccountNumber);

                Assert.AreEqual(creditCardType, entry.CreditCardType);

                string scheme = creditCardType.GetDescription();
            }
        }

        [TestMethod]
        public void AuthorizationProcessorTest()
        {
            // asdf
            string processorName = "Barclaycard SmartPay IntelligentParkingSolutions UK";

            //processorName = "Barclaycard SmartPay IntelligentParkingSolutions Italy";
            processorName = "Barclaycard SmartPay IPSEuropeSRL Italy";

            ClearingPlatform processorInfo = TestData.Processors[processorName];

            IProcessorPlugin plugin = new AuthorizationClientPlatforms.Plugins.BarclaycardSmartPayPlugin();

            AuthorizationProcessor processor = new AuthorizationProcessor(plugin);

            Dictionary<string, string> configuration = new Dictionary<string, string>();

            configuration["endpoint"] = "https://pal-test.barclaycardsmartpay.com/pal/servlet/soap/Payment";

            processor.Initialize(configuration);

            List<Task> taskList = new List<Task>();

            //for (int i = 0; i < 10; i++)
            {
                foreach (AuthorizationRequestEntry entry in TestData.AuthRequests["Barclaycard SmartPay"])
                {
                    string uniqueId = TestData.GenerateUniqueId();

                    //var task = Task.Factory.StartNew(() =>
                    if (entry.ResultCode == AuthorizationResultCode.Approved)
                    {
                        entry.Amount = 1.00m;

                        AuthorizationRequest request = new AuthorizationRequest(entry.MeterId,
                                    DateTime.Now, processorInfo.MerchantId, processorInfo.MerchantPassword,
                                    entry.CreditCard.AccountNumber,
                                    String.Format("{0:00}{1:00}", entry.CreditCard.ExpiryMonth, entry.CreditCard.ExpiryYear), 
                                    entry.Amount, "", "", "", entry.CreditCard.Track2,
                                    entry.CreditCard.FullTracks, uniqueId, "", null);
                        request.ProcessorSettings["MerchantAccount"] = processorInfo.MerchantNumber;
                        request.ProcessorSettings["CurrencyCode"] = processorInfo.CashierNumber;

                        AuthorizationResponseFields response = processor.AuthorizePayment(request, AuthorizeMode.Normal);
                        Assert.AreEqual(response.resultCode, entry.ResultCode);
                    }
                    //);

                    //taskList.Add(task);
                    //break;
                }
            }
            Task.WaitAll(taskList.ToArray());
        }

        [TestMethod]
        public void AuthorizationPlatformTest()
        {
            ClearingPlatform processorInfo = TestData.Processors["Barclaycard SmartPay IntelligentParkingSolutions UK"];

            Dictionary<string, string> configuration = new Dictionary<string, string>();

            AuthorizationClientPlatformsSection acpSection = (AuthorizationClientPlatformsSection)ConfigurationManager.GetSection("authorizationClientPlatforms");

            AuthorizationProcessorsCollection apCollection = acpSection.AuthorizationProcessors;

            ProcessorElement barclaycardSmartPay = apCollection["barclaycard-smartpay"];

            configuration["endpoint"] = barclaycardSmartPay.Endpoint;

            string server = Dns.GetHostName();
            string processor = barclaycardSmartPay.Name;

            IAuthorizationPlatform platform = new AuthorizationPlatform(server, processor, configuration);

            List<Task> taskList = new List<Task>();

            foreach (AuthorizationRequestEntry entry in TestData.AuthRequests["Barclaycard SmartPay"])
            {
                //var task = Task.Factory.StartNew(() =>
                {
                    string uniqueId = TestData.GenerateUniqueId();

                    AuthorizationRequest request = new AuthorizationRequest(entry.MeterId,
                                DateTime.Now, processorInfo.MerchantId, processorInfo.MerchantPassword,
                                entry.CreditCard.AccountNumber,
                                String.Format("{0:00}{1:00}", entry.CreditCard.ExpiryMonth, entry.CreditCard.ExpiryYear),
                                entry.Amount, "", "", "", entry.CreditCard.Track2,
                                entry.CreditCard.FullTracks, uniqueId, "", null);
                    request.ProcessorSettings["MerchantAccount"] = processorInfo.MerchantNumber;
                    request.ProcessorSettings["CurrencyCode"] = processorInfo.CashierNumber;

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
                        request.ProcessorSettings["SettleMerchantCode"] = processorInfo.MerchantNumber;

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
                    request.ProcessorSettings["SettleMerchantCode"] = processorInfo.MerchantNumber;

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
