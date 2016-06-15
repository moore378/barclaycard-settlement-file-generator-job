using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;

namespace AuthorizationClientPlatforms.Settings
{
    public class AuthorizationClientPlatformsSection : ConfigurationSection
    {
        [ConfigurationProperty("authorizationProcessors")]
        public AuthorizationProcessorsCollection AuthorizationProcessors
        {
            get
            {
                return this["authorizationProcessors"] as AuthorizationProcessorsCollection;
            }
        }
    }
}
