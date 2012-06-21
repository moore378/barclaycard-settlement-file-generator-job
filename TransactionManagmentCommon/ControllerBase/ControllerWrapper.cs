using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon.ControllerBase
{
    public class ControllerWrapper<ServerType>
        where ServerType : class
    {
        protected ServerController<ServerType> Controller { get; private set; }

        public ControllerWrapper(ServerController<ServerType> controller)
        {
            this.Controller = controller;
            if (!(this is ServerType))
                throw new NotSupportedException("Controller wrapper must support server type");
        }
    }
}
