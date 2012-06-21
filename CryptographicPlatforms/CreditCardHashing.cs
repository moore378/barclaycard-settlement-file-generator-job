using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptographicPlatforms
{
    public class CreditCardHashing
    {
        public static string HashPAN(string PAN)
        {
            return CCCrypto.CCCrypt.HashPAN(PAN);
        }
    }
}
