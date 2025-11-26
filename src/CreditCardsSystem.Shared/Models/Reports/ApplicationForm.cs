using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.Customer;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.ConfigParameter;
using CreditCardsSystem.Utility.Extensions;

namespace CreditCardsSystem.Domain.Models.Reports;


public class ApplicationForm : RebrandDto
{
    private readonly ProfileDto profile;
    private readonly RequestDto request;
    private readonly GenericCustomerProfileDto genericProfile;
    private readonly CustomerLookupData lookup;
    private readonly List<ConfigParameterDto>? accountTypeConfigs;
    private readonly CustomerAddressDto? residenceAddress;
    private readonly string[] addressDetail;

    public ApplicationForm(ProfileDto profile, RequestDto request, GenericCustomerProfileDto genericProfile, CustomerLookupData lookup, List<ConfigParameterDto>? accountTypeConfigs)
    {
        this.profile = profile;
        this.request = request;
        this.genericProfile = genericProfile;
        this.lookup = lookup;
        this.accountTypeConfigs = accountTypeConfigs;
        residenceAddress = genericProfile.CustomerAddresses?.FirstOrDefault(x => x.AddressId == 1);
        addressDetail = residenceAddress?.AddressLine2 != null ? residenceAddress!.AddressLine2.Replace("Block:", "").Replace("Street:", "").Replace("Avenue:", "").Split(' ') : [];
    }
    #region Properties
    #region Header

    public string PrintDate => DateTime.Now.ToString("dd/MM/yyyy");
    public int ProductId => request.CardType;
    public string ProductName { get; set; } = string.Empty;
    public string RimNumber { get; set; } = string.Empty;
    public string Barcode => $"010{request.CivilId}{ProductId}{RimNumber}";
    public string Branch { get; set; } = string.Empty;
    public string RequestDate => request.ReqDate.ToString("dd/MM/yyyy");

    #endregion

    #region Body

    public string CardHolderName => profile.HolderName;
    public string? ArabicName => profile.ArabicName;
    public string RequestId => request.RequestId.ToString();
    public string CivilID => request.CivilId;
    public string CivilIDExpiryDate => genericProfile.CIDExpiryDate?.ToString("dd/MM/yyyy") ?? "";
    public string? Nationality
    {
        get
        {
            if (lookup.NationalityLookupData.AnyWithNull() && profile.Nationality is not null)
            {
                return lookup.NationalityLookupData?.FirstOrDefault(x => x.Key == profile.Nationality)?.Value;
            }
            else
            {
                return string.Empty;
            }
        }
    }
    public string DateOfBirth => profile.Birth.ToString("dd/MM/yyyy") ?? "";

    #region Residence Details as in Civil Id

    public string? Street => profile.Street;
    public string? Block => profile.BlockNo;
    //public string? Area => profile.Area;

    public string? Area
    {
        get
        {
            if (lookup.AreaCode.AnyWithNull() && profile.Area is not null)
            {
                return lookup.AreaCode?.FirstOrDefault(x => x.Key == profile.Area.ToInt())?.Value;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public string? Flat => profile.Flatno;
    public string? HouseBuilding => residenceAddress?.AddressLine3.Replace("Home:", "").Replace("Floor:", "").Replace("Flat:", "").Split(' ')[0].ToString().Trim();
    public string Avenue
    {
        get
        {
            try
            {
                return addressDetail[2]?.Trim() ?? "";
            }
            catch (Exception ex)
            {

                return "";
            }

        }
    }
    public string HomePhone => request.HomePhone.ToString();
    public string? Mobile => request.Mobile.ToString();
    #endregion

    #region Mailling Address

    public string? POBox => request.PostOfficeBoxNumber.ToString();
    public string ZipCode => request.PostalCode.ToString();
    public string City => request.City.ToString();
    public string? Email => profile.Email;

    #endregion

    #region Place and Address of Occupation

    public string? EmployerArea => profile.EmployerArea;
    public string? EmployerSection => profile.EmployerSection;
    public string? EmployerDepartment => profile.EmployerDepartment;
    public string? WorkFaxNo => request.FaxReference;
    public string? WorkTelNo => request.WorkPhone.ToString();

    #endregion

    #region Name and Address of Close Friend

    //public string Relationship { get; set; }
    //public string Name { get; set; }
    //public string Address { get; set; }
    //public string MobileNo { get; set; }

    #endregion

    #region Co-Brand MemberShip

    public string? ClubMemberShipId { get; set; }

    #endregion

    #region Upgrade/Downgarde

    public string? OldCardNo { get; set; }

    #endregion

    #region Is Prepaid

    public bool? IsPrepaid { get; set; }

    #endregion

    public string CurrentJob => profile.EmployerName;
    public string? EducationQualification
    {
        get
        {
            if (lookup.EducationLookupData.AnyWithNull() && genericProfile.EducationId is not null)
            {
                return lookup.EducationLookupData?.FirstOrDefault(x => x.Key == genericProfile.EducationId.ToInt())?.Value;
            }
            else
            {
                return string.Empty;
            }
        }
    }
    public string? DurationInService => profile.DurationService;
    public string? CurrentSalary => request.Salary.ToString();
    public string? CustomerActivity { get; set; }
    public string? MaritalStatus => genericProfile.MaritalStatus;
    public string? Gender => genericProfile.Gender;
    public string? AccountType
    {
        get
        {

            string bankAccountCode = request.AcctNo?.Substring(2, 3) ?? "";

            var accountType = accountTypeConfigs?.FirstOrDefault(pc => pc.ParamValue.Split(",").Any(pv => pv.Equals(bankAccountCode, StringComparison.InvariantCultureIgnoreCase)));

            if (accountType is not null)
                return accountType.ParamName.Replace(ConfigurationBase.AccountType, "");

            if (Enum.TryParse(typeof(BankAccountType), bankAccountCode, out object? _bankAccountCode))
                return _bankAccountCode.ToString();
            else
                return "";
        }
    }
    public string? AccountNumber => request.AcctNo;
    public bool IsSupplementary { get; set; }
    public bool HasSupplementary => Supplementary?.Count > 0;
    public List<SupplementaryRequestDetailsDTO>? Supplementary { get; set; }
    #endregion

    #endregion Properties




}


public class EFormResponse
{
    public Guid FileId { get; set; }
    public byte[]? FileBytes { get; set; }
    public string FileName { get; set; }
}