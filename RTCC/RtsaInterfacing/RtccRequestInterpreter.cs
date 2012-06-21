using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using System.Xml.Serialization;
using Common;
using System.IO;

namespace Rtcc.RtsaInterfacing
{
    /// <summary>
    /// Object to interpret XML messages from and to the client RT session.
    /// </summary>
    public class RtccRequestInterpreter : LoggingObject
    {
        /// <summary>
        /// This is called when a message is received by the messenger
        /// </summary>
        /// <param name="message">Raw data representing the message block</param>
        /// <param name="failStatus">The fail status of the ParseException if there is a problem parsing.</param>
        /// <exception cref="ParseException"></exception>
        public virtual ClientAuthRequest ParseMessage(RawDataMessage message, string failStatus)
        {
            try
            {
                // Read the authorization request from the stream
                XmlSerializer ser = new XmlSerializer(typeof(ClientAuthRequestXML));
                ClientAuthRequestXML requestFromXML = (ClientAuthRequestXML)ser.Deserialize(message.Data);

                byte[] decodedEncryptedTrack;

                EncryptionMethod encryptionMethod = IntToEncryptionMethod(requestFromXML.encryptionMethod);

                decodedEncryptedTrack = decodeTrack(Convert.FromBase64String(requestFromXML.ccTrackBase64), StripeEncodingMethod.Ascii);

                if (requestFromXML.StructureVersion != "03")
                    throw new ArgumentException("XML structure from client is of an invalid version");

                ClientAuthRequest request = new ClientAuthRequest(
                    requestFromXML.meterID,
                    DateTime.SpecifyKind(requestFromXML.LocalStartDateTime.ToUniversalTime(), DateTimeKind.Unspecified), // The time is local, no time-zone information
                    requestFromXML.requestType,
                    requestFromXML.ccTransactionIndex,
                    IntToEncryptionMethod(requestFromXML.encryptionMethod),
                    requestFromXML.keyVer,
                    requestFromXML.requestType,
                    requestFromXML.uniqueRecNo,
                    decimal.Parse(requestFromXML.amount),
                    requestFromXML.transactionDesc,
                    requestFromXML.invoice,
                    new EncryptedStripe(decodedEncryptedTrack),
                    requestFromXML.purchasedTime,
                    requestFromXML.UniqueNumber2,
                    requestFromXML.Flags);

                return request;
            }
            catch (Exception exception)
            {
                //log("Error parsing request message - " + exception.ToString());
                throw new ParseException("Error parsing request message", failStatus, exception);
            }
        }

        public virtual RawDataMessage SerializeResponse(ClientAuthResponse reply)
        {
            // Serialize the reply
            XmlSerializer ser = new XmlSerializer(typeof(ClientAuthResponseXML));
            MemoryStream memStream = new MemoryStream();
            ClientAuthResponseXML responseXMLObject = new ClientAuthResponseXML();
            responseXMLObject.Accepted = reply.Accepted;
            responseXMLObject.AmountDollars = reply.AmountDollars;
            responseXMLObject.ReceiptReference = reply.ReceiptReference;
            responseXMLObject.ResponseCode = reply.ResponseCode;

            ser.Serialize(memStream, responseXMLObject);

            LogDetail("Sending reply sent to client");
            return new RawDataMessage(memStream);
        }

        /// <summary>
        /// Converts an integer to an EncryptionMethod. This method is provide because it is more explicit than a cast.
        /// </summary>
        /// <returns></returns>
        private EncryptionMethod IntToEncryptionMethod(int encryptionMethod)
        {
            switch (encryptionMethod)
            {
                case 0: return EncryptionMethod.Unencrypted;
                case 1: return EncryptionMethod.IpsEncryption;
                case 2: return EncryptionMethod.RsaEncryption;
                default: throw new ArgumentOutOfRangeException("Invalid encryption method \"" + encryptionMethod.ToString() + "\"");
            }
        }

        private enum StripeEncodingMethod
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
        private byte[] decodeTrack(byte[] encodedEncryptedTrack, StripeEncodingMethod encodingMethod)
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
