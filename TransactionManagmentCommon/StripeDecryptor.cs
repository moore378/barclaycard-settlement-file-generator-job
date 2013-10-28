using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    /// <summary>
    /// Decrypts a stripe using a given encryption method
    /// </summary>
    public class StripeDecryptor : Object
    {
        /// <summary>
        /// Decrypt given track data
        /// </summary>
        /// <param name="encryptedTrack">Encrypted track data</param>
        /// <returns>Returns an UnencryptedTracks if successful,
        /// and UnencryptedTracks.nullTracks() if unsuccessful.</returns>
        /// <exception cref="StripeErrorException"></exception>
        public UnencryptedStripe decryptStripe(EncryptedStripe encryptedTrack, EncryptionMethod encryptionVersion, int keyVersion, TransactionInfo info, string errorStatus)
        {
            switch (encryptionVersion)
            {
                case EncryptionMethod.Unencrypted:
                    try
                    {
                        // Convert directly from Ascii
                        System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
                        return new UnencryptedStripe(encoder.GetString(encryptedTrack.Data));
                    }
                    catch (Exception exception)
                    {
                        throw new StripeErrorException("Error using plain-text stripe", errorStatus, exception);
                    }

                case EncryptionMethod.IpsEncryption:
                    try
                    {
                        return CryptographicPlatforms.IPSTrackCipher.Decrypt(encryptedTrack, info.RefDateTime ?? info.StartDateTime,
                            (int)info.TransactionIndex, Int32.Parse(info.MeterSerialNumber),
                            keyVersion);
                    }
                    catch (Exception exception)
                    {
                        throw new StripeErrorException("Error decrypting IPS-encrypted stripe: " + exception.Message, errorStatus, exception);
                    }

                case EncryptionMethod.RsaEncryption:
                    try
                    {
                        return CryptographicPlatforms.RsaCipher.Decrypt(encryptedTrack, keyVersion);
                    }
                    catch (Exception exception)
                    {
                        throw new StripeErrorException("Error decrypting RSA-encrypted stripe", errorStatus, exception);
                    }

                case EncryptionMethod.StandardFormat:
                    try
                    {
                        // Convert directly from Ascii
                        System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
                        return new UnencryptedStripe(encoder.GetString(encryptedTrack.Data));
                    }
                    catch (Exception exception)
                    {
                        throw new StripeErrorException("Error using plain-text stripe", errorStatus, exception);
                    }

                default:
                    throw new StripeErrorException("Unknown encryption method", errorStatus);
            }
        }
    }
}
