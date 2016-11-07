using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;

using AuthorizationClientPlatforms.WcfExtensions;

namespace AuthorizationClientPlatforms
{
    public class AuthorizationPlatform : IAuthorizationPlatform
    {
        IAuthorizationProcessor _processor;

        public AuthorizationPlatform(string server, string processor, Dictionary<string, string> configuration)
        {
            // Forcing this to be HTTPS
            BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);

            // Standardize on a URL convention.
            EndpointAddress endpoint = new EndpointAddress(String.Format("https://{0}:56341/AuthorizationProcessors/{1}/", server, processor));

            ChannelFactory<IAuthorizationProcessor> channelFactory = new ChannelFactory<IAuthorizationProcessor>(binding, endpoint);

            // Allow exceptions to go through.
            channelFactory.Endpoint.Behaviors.Add(new ExceptionMarshallingBehavior());

            _processor = channelFactory.CreateChannel();

            //PingResponse response = _processor.Ping();

            // Pass in the configuration parameters.
            _processor.Initialize(configuration);
        }

        // Lightweight wrapper for calling the actual service.
        public AuthorizationResponseFields Authorize(AuthorizationRequest request, AuthorizeMode mode)
        {
            AuthorizationResponseFields response;

            try
            {
                response = _processor.AuthorizePayment(request, mode);
            }
            // The service is not running at all. Need to return this back as a "timeout"
            catch (EndpointNotFoundException e)
            {
                throw new AuthorizerProcessingException("Not connected to authorization processor", e, true);
            }
            catch (System.ServiceModel.CommunicationException e)
            {
                throw new AuthorizerProcessingException("Break down in authorization processor communications", e, true);
            }
            catch (Exception e)
            {
                throw;
            }

            return response;
        }

        /// <summary>
        /// Doesn't look like anybody is using this.
        /// </summary>
        public IAuthorizationStatistics Statistics
        {
            get { throw new NotImplementedException(); }
        }
    }
}
