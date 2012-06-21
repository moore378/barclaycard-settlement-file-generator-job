#light
namespace CctmForBlacklisted

open System
open TransactionManagementCommon
open CryptographicPlatforms
open System.IO

type BlacklistedProcessor = 
    static member SeparateTransaction trans = Main.separate trans;