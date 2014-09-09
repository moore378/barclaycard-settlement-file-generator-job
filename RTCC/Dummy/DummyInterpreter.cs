using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rtcc.RtsaInterfacing;

namespace Rtcc.Dummy
{
    public class DummyInterpreter : RtccRequestInterpreter
    {
        public ClientAuthRequest Request;

        public Action<ClientAuthResponse> onResponse;

        public DummyInterpreter(ClientAuthRequest request)
        {
            this.Request = request;
        }

        /// <summary>
        /// Pretends to parse the message, but in fact just returns the request object specified by this.Request.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override ClientAuthRequest ParseMessage(byte[] message, string failStatus)
        {
            return Request;
        }
        public override RawDataMessage SerializeResponse(ClientAuthResponse reply)
        {
            onResponse(reply);
            return new RawDataMessage(new System.IO.MemoryStream());
        }
    }
}
