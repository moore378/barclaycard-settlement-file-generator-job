using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuthorizationClientPlatforms
{
    public class IsraelPremium : IAuthorizationPlatform
    {
        private static Dictionary<int, string> statusCodes;
        IsraelProcessor.IsraelProcessorServiceClient client;
        private string merchantNumber;
        private string cashierNum;

        public IsraelPremium(string merchantNumber, string cashierNum)
        {
            this.merchantNumber = merchantNumber;
            this.cashierNum = cashierNum;
            client = new IsraelProcessor.IsraelProcessorServiceClient();
        }
                   
            
        public AuthorizationResponseFields Authorize(AuthorizationRequest request, AuthorizeMode mode)
        {
            string transactionRecordResult;
            string resultRecord;
            
            string premiumUserName = request.MerchantID.Trim();
            string premiumPassword = request.MerchantPassword;
            string premiumCashierNum = cashierNum;
            string merchantNumber = this.merchantNumber;
            string transactionDate_yyyyMMdd = request.StartDateTime.ToString("yyyyMMdd");
            string transactionTime_HHmm = request.StartDateTime.ToString("HHmm");
            string track2 = request.TrackTwoData.Trim(';','?');
            string cardNum = request.Pan;
            string expDate_YYMM = request.ExpiryDateMMYY.Substring(2, 2) + request.ExpiryDateMMYY.Substring(0, 2);
            string amount = ((int)(request.AmountDollars * 100)).ToString();
            string transactionType = "01";
            string creditTerms = "1";
            string currency = "1";
            string code = "01";
            string uniqNum = request.MeterSerialNumber;
            string paramJ = "4";
            string last4Digits = request.Pan.Substring(request.Pan.Length - 4, 4);
            
            int res = client.AuthCreditCardFull(
                out transactionRecordResult, // out TransactionRecord
                out resultRecord,  // out ResultRecord
                premiumUserName, //"user1", // premiumUserName
                premiumPassword, //"11111-22222-33333-44444-55555", // premiumPassword
                premiumCashierNum, // premiumCashierNum
                merchantNumber, // merchantNumber
                transactionDate_yyyyMMdd, // transactionDate_yyyyMMdd
                transactionTime_HHmm, // transactionTime_HHmm
                "", // uniqueTransactionNumber_SixDigits
                track2, // track2
                cardNum, // cardNum
                expDate_YYMM, // expDate_YYMM
                amount, // amount
                "", // cochavAmount
                transactionType, // transactionType
                creditTerms, // creditTerms
                currency, // currency
                "", // authNum
                code, // code
                "", // firstAmount
                "", // nonFirstAmount
                "", // numOfPayment
                "", // sapakMutav
                "", // sapakMutavNo
                uniqNum, // uniqNum
                "", // clubCode
                paramJ, // paramJ
                "", // addData
                "", // eci
                "", // cvv2
                "", // id
                "", // cavvUcaf
                last4Digits, // last4Digits
                "", // transactionCurrency
                "" // transactionAmount
                );


            if (resultRecord.Length < 77)
                return new AuthorizationResponseFields(
                (res == 0) ? AuthorizationResultCode.Approved : AuthorizationResultCode.Declined,
                "Response fields incorrect", "Unknown", "Unknown", "", 0, 0);

            Func<int, int, string> readField = (from, num) => resultRecord.Substring(from - 1, num);

            var statusNum = Int32.Parse(readField(1, 3));
            var statusStr = statusCodes.ContainsKey(statusNum) ? statusCodes[statusNum] : statusNum.ToString();
            int fileNumber = 0; Int32.TryParse(readField(96, 2), out fileNumber);


            var brand = Int32.Parse(readField(24, 1));
            string cardType;
            if (brand == 0)
                cardType = "PL";
            else if (brand == 1)
                cardType = "MC";
            else if (brand == 2)
                cardType = "Visa";
            else if (brand == 3)
                cardType = "Maestro";
            else if (brand == 5)
                cardType = "IsraCard";
            else
                cardType = brand.ToString();

            var cardValidityDate = readField(30, 4);
            var transactionAmount = Int32.Parse(readField(36, 8))*0.01m;
            var authorizationNumber = readField(71, 7);
            short batchNum = short.Parse(request.StartDateTime.ToString("MMdd"));

            return new AuthorizationResponseFields(
                (res == 0) ? AuthorizationResultCode.Approved : AuthorizationResultCode.Declined,
                authorizationNumber, cardType, "", statusStr, fileNumber, batchNum);
        }

        public IAuthorizationStatistics Statistics
        {
            get { throw new NotImplementedException(); }
        }

        static IsraelPremium()
        {
            statusCodes = new Dictionary<int, string>()
                {
                    { 000, "authorized." },
                    { 001, "Blocked card. " },
                    { 002, "Stolen card." },
                    { 003, "Referral to credit company." },
                    { 004, "Refuse." },
                    { 005, "Counterfeit." },
                    { 006, "CVV2 or ID is wrong." },
                    { 007, "ECI or CAVV/UCAF is wrong" },
                    { 008, "Error while building Access key into Block list file. " },
                    { 009, "Communication Error between Web server and Credit Card Company." },
                    { 010, "Program stopped by operator or unable to open COM PORT(WINDOWS)" },
                    { 011, "No authorization for ISO Foreign Currency." },
                    { 012, "No authorization for brand with ISO Currency Code" },
                    { 013, "Unauthorized Load/Unload operation." },
                    { 014, "Unsupported card." },
                    { 015, "Entered number does not mach magnetic stripe." },
                    { 016, "Additional data existence does not mach terminal authorizations." },
                    { 017, "Need to supply last 4 digits" },
                    { 019, "INT_IN record shorter than 16 bytes." },
                    { 020, "No input file (INT_IN)" },
                    { 021, "Negative file is absence or outdated, do transmit or else go ON-LINE." },
                    { 022, "No parameters or vectors file exist." },
                    { 023, "Dates file (DATA) missing" },
                    { 024, "Init file (START) missing" },
                    { 025, "Negative file too old (dates) , call SHVA" },
                    { 026, "Negative file too old (generations , call SHVA" },
                    { 027, "When no magnetic strip, need to define the transaction as , '50' – MOTO (Card not present), '51' – Signature only (Customer present when Card not present)." },
                    { 028, "No leading Merchant Number when working as SapakMutav=1" },
                    { 029, "No Mutav account Number when working as SapakMutav=2" },
                    { 030, "SapakMutavNo entered for non SapakMutav terminal" },
                    { 032, "Terminal has old transaction records, call SHVA" },
                    { 033, "Bad card" },
                    { 034, "Unauthorized card or transaction, on this terminal" },
                    { 035, "Card not authorized for this credit terms." },
                    { 036, "Obsolete card" },
                    { 037, "Installments miscalculation" },
                    { 038, "Over ceiling for debit immediate card, go online" },
                    { 039, "Wrong check digit" },
                    { 040, "SapakMutavNo entered for non SapakMutav terminal" },
                    { 041, "Above ceiling when op-code was remain offline (J1,J2,J3)" },
                    { 042, "Block suspicious when op-code was remain offline (J1,J2,J3)" },
                    { 043, "Random on line when op-code was remain offline (J1,J2,J3)" },
                    { 044, "Terminal not authorized for pre authorization." },
                    { 045, "Terminal not authorized for forced authorization " },
                    { 046, "Need authorization when op-code was remain offline (J1,J2,J3)" },
                    { 051, "Invalid car number" },
                    { 052, "Odometer not supplied" },
                    { 053, "Fuel/gas parameters for non petrol station terminal" },
                    { 057, "Missing ID" },
                    { 058, "Missing CVV2" },
                    { 059, "Missing ID and CVV2" },
                    { 061, "Card number Missing or in both Trak2 and CardNum" },
                    { 062, "invalid TransactionType." },
                    { 063, "Invalid Code" },
                    { 064, "Invalid CreditTerms" },
                    { 065, "Invalid currency" },
                    { 066, "FirstAmount or NonFirstAmount entered for non installments CreditTerms" },
                    { 067, "NumOfPayment entered for non installments CreditTerms" },
                    { 068, "Can't link to Index or Dollar for non installments CreditTerms" },
                    { 069, "Magnetic stripe too short" },
                    { 079, "ISO currency code not in table" },
                    { 080, "Invalid CrediteTerms for ClubCode." },
                    { 090, "Cancel transaction not allowed, instead perform load transaction" },
                    { 091, "Cancel transaction not allowed, instead perform unload transaction" },
                    { 092, "Cancel transaction not allowed, instead perform refund transaction" },
                    { 099, "Tran file is unavailable." },
                    { 101, "Terminal is not authorized by acquirer" },
                    { 106, "Terminal is not authorized for debit immediate transactions" },
                    { 107, "Amount more than 8 digits, need to split" },
                    { 108, "Terminal is not authorized to force offlinetransactions " },
                    { 109, "Terminal is not authorized to handle cards with for 587 service code" },
                    { 110, "Terminal is not authorized for debit immediate cards" },
                    { 111, "Terminal is not authorized for Installments transactions" },
                    { 112, "Terminal is not authorized for Installments transactions when card not present" },
                    { 113, "Terminal is not authorized for MOTO transactions" },
                    { 114, "Terminal is not authorized for signature only transactions" },
                    { 115, "Terminal is not authorized for foreign currency transactions or the transaction was refused" },
                    { 116, "Terminal is not authorized for club code transactions" },
                    { 117, "Terminal is not authorized for stars/points/miles discounted transactions" },
                    { 118, "Terminal is not authorized for Isracredit transactions" },
                    { 119, "Terminal is not authorized for AMEX credit (refund) transactions" },
                    { 120, "Terminal is not authorized for Dollar linked transactions" },
                    { 121, "Terminal is not authorized for INDEX linked transactions" },
                    { 122, "Terminal is not authorized for INDEX linked transactions for foreign cards" },
                    { 123, "Terminal is not authorized for stars/points/miles discounted transactions for the given credit terms." },
                    { 124, "Terminal is not authorized for credit (refund) with installments for Isracart card" },
                    { 125, "Terminal is not authorized for credit (refund) with installments for AMEX card" },
                    { 126, "Terminal is not authorized for the given club code" },
                    { 127, " Terminal is authorized for debit immediate transactions to debit immediate cards only" },
                    { 128, "Terminal is not authorized for VISA cards beginning with 3" },
                    { 129, " Terminal is not authorized for credit (refund) transactions above ceiling" },
                    { 130, "The card is not authorized for a club code transaction" },
                    { 131, "The card is not authorized for a stars/points/miles discounted transaction" },
                    { 132, "The card is not authorized for a foreign currency transaction" },
                    { 133, "The card is not valid for this terminal" },
                    { 134, "Invalid card" },
                    { 135, "The card is not authorized for a foreign currency transaction)vector1)" },
                    { 136, " Invalid card (vector20)" },
                    { 137, " Invalid card (vector21)" },
                    { 138, " This card is not authorized by Isracart for installments transaction " },
                    { 139, " Number of installments too big" },
                    { 140, " Visa & Diners cards are not authorized for installments and club transaction" },
                    { 141, " Invalid card (vector5)" },
                    { 142, " Invalid card (service code not in vector 6)" },
                    { 143, " Invalid card (vector 7)" },
                    { 144, " Invalid card (service code not in vector12)" },
                    { 145, " Invalid card (service code not in vector13)" },
                    { 146, " Debit immediate card is not authorized for credit (refund) transaction" },
                    { 147, " The card is not authorized for installment transaction (vector31)" },
                    { 148, " The card is not authorized for card not present transaction (vector31)" },
                    { 149, " The card is not authorized for card not present transaction (vector31)" },
                    { 150, " Unauthorized credit terms for debit immediate card" },
                    { 151, " Unauthorized credit terms for foreign card" },
                    { 152, " Bad club code." },
                    { 153, " The card is not authorized for credit (refund) transaction (vector21)" },
                    { 154, " The card is not authorized for debit immediate transaction (vector21)" },
                    { 155, " Below minimal amount for credit (refund) transaction" },
                    { 156, " Bad NumOfPayments" },
                    { 157, " The card is not authorized for regular or credit (refund) transaction(ceiling 0)" },
                    { 158, " The card is not authorized for card not present transaction (ceiling 0)" },
                    { 159, " The card is not authorized for debit immediate transaction (ceiling 0) " },
                    { 160, " The card is not authorized for card not present transaction (ceiling 0)" },
                    { 161, " The card is not authorized for refund transaction (ceiling 0)" },
                    { 162, " The card is not authorized for installment transaction (ceiling 0)" },
                    { 163, " The foreign AMEX card is not authorized for installment transaction" },
                    { 164, " The foreign JCB card is authorized for regular transaction only" },
                    { 165, " Discount Amount in stars/points/miles exceeds transaction amount" },
                    { 166, " Invalid loyalty card for terminal" },
                    { 167, " A stars/points/miles transaction is not allowed in USD" },
                    { 168, " The card is not authorized for a foreign(USD) currency transaction with the given credit terms." },
                    { 169, " Invalid credit (refund) transaction with other then regular credit terms code" },
                    { 170, " Discount amount in stars/points/miles exceeds permission" },
                    { 171, " A forced off line transaction is not allowed for debit immediate card" },
                    { 172, "Failed to cancel the transaction " },
                    { 173, "Duplicate transaction" },
                    { 174, "Terminal is not authorized for an INDEX linked transaction for the given credit terms" },
                    { 175, " Terminal is not authorized for an Dollar linked transaction for the given credit terms" },
                    { 176, "Invalid card (vector1)" },
                    { 177, "Invalid credit terms for self service in petrol station" },
                    { 178, "A credit (refund) transaction is not allowed in stars/points/miles" },
                    { 179, "A credit (refund) transaction is not allowed in foreign currency" },
                    { 180, "A card not present transaction is not allowed for a loyalty card" },
                    { 200, "Application Error- Invalid data" },
                    { 250, "Name, Password, or Merchant number is wrong" },
                    { 255, "Exception" },
                    { 256, "Unique Transaction Number is wrong (not unique for the given date)" },
                    { 257, "Insufficient data received" },
                    { 260, "One or more of the input fields are wrong (usually not numeric)." },
                    { 280, "Timeout" },
                    { 298, "Attempt to use production terminal in the test environment" },
                    { 300, "Call G.B Premium" }
                };

        }
    }
}
