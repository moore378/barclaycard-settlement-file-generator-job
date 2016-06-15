using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;

namespace AuthorizationClientPlatforms.Settings
{
    public class AuthorizationProcessorsCollection : ConfigurationElementCollection
    {
        public ProcessorElement this[object key]
        {
            get
            {
                return base.BaseGet(key) as ProcessorElement;
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override string ElementName
        {
            get
            {
                return "processor";
            }
        }

        protected override bool IsElementName(string elementName)
        {
            bool isName = false;
            if (!String.IsNullOrEmpty(elementName))
                isName = elementName.Equals("processor");
            return isName;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ProcessorElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ProcessorElement)element).Name;
        }
    }
}
