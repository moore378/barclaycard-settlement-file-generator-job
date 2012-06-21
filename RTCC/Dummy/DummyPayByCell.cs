using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rtcc.Main;
using Rtcc.PayByCell;

namespace Rtcc.Dummy
{
    public class DummyPayByCell : PayByCellClient
    {
        public override void RegisterTransaction(
            long accountNumber,
            int poleID,
            string poleSerialNumber,
            DateTime startDateTime,
            double amount,
            int purchasedTime,
            string authorizationCode,
            string phoneNumber)
        {
            // Do nothing
        }
    }
}
