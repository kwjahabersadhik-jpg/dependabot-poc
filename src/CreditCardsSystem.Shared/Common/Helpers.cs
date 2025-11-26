using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Enums;
using System.Text.RegularExpressions;

namespace CreditCardsSystem.Domain.Common;

public static class Helpers
{
    public static string Format(this DateTime? input, bool showTime = false)
    {
        if (!input.HasValue)
        {
            return string.Empty;
        }

        if (showTime)
        {
            return input.Value.ToString(ConfigurationBase.DateTimeFormat);
        }

        return input.Value.ToString(ConfigurationBase.DateFormat);
    }
    public static string ToLetter(this short amount)
    {
        return new MoneyTafkeet.MoneyStr().GetMoneyString(amount, true, 1);
        //return amount.ToString();
    }

    public static string ToLetter(this decimal amount)
    {
        return new MoneyTafkeet.MoneyStr().GetMoneyString(amount, true, 1);
        //return amount.ToString();
    }

    public static int ToInt(this string? source)
    {
        if (string.IsNullOrEmpty(source) || source == "0") return 0;

        if (int.TryParse(source, out var result)) return result;

        return 0;
    }

    public static long ToLong(this string? source)
    {

        if (string.IsNullOrEmpty(source) || source == "0") return 0;

        if (long.TryParse(source, out var result)) return result;

        return 0;
    }

    public static decimal ToDecimal(this string? source)
    {

        if (string.IsNullOrEmpty(source) || source == "0") return 0;

        if (decimal.TryParse(source, out var result)) return result;

        return 0;
    }

    public static ProductTypes GetProductType(int? duality, decimal? minLimit, decimal? maxLimit)
    {
        if (minLimit == 0 && maxLimit == 0)
            return ProductTypes.PrePaid;

        if (minLimit > 0 && maxLimit > 0 && duality == 7)
            return ProductTypes.Tayseer;

        return ProductTypes.ChargeCard;

    }

    public static IssuanceTypes GetIssuanceType(ProductTypes productTypes)
    {
        if (productTypes is ProductTypes.PrePaid)
            return IssuanceTypes.PREPAID;

        if (productTypes is ProductTypes.Tayseer)
            return IssuanceTypes.TAYSEER;

        return IssuanceTypes.CHARGE;

    }

    public static decimal GetMinCardLimit(int duality, decimal? installments, decimal limit)
    {
        if (duality != 7) return limit;

        if (installments == 12)
            return limit / ConfigurationBase.T12PLF;

        return limit / ConfigurationBase.T3PLF;
    }



    public static bool IsPrepaid(int cardTypeId)
    {
        return cardTypeId is ConfigurationBase.AlOsraPrimaryCardTypeId or ConfigurationBase.AlOsraSupplementaryCardTypeId;
    }
    public static string GetStatusClass(CreditCardStatus status)
    {
        string color = status switch
        {
            CreditCardStatus.Closed
            or CreditCardStatus.Cancelled
            or CreditCardStatus.Rejected
            or CreditCardStatus.Stopped
            or CreditCardStatus.ChargeOff
            or CreditCardStatus.Delinquent30Days
            or CreditCardStatus.Delinquent60Days
            or CreditCardStatus.Delinquent90Days
            or CreditCardStatus.Delinquent120Days
            or CreditCardStatus.Delinquent150Days
            or CreditCardStatus.Delinquent180Days
            or CreditCardStatus.Delinquent210Days
            or CreditCardStatus.Delinquent240Days
            or CreditCardStatus.TemporaryClosedbyCustomer
            or CreditCardStatus.Lost
            or CreditCardStatus.Stolen
                => "red",
            CreditCardStatus.Pending
            or CreditCardStatus.AccountBoardingStarted
            or CreditCardStatus.PendingForCreditCheckingReview
            or CreditCardStatus.PendingForMinorsApproval
            or CreditCardStatus.CreditCheckingReviewRejected
            or CreditCardStatus.CreditCheckingRejected
            or CreditCardStatus.CreditCheckingReviewed
                => "gray",
            CreditCardStatus.Approved
            or CreditCardStatus.Active
            or CreditCardStatus.TemporaryClosed
            or CreditCardStatus.CardUpgradeStarted
                => "blue",
            _ => "orange"
        };

        return $"StatusFlag {color}Status AO-StatusFlag--ml";
    }

