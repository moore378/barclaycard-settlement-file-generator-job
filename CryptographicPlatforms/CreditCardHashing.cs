using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;

namespace CryptographicPlatforms
{
    public class CreditCardHashing
    {
        public static string HashPAN(string PAN)
        {
            return CCCrypto.CCCrypt.HashPAN(PAN);
        }

        public static Int64 HashPANToInt64(string PAN)
        {
            Int64 hashCode = 0;

            SHA256 hasher = new SHA256CryptoServiceProvider();

            string salted;

            if (10 <= PAN.Length)
            {
                int offset = PAN.Length / 2;

                salted = String.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}",
                    PAN[1],                                     // second digit.
                    PAN[PAN.Length - 3],                        // third to the last digit.
                    PAN.Substring(offset, PAN.Length - offset), // second part of account number.
                    PAN[PAN.Length - 1],                        // last digit.
                    PAN[2],                                     // third digit.
                    PAN[PAN.Length - 1],                        // last digit of account.
                    PAN[4],                                     // fifth digit of account.
                    PAN.Substring(0, offset),                   // first part of account number
                    PAN[PAN.Length - 2],                        // second to the last digit.
                    PAN[3],                                     // fourth digit.
                    PAN[0],                                     // first digit.
                    PAN[PAN.Length - 4],                        // fourth to the last digit.
                    PAN[1],                                     // second digit of account.
                    PAN[5]);                                    // sixth digit of the account.
            }
            else
            {
                int sum = 0;

                foreach (char c in PAN)
                {
                    sum += c - '0';
                }

                // Make it a bit more complicated by multiplying with the first digit.
                int firstDigit = PAN[0] - '0';

                if (0 != firstDigit)
                {
                    sum *= firstDigit;
                }

                // unknown lengths so add the account and sum again
                salted = String.Format("{0}{1}{2}{3}",
                    PAN[PAN.Length - 1],
                    PAN,
                    PAN[0],
                    sum);
            }

            // Using algorithm from http://www.codeproject.com/Articles/34309/Convert-String-to-bit-Integer.
            byte[] hashText = hasher.ComputeHash(Encoding.ASCII.GetBytes(salted));

            Int64 hashCodeStart = BitConverter.ToInt64(hashText, 0);
            Int64 hashCodeMedium = BitConverter.ToInt64(hashText, 8);
            Int64 hashCodeEnd = BitConverter.ToInt64(hashText, 24);

            hashCode = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;

            return hashCode;
        }
    }
}
