using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RsaUtils;
using Common;

namespace CryptographicPlatforms
{
    public class RsaCipher
    {
        private static string BytesToHexStr(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes)
                result = result + b.ToString("x2");
            return result;
        }

        private static byte[] HexStrToBytes(string s)
        {
            byte[] result = new byte[s.Length/2];
            for (int i = 0; i < s.Length / 2; i++)
            {
                string tmp = s.Substring(i * 2, 2);
                result[i] = byte.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
            }
            return result;
        }

        public static UnencryptedStripe Decrypt(EncryptedStripe encryptedStripe, int keyVersion)
        {
            byte[] encryptedData = encryptedStripe.Data;

            // Convert byte array to string 
            string encryptedString = BytesToHexStr(encryptedData).ToUpper();

            // Create RSA object
            RsaUtility RsaUtl = new RsaUtility();

            string decryptedAsciiHexString = RsaUtl.RsaDecrypt(encryptedString, (ushort)keyVersion);

            // The result is a hex-encoded, ascii-encoded string
            byte[] decoded = HexStrToBytes(decryptedAsciiHexString);

            ASCIIEncoding encoding = new ASCIIEncoding();
            
            return new UnencryptedStripe(encoding.GetString(decoded));
        }

        public static byte[] Encrypt(string unencryptedString, int keyVersion)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();

            // Convert string to byte array
            byte[] unencryptedData = HexStrToBytes(unencryptedString);

            // Create RSA object
            RsaUtility RsaUtl = new RsaUtility();
            string encryptedAsciiHexString = RsaUtl.RsaEncrypt(unencryptedData, (ushort)keyVersion);

            // Convert string to byte array
            byte[] encryptedData = encoder.GetBytes(encryptedAsciiHexString);

            return encryptedData;
        }

    }
}