    public static string GetRequestActivityStatusClass(RequestActivityStatus status)
    {
        string color = status switch
        {
            RequestActivityStatus.Rejected
            or RequestActivityStatus.Deleted
              => "red",
            RequestActivityStatus.Pending
                => "gray",
            RequestActivityStatus.New
            => "blue",
            RequestActivityStatus.Approved
                => "green",
            _ => "orange"
        };

        return $"StatusFlag {color}Status AO-StatusFlag--ml";
    }
    public static long GetRemainMonths(DateTime openDate)
    {
        DateTime closeDate = DateTime.Now;
        //Calculating the Total months from the Start date of the Monthof the Open date to the Start date of the Closing date
        int monthsApart = 12 * (openDate.Year - closeDate.Year) + openDate.Month - closeDate.Month;

        monthsApart = Math.Abs(monthsApart);
        //Monthly deductions for the Fees are always on the 15th of each month
        //Checking for the Condition that Open Date is grater than 15, so this open date month date will not considered in the Month
        //diff
        if (openDate.Day > 15)
            monthsApart -= 1;
        //Checking for the Condition that Closing date is greater than 15 then it will be added in the total months
        if (closeDate.Day > 15)
            monthsApart += 1;

        //Remaining months for the deduction  will be always the current year
        long remainMonths = 12 - monthsApart;
        if (remainMonths < 0)
            remainMonths = 0;
        return remainMonths;
    }


    public static string IsCreditCardNumber(string content, bool doMask = true)
    {

        if (IsCreditCardNoExists())
            return GetMaskCreditCardNoText();

        return content;

        bool IsCreditCardNoExists()
        {
            int i = 0;
            long j;

            while (i < content.Length)
            {
                if (long.TryParse(content[i].ToString(), out j))
                {
                    if ((i + 16) <= content.Length)
                    {
                        if (long.TryParse(content.Substring(i, 16), out j))
                        {
                            if (content.Substring(i, 1) == "5" || content.Substring(i, 1) == "4")
                            {
                                if (i + 16 == content.Length && long.TryParse(content.Substring(i, 16), out j) == true)
                                    return true;
                                else if (long.TryParse(content.Substring(i, 17), out j) == false)
                                    return true;
                            }
                        }
                    }
                }

                i++;
            }

            return false;
        }

        string GetMaskCreditCardNoText()
        {
            string MaskedText = content;
            int len = content.Length, i = 0;

            long j = 0;

            if (len < 16)
                return MaskedText;

            while (i < len)
            {
                bool isMask = false;

                if (long.TryParse(content.Substring(i, 1), out j) == true)
                {
                    if ((i + 16) <= content.Length)
                    {
                        if (content.Length == 16)
                            isMask = true;

                        if ((i + 16) <= content.Length)
                        {
                            if (i + 16 == content.Length && long.TryParse(content.Substring(i, 16), out j) == true)
                                isMask = true;
                            else if (long.TryParse(content.Substring(i, 17), out j) == false)
                                isMask = true;
                        }

                        if (isMask)
                        {
                            MaskedText = MaskedText.Substring(0, i) + MaskedText.Substring(i, 6) + "XXXXXX" + MaskedText.Substring(i + 12, 4) + MaskedText.Substring(i + 16, len - (i + 16));
                            i = i + 16;
                        }
                    }
                }

                if (!isMask)
                    i++;
            }

            return MaskedText;
        }


    }

