using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    public enum TransactionMode
    {
        RealtimeNormal = 1,
        BatchNormal = 2,
        RealtimeDualAuth = 3,
        BatchDualAuth = 4
    }
}
