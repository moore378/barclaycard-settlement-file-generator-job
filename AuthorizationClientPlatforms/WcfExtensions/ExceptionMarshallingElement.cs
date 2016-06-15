using System;
using System.ServiceModel.Configuration;

namespace AuthorizationClientPlatforms.WcfExtensions
{
    public class ExceptionMarshallingElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get { return typeof(ExceptionMarshallingBehavior); }
        }

        protected override object CreateBehavior()
        {
            return new ExceptionMarshallingBehavior();
        }
    }
}
