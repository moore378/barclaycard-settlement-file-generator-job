#light
module Main

open CctmForBlacklisted
open System
open TransactionManagementCommon
open CryptographicPlatforms
open Common

// Converts a list of chars of the form ('0','1','2','3'...) to a list of bytes (0x01, 0x23...)
let rec decodeHex hex = 
    match hex with
        |hi::lo::tail -> Convert.ToByte("0x"+hi.ToString()+lo.ToString(), 16)::(decodeHex tail)
        |hi::[] -> failwith "Invalid hex string"
        |_ -> []

let readTracksFromDBField (dbTracks: string) =
    dbTracks.ToCharArray() // Convert to array
        |> List.ofArray // Convert to list
        |> decodeHex // Decode
        |> Array.ofList
        |> fun x -> new EncryptedStripe(x)

let decrypt (encryptionMethod, keyVersion, info) stripe = 
    let decryptor = new StripeDecryptor()
    let plainDecrypted = decryptor.decryptStripe(stripe, encryptionMethod, keyVersion, info, "")
    let formattedData = TrackFormat.FormatSpecialStripeCases(plainDecrypted, encryptionMethod, "")
    new CreditCardStripe(formattedData)

let makeDate (s: string) = 
    Convert.ToDateTime(s)

let record1 = { 
    EncryptedTrack = "2C0CFEE82AFE7E72293ACD8B0A5BF34E245B23642834191F612AEC6C4CD70F19D04A4C261855FE0F4C4CC1423D647239B7672E6E0FECA214955BE45617C8AB838C0D0DDE0FAD339578141E89B2653F1B471390C9276ABE95EEBAC744C6F5A6E31563CE7B4442B61EC38E5E5469602EC978D33003E7F95E7E5AA9D415E4E73DF5"
    Transaction = { Date = makeDate("2010-09-20 09:26:24.000"); Amount = 10m }
    Encoding = { EncryptionVersion = 0; TransactionIndex = 274; KeyVer = 1 }
    Source = { TerminalSerNo = "0025859" }
    }

let getInfo record = new TransactionInfo(record.Transaction.Date, record.Encoding.TransactionIndex, record.Source.TerminalSerNo, record.Transaction.Amount);
let getEncMethod(record):EncryptionMethod = enum record.Encoding.EncryptionVersion
let getKeyVer record = record.Encoding.KeyVer

let split (stripe:CreditCardStripe) = 
    stripe.SplitIntoTracks("").ParseTrackTwo("")


let separate trans = 
    trans.EncryptedTrack 
        |> readTracksFromDBField // Decode
        |> decrypt(getEncMethod(trans), getKeyVer(trans), getInfo(trans)) // Decrypt
        |> split
        