using System;
using System.Runtime.InteropServices;
using RsaUtils;

namespace TransactionManagement
{
    public unsafe class IPSTrackDecryptor
    {
        [DllImport("Track.dll")]
        private static extern void EncryptData(byte* bufferOut,
          byte* bufferIn, double startDateTime, int transactionIndex,
          int intMeterSN, int EncVer, int KeyVer);

        /// <summary>
        /// Call this to decrypt using the IPS cryptographic DLL.
        /// </summary>
        /// <param name="encryptedData">Data to decrypt (must be 256 bytes long) </param>
        /// <param name="startDateTime">Transaction start datetime as recorded from the parking meter</param>
        /// <param name="transactionIndex">Index of the transaction as recorded from the parking meter</param>
        /// <param name="meterSerialNumber">The parking meter's meter serial number</param>
        /// <param name="encryptionVersion">Version of the IPS encryption</param>
        /// <param name="keyVersion">Key version to use for the decryption</param>
        /// <returns>Unencrypted string</returns>
        public static string Decrypt(byte[] encryptedData, DateTime startDateTime,
            int transactionIndex, int meterSerialNumber, int encryptionVersion, int keyVersion)
        {
            if (encryptedData.Length != 256)
                throw new InvalidDataException("Encrypted data length is invalid");
            
            byte[] decryptedBuffer = new byte[128];
            
            fixed (byte* bufin = &encryptedData[0], bufout = &decryptedBuffer[0])
            {
                EncryptData(bufout, bufin, startDateTime.ToOADate(), transactionIndex, 
                    meterSerialNumber, encryptionVersion, keyVersion);
            }

            System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
            return encoder.GetString(decryptedBuffer);
        }
    }

    public class RsaDecryptor
    {
        public static string Decrypt(byte[] encryptedData, ushort keyVersion)
        {
            // Convert byte array to string 
            System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
            string encryptedString = encoder.GetString(encryptedData);

            // Create RSA object
            RsaUtility RsaUtl = new RsaUtility();
            string decryptedAsciiHexString = RsaUtl.RsaDecrypt(encryptedString, (ushort)keyVersion);

            // Convert hex sequence to string
            string Result = "";
            int decryptedAsciiHexStringLen = decryptedAsciiHexString.Length / 2;
            for (int i = 0; i < decryptedAsciiHexStringLen; i+=2)
            {
                Result += Convert.ToChar(Convert.ToByte(decryptedAsciiHexString.Substring(i, 2), 16));
            }
            return Result;
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
