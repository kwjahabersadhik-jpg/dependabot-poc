using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.Customer;
using Kfh.Aurora.Common.Shared.Models.Customer;

namespace CreditCardsSystem.Web.Client.Pages.CustomerProfile;

public class AppState
{
    public bool ValidCustomer => CustomerProfile is not null && CustomerProfile?.RimNumber != 0;
    private string? _currentCivilId;

    public string? CurrentCivilId
    {
        get { return _currentCivilId?.Trim(); }
        set { _currentCivilId = value; }
    }

    public decimal? CurrentRequestId { get; set; }

    public string? PrimaryCivilId { get; set; }
    public decimal? PrimaryRequestId { get; set; }

    public string? SupplementaryCivilId { get; set; }
    public decimal? SupplementaryRequestId { get; set; }

    public bool IsFromSpplementay { get; set; }

    public bool HavingRequestId => PrimaryRequestId != null || SupplementaryRequestId != null;

    public CommonCustomerProfile? CustomerProfile { get; set; }
    public GenericCustomerProfileDto GenericCustomerProfile { get; set; }
}


public class ApplicationState
{
    public DataItem<List<CreditCardDto>> CreditCards { get; set; } = new();
    public DataItem<GenericCustomerProfileDto> GenericCustomerProfile { get; set; } = new();
    public DataItem<GenericCustomerProfileDto> GenericCorporateProfile { get; set; } = new();
    public DataItem<RelationManagerDto?> RelationManager { get; set; } = new();
    public DataItem<List<StandingOrderDto>> StandingOrders { get; set; } = new();
}




