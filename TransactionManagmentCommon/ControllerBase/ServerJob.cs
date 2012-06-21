using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon.ControllerBase
{
    public delegate TResult ServerJob<TResult>(ref bool successfull);
}
