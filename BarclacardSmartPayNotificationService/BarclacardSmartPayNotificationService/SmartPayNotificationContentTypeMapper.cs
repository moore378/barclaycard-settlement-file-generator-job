using System.ServiceModel.Channels;

namespace BarclacardSmartPayNotificationService
{
    /// <summary>
    /// SmartPayNotificationContentTypeWrapper is a custome WebContentType used to convert
    /// whatever is sent to the service to Raw content. This way the Stream type on the service
    /// parameter can handle the content whatever it may be. This is type is referenced in the 
    /// Web.config.
    /// </summary>
    public class SmartPayNotificationContentTypeMapper : WebContentTypeMapper
    {
        public override WebContentFormat GetMessageFormatForContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return WebContentFormat.Raw;
            else
                return WebContentFormat.Raw;
        }
    }
}