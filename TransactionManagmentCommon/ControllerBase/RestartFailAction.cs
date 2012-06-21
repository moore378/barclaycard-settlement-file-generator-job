using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon.ControllerBase
{
    public enum RestartFailAction
    {
        /// <summary>
        /// Attempt to restart the server again
        /// </summary>
        Retry,
        /// <summary>
        /// Abort the server
        /// </summary>
        Abort
    };
}
