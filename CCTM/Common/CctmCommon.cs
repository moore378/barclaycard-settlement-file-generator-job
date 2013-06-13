using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuthorizationClientPlatforms;
using CryptographicPlatforms;
using TransactionManagementCommon;
using System.Threading;
using Common;

namespace Cctm.Common
{
    /// <summary>
    /// Groups a set of authorization platforms. Members are Lazy because their existence will only be enforced when they are needed. 
    /// </summary>
    public struct AuthorizationSuite
    {
        public Lazy<IAuthorizationPlatform> Monetra;
        public Lazy<IAuthorizationPlatform> IsraelPremium;
            //CreditCallLive,
            //CreditCallTest,
            //ICVerifyLive,
            //ICVerifyTest,
            //MigsLive,
            //MigsTest;
    }

    public class UpdatedTransactionRecord
    {
        public int TransactionRecordID;
        public string CardEaseReference;
        public string AuthorizationCode;
        public string PAN;
        public string ExpiryDate;
        public string CardScheme;
        public string FirstSix;
        public string LastFour;
        public short BatchNum;
        public int Ttid;
        public TransactionStatus Status;
        public string TrackText;
    }
}
