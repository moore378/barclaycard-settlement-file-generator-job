using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public class EncryptedStripe
    {
        public byte[] Data { get; private set; }

        public EncryptedStripe(byte[] data)
        {
            Data = data;
        }

        public static implicit operator byte[](EncryptedStripe stripe)
        {
            return stripe.Data;
        }

        public static implicit operator EncryptedStripe(byte[] stripe)
        {
            return new EncryptedStripe(stripe);
        }
    }

    public class UnencryptedStripe
    {
        public string Data { get; private set; }

        public UnencryptedStripe(string data)
        {
            Data = data;
        }

        public override string ToString()
        {
            return Data;
        }

        public static implicit operator string(UnencryptedStripe stripe)
        {
            return stripe.Data;
        }

        public static implicit operator UnencryptedStripe(string stripe)
        {
            return new UnencryptedStripe(stripe);
        }
    }

    public enum EncryptionMethod
    {
        /// <summary>
        /// No encryption, stripe is in the standard format already (including sentinels)
        /// </summary>
        StandardFormat = -1,
        /// <summary>
        /// Unencrypted track, without track two beginning and end sentinels, with track two starting at byte[88]
        /// </summary>
        Unencrypted = 0,
        /// <summary>
        /// IPS-encrypted track, without track two beginning and end sentinels, with track two starting at byte[88]
        /// </summary>
        IpsEncryption = 1,
        /// <summary>
        /// Rsa-encrypted track, without track two beginning and end sentinels, with track two starting at byte[78]
        /// </summary>
        RsaEncryption = 2
    }

    public class FormattedStripe
    {
        public string Data { get; private set; }

        public FormattedStripe(string data)
        {
            Data = data;
        }

        public override string ToString()
        {
            return Data;
        }

        public static implicit operator string(FormattedStripe stripe)
        {
            return stripe.Data;
        }

        public static implicit operator FormattedStripe(string stripe)
        {
            return new FormattedStripe(stripe);
        }
    }

    /// <summary>
    /// A special kind of exception that occurs during transaction validation, and has a "FailStatus" which should be used to set the status of the record
    /// </summary>
    public class StripeErrorException : Exception
    {
        public string FailStatus = "Suspended";
        public StripeErrorException(string message, string transactionStatus)
            : base(message)
        {
            this.FailStatus = transactionStatus;
        }
        public StripeErrorException(string message, string transactionStatus, Exception innerException) :
            base(message, innerException)
        {
            this.FailStatus = transactionStatus;
        }
        public StripeErrorException() : base() { }
        public StripeErrorException(string message) : base(message) { }
        public StripeErrorException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// A special kind of exception that occurs during transaction validation, and has a "FailStatus" which should be used to set the status of the record
    /// </summary>
    public class ValidationException : StripeErrorException
    {
        public ValidationException(string message, string transactionStatus) : base(message, transactionStatus) { }
        public ValidationException() : base() { }
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
        public ValidationException(string message, string transactionStatus, Exception innerException) : base(message, transactionStatus, innerException) { }
    }

    /// <summary>
    /// This means there was a problem with parsing some property of a transaction
    /// </summary>
    public class ParseException : StripeErrorException
    {
        public ParseException(string message, string transactionStatus) : base(message, transactionStatus) { }
        public ParseException() : base() { }
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception innerException) : base(message, innerException) { }
        public ParseException(string message, string transactionStatus, Exception innerException) : base(message, transactionStatus, innerException) { }
    }

    public static class DatabaseFormats
    {
        public static EncryptionMethod decodeDatabaseEncryptionMethod(decimal encryptionVer)
        {
            return (EncryptionMethod)encryptionVer;
        }

        public static EncryptedStripe DecodeDatabaseStripe(string databaseStripe)
        {
            if (databaseStripe.Length == 0)
                return new EncryptedStripe(new byte[0]);

            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            // The stripe is base16 encoded
            byte[] encryptedEncodedStripe = encoding.GetBytes(databaseStripe);
            byte[] encryptedDecodedStripe = decodeStripe(encryptedEncodedStripe, StripeEncodingMethod.Base16);

            return new EncryptedStripe(encryptedDecodedStripe);            
        }

        enum StripeEncodingMethod
        {
            Ascii, // 1 byte for 1 char
            Base16 // Hex-ascii (1 byte for 2 chars)
        };

        /// <summary>
        /// This method decodes (not decrypts) a track from a specific encoding into binary bytes
        /// </summary>
        /// <param name="encodedEncryptedTrack"></param>
        /// <param name="encodingMethod"></param>
        /// <returns>Decoded, encrypted track</returns>
        private static byte[] decodeStripe(byte[] encodedEncryptedTrack, StripeEncodingMethod encodingMethod)
        {
            switch (encodingMethod)
            {
                case StripeEncodingMethod.Ascii:
                    return encodedEncryptedTrack;

                case StripeEncodingMethod.Base16:
                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                    string encodedEncryptedTrackString = encoding.GetString(encodedEncryptedTrack);

                    // Convert hex sequence to string
                    int asciiHexStringLen = encodedEncryptedTrack.Length / 2;
                    byte[] result = new byte[asciiHexStringLen];
                    for (int i = 0; i < asciiHexStringLen; i++)
                    {
                        result[i] = Convert.ToByte(encodedEncryptedTrackString.Substring(i * 2, 2), 16);
                    }
                    return result;

                default:
                    throw new FormatException("Invalid encoding track scheme");
            }
        }
    }
}