    public static int GetAgeByDateOfBirth(DateTime birthDate)
    {

        DateTime today = DateTime.Today;
        int age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
            age--;

        return age;
    }

    public static int? GetPayeeTypeID(int cardType, string? isSupplementaryOrPrimaryChargeCard)
    {
        var isPrimaryPrepaidCard = cardType == ConfigurationBase.AlOsraPrimaryCardTypeId;
        var isPrimaryChargeCard = isSupplementaryOrPrimaryChargeCard == "P";

        if (isPrimaryPrepaidCard)
            return ConfigurationBase.PrimaryPrepaidCardPayeeTypeId;

        if (isPrimaryChargeCard)
            return ConfigurationBase.PrimaryChargeCardPayeeTypeId;

        return null;
    }

    public static (string productName, CardCategoryType cardCategory) GetCreditCardProductName(CardDefinition product, string cardType)
    {
        if (string.IsNullOrEmpty(cardType))
            cardType = "N";

        string? productNameWithType = product.Name;

        if (decimal.TryParse(product.MinLimit, out decimal _minLimit) && decimal.TryParse(product.MaxLimit, out decimal _maxLimit))
            productNameWithType = $"{productNameWithType} -  {Helpers.GetProductType(product.Duality, _minLimit, _maxLimit)}";

        return cardType switch
        {
            "S" => ($"{productNameWithType} (Supplementary)", CardCategoryType.Supplementary),
            "P" => ($"{productNameWithType} (Primary)", CardCategoryType.Primary),
            "N" => ($"{productNameWithType}", CardCategoryType.Normal),
            _ => ($"{productNameWithType}", CardCategoryType.Normal)
        };
    }

    #region Validate File Header

    public static bool IsValidFile(string fileName, byte[] FileContent, bool isValidExtension, int maxFileSizeInMB = ConfigurationBase.MaxFileSizeInMB)
    {
        bool isValidFileName = Regex.IsMatch(fileName, ConfigurationBase.FileNameFormat);
        int maxFileSize = maxFileSizeInMB * 1024 * 1024;
        bool isValidFileSize = FileContent!.Length <= maxFileSize;
        //bool isValidHeader = IsFileHeaderValid(FileContent);

        if (!isValidFileName || !isValidFileSize || !isValidExtension)
            return false;

        return true;
    }

    public static bool IsFileHeaderValid(byte[] FileContent)
    {
        using var reader = new BinaryReader(new MemoryStream(FileContent));
        var signatures = _fileSignatures.Values.SelectMany(x => x).ToList();  // flatten all signatures to single list
        var headerBytes = reader.ReadBytes(_fileSignatures.Max(m => m.Value.Max(n => n.Length)));
        bool result = signatures.Any(signature => headerBytes.Take(signature.Length).SequenceEqual(signature));
        return result;
    }

    private static readonly Dictionary<string, List<byte[]>> _fileSignatures =

        new()
            {
                { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
                { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
                { ".jpeg", new List<byte[]>
                {
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xEE },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xDB },
                }
                },
    { ".jpg", new List<byte[]>
        {
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 },
            new byte[] { 0xFF, 0xD8, 0xFF, 0xEE },
            new byte[] { 0xFF, 0xD8, 0xFF, 0xDB },
    }
    },
    { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },

    { ".tif", new List<byte[]>
        {
            new byte[] { 0x49, 0x49, 0x2A, 0x00 },
            new byte[] { 0x4D, 0x4D, 0x00, 0x2A }
        }
            },
    { ".tiff", new List<byte[]>
        {
            new byte[] { 0x49, 0x49, 0x2A, 0x00 },
            new byte[] { 0x4D, 0x4D, 0x00, 0x2A }
        }
            },
    { ".docx", new List<byte[]>
        {
            new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14},
              new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1},
              new byte[] { 0xEC, 0xA5, 0xC1, 0x00},
    }
            }
        };

    #endregion

}
