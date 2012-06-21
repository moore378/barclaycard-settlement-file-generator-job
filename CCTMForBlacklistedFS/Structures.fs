#light
namespace CctmForBlacklisted

open System
open TransactionManagementCommon

type Source = {
    TerminalSerNo: string
    }

type Encoding = {
    EncryptionVersion: int
    TransactionIndex: int
    KeyVer: int
    }

type Transaction = {
    Date: System.DateTime
    Amount: decimal
    }

type DeclinedRecord = {
    EncryptedTrack: string
    Transaction: Transaction
    Encoding: Encoding
    Source: Source
    }

type SlitRecord = {
    DecryptedTrack: string 
    }