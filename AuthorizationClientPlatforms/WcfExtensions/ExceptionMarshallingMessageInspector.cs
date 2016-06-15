using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

using System.Reflection;

namespace AuthorizationClientPlatforms.WcfExtensions
{
    public class ExceptionMarshallingMessageInspector : IClientMessageInspector
    {
        void IClientMessageInspector.AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (reply.IsFault)
            {
                // Create a copy of the original reply to allow default processing of the message
                MessageBuffer buffer = reply.CreateBufferedCopy(Int32.MaxValue);
                Message copy = buffer.CreateMessage();  // Create a copy to work with
                reply = buffer.CreateMessage();         // Restore the original message

                object faultDetail = ReadFaultDetail(copy);
                Exception exception = faultDetail as Exception;
                if (exception != null)
                {
                    // NB: Error checking etc. excluded
                    // Get the _remoteStackTraceString of the Exception class
                    FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    // Set the InnerException._remoteStackTraceString to the current InnerException.StackTrace
                    remoteStackTraceString.SetValue(exception, exception.StackTrace + Environment.NewLine);

                    throw exception;
                }
            }
        }

        object IClientMessageInspector.BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return null;
        }

        private static object ReadFaultDetail(Message reply)
        {
            const string detailElementName = "detail";

            using (XmlDictionaryReader reader = reply.GetReaderAtBodyContents())
            {
                // Find <soap:Detail>
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName.ToLower() == detailElementName)
                    {
                        break;
                    }
                }

                // Did we find it?
                if (reader.NodeType != XmlNodeType.Element || reader.LocalName.ToLower() != detailElementName)
                {
                    return null;
                }

                // Move to the contents of <soap:Detail>
                if (!reader.Read())
                {
                    return null;
                }

                // Deserialize the fault
                NetDataContractSerializer serializer = new NetDataContractSerializer();
                try
                {
                    return serializer.ReadObject(reader);
                }
                catch (FileNotFoundException)
                {
                    // Serializer was unable to find assembly where exception is defined 
                    return null;
                }
            }
        }
    }
}
