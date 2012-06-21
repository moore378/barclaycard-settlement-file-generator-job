using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Common;

namespace CryptographicPlatforms
{
    public unsafe class IPSTrackCipher
    {
        [DllImport("Track.dll")]
        private static extern void EncryptData(byte* bufferOut,
          byte* bufferIn, double startDateTime, int transactionIndex,
          int intMeterSN, int EncVer, int KeyVer);
        private static Mutex trackDllLock = new Mutex();

        /// <summary>
        /// Call this to decrypt using the IPS cryptographic DLL.
        /// </summary>
        /// <param name="encryptedData">Binary data to decrypt (must be 128 bytes long)</param>
        /// <param name="startDateTime">Transaction start datetime as recorded from the parking meter</param>
        /// <param name="transactionIndex">Index of the transaction as recorded from the parking meter</param>
        /// <param name="meterSerialNumber">The parking meter's meter serial number</param>
        /// <param name="encryptionVersion">Version of the IPS encryption</param>
        /// <param name="keyVersion">Key version to use for the decryption</param>
        /// <returns>Unencrypted string</returns>
        unsafe public static UnencryptedStripe Decrypt(EncryptedStripe encryptedStripe, DateTime refDateTime,
            int transactionIndex, int meterSerialNumber, int keyVersion)
        {
            byte[] encryptedData = encryptedStripe.Data;

            if (encryptedData.Length > 128)
                throw new InvalidDataException("Encrypted data length is invalid");

            System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();

            // Make sure that it is 128 bytes long
            Array.Resize(ref encryptedData, 128);

            // Convert input buffer to ASCII-Hex
            StringBuilder stringBuilder = new StringBuilder(encryptedData.Length * 2);
            for (int i = 0; i < encryptedData.Length; i++)
                stringBuilder.AppendFormat("{0:x2}", encryptedData[i]); 
            byte[] asciiHexData = encoder.GetBytes(stringBuilder.ToString().ToUpper());
            
            byte[] decryptedBuffer = new byte[128];

            trackDllLock.WaitOne();
            try
            {
                fixed (byte* bufin = &asciiHexData[0], bufout = &decryptedBuffer[0])
                {
                    EncryptData(bufout, bufin, refDateTime.ToOADate(), transactionIndex,
                        meterSerialNumber, 1, keyVersion);
                }
            }
            finally
            {
                trackDllLock.ReleaseMutex();
            }
            
            // The resulting data is not hex encoded
            return new UnencryptedStripe(encoder.GetString(decryptedBuffer));
        }

        /// <summary>
        /// Encrypt a string using IPS encryption
        /// </summary>
        /// <param name="unencryptedString">String of plain-text data</param>
        /// <param name="startDateTime"></param>
        /// <param name="transactionIndex"></param>
        /// <param name="meterSerialNumber"></param>
        /// <param name="keyVersion"></param>
        /// <returns>Binary byte array of encrypted data</returns>
        public static byte[] Encrypt(string unencryptedString, DateTime startDateTime,
            int transactionIndex, int meterSerialNumber, int keyVersion)
        {
            if (unencryptedString.Length > 128)
                throw new InvalidDataException("unencryptedString data length is invalid");

            System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();

            // Make sure that it is 128 bytes long
            while (unencryptedString.Length < 128)
                unencryptedString += '\0';
            
            // Convert input buffer to ASCII-Hex
            StringBuilder stringBuilder = new StringBuilder(unencryptedString.Length * 2);
            for (int i = 0; i < unencryptedString.Length; i++)
                stringBuilder.AppendFormat("{0:x2}", (byte)(unencryptedString[i]));
            byte[] asciiHexData = encoder.GetBytes(stringBuilder.ToString().ToUpper());

            byte[] decryptedBuffer = new byte[128];

            fixed (byte* bufin = &asciiHexData[0], bufout = &decryptedBuffer[0])
            {
                EncryptData(bufout, bufin, startDateTime.ToOADate(), transactionIndex,
                    meterSerialNumber, 1, keyVersion);
            }

            // The resulting data is not hex encoded
            return decryptedBuffer;
        }
    }

    [Serializable]
    public class InvalidDataException : Exception
    {
        public InvalidDataException() : base() { }
        public InvalidDataException(string message) : base(message) { }
        public InvalidDataException(string message, Exception innerException) : base(message, innerException) { }
    }
}
