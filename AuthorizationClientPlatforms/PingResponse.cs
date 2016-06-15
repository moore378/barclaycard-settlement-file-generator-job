using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;

namespace AuthorizationClientPlatforms
{
    [DataContract]
    public class PingResponse
    {
        [DataMember]
        public bool Initialized { get; set; }
    }
}
