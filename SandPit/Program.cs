using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GiveMeAName;
using System.IO;
using System.Data.SqlClient;
using AutoDatabase;
using System.Threading.Tasks;
using System.Data;
using AuthorizationClientPlatforms;

namespace SandPit
{
    /// <summary>
    /// Used to create small temporary programs to test certain features during R&D
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var stripeStr = "1979BB6AE3E2B9A6591C9CCD63D3B4C7B59FCEDEEEF58ACE0D86C639EFF039B6C9D1E3C16C9AD0DB54D6A8F1EEA421F2BBFB9A79378100CE71C785583E161998984BAA089630DDB1BB2C17C09ABEE2B6356C82CF1B307A2F67E462580443B9C769DDE2E21EBE6229F6F1C9F5B25EED47A596726CCBEFC83971B04142985089A9";
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            var encStripe = Common.DatabaseFormats.DecodeDatabaseStripe(stripeStr);

            //var stripe = new Common.EncryptedStripe(encStripe);
            //CryptographicPlatforms.IPSTrackCipher.Decrypt(encStripe, );



            //var ip = new IsraelPremium("0963185013", "01");
            //var resp = ip.Authorize(new AuthorizationRequest("00412521", DateTime.Now, "user1", "11111-22222-33333-44444-55555", "458045804580", "1111", 1, "", "", "", "", "", "", "", 0), AuthorizeMode.Normal);
            //Console.WriteLine(resp);
        }

    }
}
