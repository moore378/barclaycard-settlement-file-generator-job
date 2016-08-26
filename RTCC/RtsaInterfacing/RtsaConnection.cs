using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using TransactionManagementCommon;

namespace Rtcc.RtsaInterfacing
{
    /// <summary>
    /// The TCP Messenger object wrapps a TCP server connection, reading stream "packets" 
    /// from the specified TCP socket. Stream packets start with a 4 byte (double word),
    /// in little-endian, containing the length of the stream string to follow, followed
    /// by the actual stream.
    /// </summary>
    public class RtsaConnection : LoggingObject
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        private TcpClient client;
        private NetworkStream clientStream;
        private bool doDisconnect = false;

        /// <summary>
        /// Create a TCP Messenger to wrap a TcpClient
        /// </summary>
        /// <param name="client">TCP client to wrap</param>
        public RtsaConnection(TcpClient client)
        {
            this.client = client;
        }

        // Null constructor for fake
        public RtsaConnection()
        {

        }

        public virtual void Disconnect() { doDisconnect = true; }

        public virtual void SendMessage(RawDataMessage msg)
        {
            // Copy the data from the stream into a byte buffer to send
            msg.Data.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[msg.Data.Length];
            msg.Data.Read(data, 0, data.Length);

            // Write the bytes to the TCP
            clientStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Reads in a binary block of known size from a given stream. This call will block 
        /// until the full size is read from the stream, or the connection closes.
        /// </summary>
        /// <param name="stream">The stream from which the block is read</param>
        /// <param name="size">The size of the block to read</param>
        /// <param name="block">Output byte array for read data</param>
        /// <returns>
        /// Returns true if the data is successfully read, false if the connection terminated
        /// during the reading.
        /// </returns>
        private bool readBlock(Stream stream, int size, out byte[] block)
        {
            int bytesRead;
            if (size == 0)
            {
                block = new byte[0];
                return true;
            }

            bytesRead = 0;
            byte[] buf = new byte[size];
            int targetIndex = 0;
            byte[] readBytes = new byte[size];

            int sizeRemaining = size;

            while (sizeRemaining > 0)
            {
                // Read in as much as we can of the remaining size
                int numBytesToRead = sizeRemaining;

                LogDetail("Waiting to receive " + numBytesToRead + " bytes");

                bytesRead = stream.Read(readBytes, 0, numBytesToRead);
                // If there are no bytes read, it means there is a stream error (or disconnection)
                if (bytesRead == 0)
                {
                    block = new byte[0];
                    return false;
                }
                // Copy the received data into the buffer
                Array.Copy(readBytes, 0, buf, targetIndex, bytesRead);

                sizeRemaining -= bytesRead;

                LogDetail("Received " + bytesRead + " bytes. " + (sizeRemaining > 0 ? "Waiting for " + sizeRemaining + " remaining bytes" : ""));
                //LogDetail("Rx: " + BitConverter.ToString(readBytes, targetIndex, bytesRead)); // For now do not dump out the raw message.

                targetIndex += bytesRead;
            }



            block = buf;
            return true;
        }

        /// <summary>
        /// Reads an XML message from a stream. The call will block until a full message is read
        /// from the stream, or the stream closes. (See TCPMessenger for format details).
        /// </summary>
        /// <param name="stream">The stream from which to read the XML message</param>
        /// <returns>Returns an XML document if successful, returns null if termination is closed</returns>
        private byte[] readMessage(Stream stream)
        {
            // Read the size of the message (4 bytes)
            byte[] sizeBlock;
            if (!readBlock(stream, 4, out sizeBlock))
                return null;
            int count = BitConverter.ToInt32(sizeBlock, 0);

            // Read in the message data
            byte[] messageBlock;
            if (!readBlock(stream, count, out messageBlock))
                return null;

            return messageBlock;
        }

        /// <summary>
        /// This is the main thread method of the client, which reads in XML messages and fires 
        /// events. It will terminate when the connection closes, or when there is a socket error.
        /// </summary>
        /// <param name="client">A TcpClient representing the socket to be watched.</param>
        private void ThreadProc(TcpClient client)
        {
            TcpClient tcpClient = (TcpClient)client;
            clientStream = tcpClient.GetStream();
            try
            {
                byte[] message = new byte[4096];

                if (clientStream == null)
                {
                    LogError("Error: Unable to get TCP client stream.");
                    return;
                }

                while (!doDisconnect)
                {
                    // Read in a message
                    byte[] msg = readMessage(clientStream);
                    if (msg == null)
                    {
                        // The client has disconnected from the server
                        LogDetail("Client disconnected");
                        break;
                    }

                    // Message has successfully been received
                    LogDetail("Message received: " + msg.ToString());
                    DoMessageReceived(msg);
                }
            }
            catch (Exception e)
            { 
                LogError("Error reading network stream from RTSA", e); 
            }
            finally
            { 
                tcpClient.Close();
                LogDetail("RTSA-listener thread terminating");
            }
        }

        protected void DoMessageReceived(byte[] msg)
        {
            var temp = MessageReceivedEvent;
            if (temp != null)
                temp(this, new MessageReceivedEventArgs(msg));
        }

        internal void Start()
        {
            Thread clientThread = new Thread(new ThreadStart(
                () =>
                {
                    ThreadProc(client);
                }
                ));
            clientThread.Start();

            LogDetail("TCP messenger created!");
        }
    }
}
