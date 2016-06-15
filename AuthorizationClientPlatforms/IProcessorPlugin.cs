using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuthorizationClientPlatforms
{
    public interface IProcessorPlugin
    {
        void ModuleInitialize(Dictionary<string, string> configuration);

        void ModuleShutdown();

        AuthorizationResponseFields AuthorizePayment(AuthorizationRequest request, AuthorizeMode mode);
    }
}
