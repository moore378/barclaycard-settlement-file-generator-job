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
            var ip = new IsraelPremium("0963185013", "01");
            var resp = ip.Authorize(new AuthorizationRequest("00412521", DateTime.Now, "user1", "11111-22222-33333-44444-55555", "458045804580", "1111", 1, "", "", "", "", "", "", "", 0), AuthorizeMode.Normal);
            Console.WriteLine(resp);
        }

    }
}
