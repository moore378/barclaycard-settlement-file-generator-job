using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransactionManagementCommon
{
    public interface IValidatable
    {
        void Validate(string failStatus);
    }
}
