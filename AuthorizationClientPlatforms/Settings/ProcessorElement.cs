using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;

namespace AuthorizationClientPlatforms.Settings
{
    public class ProcessorElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("description", IsRequired = true)]
        public string Description
        {
            get
            {
                return this["description"] as string;
            }
            set
            {
                this["description"] = value;
            }
        }

        [ConfigurationProperty("server", IsRequired = true)]
        public string Server
        {
            get
            {
                return this["server"] as string;
            }
            set
            {
                this["server"] = value;
            }
        }

        [ConfigurationProperty("endpoint", IsRequired = false, DefaultValue = null)]
        public string Endpoint
        {
            get
            {
                return this["endpoint"] as string;
            }
            set
            {
                this["endpoint"] = value;
            }
        }
    }
}
