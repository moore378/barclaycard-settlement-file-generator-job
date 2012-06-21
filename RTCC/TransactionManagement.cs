using System;
using System.Collections.Generic;
using System.Linq;


namespace TransactionManagement
{
    /// <summary>
    /// A TrackDecryptor can decrypt a track. See TrackDecryptor.decryptTrack()
    /// </summary>
    public class TrackDecryptor : Object
    {
        public enum EncryptionVersion
        {
            Unencrypted = 0,
            IpsEncryption = 1,
            RsaEncryption = 2
        }

        /// <summary>
        /// Decrypt given track data
        /// </summary>
        /// <param name="encVer">
        /// Encryption version. 
        /// 1 = IPS TrackEncryption
        /// 2 = RSA1024 Track Encryption
        /// Else = Unencrypted</param>
        /// <param name="encryptedTrack">Encrypted track data</param>
        /// <param name="info">Information about the transaction that may be used in the decryption</param>
        /// <returns>Returns an UnencryptedTracks if successful,
        /// and UnencryptedTracks.nullTracks() if unsuccessful.</returns>
        public string decryptTrack(byte[] encryptedTrack, EncryptionVersion encryptionVersion, int keyVersion, TransactionInfo info)
        {
            switch (encryptionVersion)
            {
                case EncryptionVersion.Unencrypted: 
                    // Convert directly
                    System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
                    return encoder.GetString(encryptedTrack);
                    break;

                case EncryptionVersion.IpsEncryption:
                    TransactionManagement.IPSTrackDecryptor.Decrypt(encryptedTrack, info.startDateTime,
                        info.transactionIndex, info.meterSerialNumber, encryptionVersion, keyVersion);
                    break;
                case EncryptionVersion.RsaEncryption: break;

                default: break;
            }
            if (encVer == 1) // IPS TrackEncryption
            {
                // 2b-i Convert trackData ASCII string to ASCI Hex and store values in input buffer
                ASCIIEncoding ascii = new ASCIIEncoding();
                bufferIn = ascii.GetBytes(encryptedTrack);     //        bufferIn               <= trackData
                //                                             "C     2     E     8"
                //                                             0x43, 0x32, 0x45, 0x38, ...  <= "C2E8..."

                // 2b-ii. Decrypt input buffer into output buffer.
                IPSTrackDecryptor.EncryptData(bufferOut, bufferIn, info.startDateTime.ToOADate(), info.transactionIndex,
                  Convert.ToInt32(info.meterSerialNo), encVer, keyVer);

                string TraxString = "";
                // Convert Output Buffer content to local string (TraxString)
                for (int OutBuf_idx = 0; OutBuf_idx < bufferOutLen; OutBuf_idx++)
                {
                    TraxString += (char)bufferOut[OutBuf_idx]; //     TraxString <=     bufferOut
                    //                                                "B419..."  <= 0x42,0x34,0x31,0x39, ...
                }
                // trim final '\0\'s from TraxString.
                int EqIdx = TraxString.IndexOf("=");
                if (EqIdx > 0)
                {
                    int lastNulls = TraxString.IndexOf("\0", EqIdx);
                    if (lastNulls > 0)
                        TraxString = TraxString.Substring(0, lastNulls);
                }
                return UnencryptedTracks.fromTrackData(TraxString, 88);

            }
            else if (encVer == 2) // RSA1024 Track Encryption
            {
                RsaUtility RsaUtl = new RsaUtility();
                string decryptedAsciiHexString = RsaUtl.RsaDecrypt(encryptedTrack, (ushort)info.keyVer);

                // convert to string
                string TraxString = "";
                int decryptedAsciiHexStringLen = (decryptedAsciiHexString.Length / 2) - 1;
                for (int idx = 0; idx < decryptedAsciiHexStringLen; idx++)
                {
                    TraxString += Convert.ToChar(Convert.ToInt32
                                        (decryptedAsciiHexString.Substring(idx * 2, 2), 16)).ToString();
                }
                return UnencryptedTracks.fromTrackData(TraxString, 78);
            }
            else return UnencryptedTracks.nullTracks();
        }
    }

    
}
