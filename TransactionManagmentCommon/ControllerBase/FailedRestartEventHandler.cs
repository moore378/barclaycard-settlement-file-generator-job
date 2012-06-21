using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon.ControllerBase
{
    /// <summary>
    /// Delegate to respond to a server restart failure
    /// </summary>
    /// <param name="exception">The exception that was raised during the server restart</param>
    /// <param name="triedCount">The number of times in a row that the server has tried to restart</param>
    /// <returns></returns>
    public delegate RestartFailAction FailedRestartEventHandler(Exception exception, int triedCount);
}
