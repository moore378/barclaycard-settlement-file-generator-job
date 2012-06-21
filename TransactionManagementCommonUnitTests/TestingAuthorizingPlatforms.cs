using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AuthorizationClientPlatforms;
using System.Diagnostics;

namespace UnitTests
{
    [TestClass]
    public class TestingMonetra
    {
        [TestMethod]
        public void MonetraTestServer()
        {
            MonetraClient monetraClient = new MonetraDotNetNativeClient("testbox.monetra.com", 8444, (log) => { Debug.WriteLine(log); });
            IAuthorizationPlatform monetra = new Monetra(monetraClient, (log) => { Debug.WriteLine(log); });

            AuthorizationRequest request = new AuthorizationRequest("0000042",
                        DateTime.Now, "test_retail:public", "publ1ct3st", "", "",
                        4.90m, "", "", "", ";5454545454545454=15121015432112345678?",
                        "%B5454545454545454^TEST CARD/MC^15121015432112345678?;5454545454545454=15121015432112345678?", "", "", null);
            AuthorizationResponseFields response = monetra.Authorize(request, AuthorizeMode.Normal);
            Assert.AreEqual(AuthorizationResultCode.Approved, response.resultCode);

            request = new AuthorizationRequest("0000042",
                        DateTime.Now, "test_retail:public", "publ1ct3st", "",
                        "", 6.01m, "", "", "", ";5454545454545454=15121015432112345678?",
                        "%B5454545454545454^TEST CARD/MC^15121015432112345678?;5454545454545454=15121015432112345678?", "", "", null);
            response = monetra.Authorize(request, AuthorizeMode.Normal);
            Assert.AreEqual(AuthorizationResultCode.Declined, response.resultCode);
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
