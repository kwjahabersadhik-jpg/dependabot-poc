using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Models.Card;
using CreditCardsSystem.Utility.Extensions;
using System.Text;

namespace CreditCardsSystem.Domain.Shared.Models.Account;

public class FinancialPosition
{
    private readonly Collateral? _collateral;
    private readonly CustomerProfileInfo _customer;
    private readonly CardInfo _card;
    private readonly CardCurrencyDto? _cardCurrency;
    private AccountDetailsDto? _selectedCardAccount;
    private readonly AccountDetailsDto? _selectedDebitAccount;
    private readonly CardDefinitionDto _cardDefinition;
    private readonly bool _isUsdCard;
    private bool isInSufficientLimit = false;

    public FinancialPositionResponse? Applications { get; set; }

    public FinancialPosition(FinancialPositionResponse? data, CardIssueRequest request, CardCurrencyDto? cardCurrency, AccountDetailsDto? selectedCardAccount, AccountDetailsDto? selectedDebitAccount, CardDefinitionDto cardDefinition)
    {
        //_request = request;
        _cardCurrency = cardCurrency!;
        _selectedCardAccount = selectedCardAccount;
        _selectedDebitAccount = selectedDebitAccount;
        _cardDefinition = cardDefinition;
        _collateral = request.IssueDetailsModel.Collateral;
        _customer = request.Customer;
        _card = request.IssueDetailsModel.Card;
        Applications = data;
        _cardCurrency.BuyCashRate = data!.UsdRate?.BuyCashRate;
        _cardCurrency.SellCashRate = data!.UsdRate?.SellCashRate;
        _isUsdCard = _cardCurrency!.CurrencyIsoCode == ConfigurationBase.USDollerCurrency;
        ForeignCurrency = data!.UsdRate;
        _ = Calculate();
    }

    public CreditCardApplication? SelectedApplication { get; set; }

    public ForeignCurrencyResponse? ForeignCurrency { get; set; }
    //TODO : Set this value on balance status call
    public bool IsIgnoreExpiredCard { get; set; } = false;
    public decimal MaximumLimit { get; set; }
    public decimal MonthlyDeduction { get; set; }
    public decimal DebitAccountBalance { get { return _selectedDebitAccount?.AvailableBalance ?? 0; } }
    public decimal TotalCreditCardLimit
    {
        get
        {
            if (IsIgnoreExpiredCard)
            {
                return Applications!.CreditCardApplications.Where(x => DateTime.ParseExact(x.Expiry, "yyyyMMdd", null) > DateTime.Today).Sum(x => x.MinimumCardLimit);
            }

            return Applications!.CreditCardApplications.Sum(x => x.MinimumCardLimit);
        }
    }
    public decimal TotalRealEstateInstallments
    {
        get
        {
            return Applications!.RealEstateApplications.Sum(x => x.InstallmentAmount);
        }
    }
    public decimal TotalMurabahaInstallments
    {
        get
        {
            return Applications!.TradingApplications.Sum(x => x.InstallmentAmount);
        }
    }
    public decimal TotalInstallmentsCINET
    {
        get { return _customer.TotalCinet ?? 0; }
    }

    public decimal TotalDuties
    {
        get
        {
            if (SelectedApplication is null || !SelectedApplication.IsValid)
                return TotalFixedDuties;

            decimal minmumCardLimits = Applications!.CreditCardApplications.FirstOrDefault(x => x.ProductId == SelectedApplication.ProductId)?.MinimumCardLimit ?? 0;
            return TotalFixedDuties - minmumCardLimits;
        }
    }

    public decimal TotalFixedDuties
    {
        get
        {
            return TotalCreditCardLimit + TotalRealEstateInstallments + TotalMurabahaInstallments;
        }
    }
    public decimal MaxAvailableLimitKFH { get; set; }
    public decimal MaxAvailableLimitCBK { get; set; }
    //TODO: Make it private

    private decimal _t3MaxLimit;
    public decimal T3MaxLimit
    {
        get
        {
            if (_collateral is not Collateral.AGAINST_SALARY)
                return _t3MaxLimit;

            return _t3MaxLimit > ConfigurationBase.T3Limit ? ConfigurationBase.T3Limit : _t3MaxLimit;
        }
        set => _t3MaxLimit = value;
    }


    private decimal _t12MaxLimit;

    public decimal T12MaxLimit
    {
        get
        {
            if (_collateral is not Collateral.AGAINST_SALARY)
                return _t12MaxLimit;

            return _t12MaxLimit > ConfigurationBase.T12Limit ? ConfigurationBase.T12Limit : _t12MaxLimit;
        }
        set => _t12MaxLimit = value;
    }

    public decimal MaxAvailableLimitKFH_USD { get; internal set; }
    public decimal MaxAllowedDue { get; internal set; }
    public bool UsedMarginAmount(string? accountNumber, out decimal usedAmounts)
    {
        usedAmounts = _collateral != Collateral.AGAINST_MARGIN ? 0 : Applications?.CreditCardApplications.Where(x => x.IsValid && x.MarginAccount == accountNumber).Sum(x => x.CardLimit) ?? 0;
        return usedAmounts > 0;
    }

    public async Task Calculate(AccountDetailsDto? cardAccount = null) //Do double validate in service
    {
        bool isNotUSDAccount = false;

        if (cardAccount != null)
        {
            _selectedCardAccount = cardAccount;
            isNotUSDAccount = _selectedCardAccount!.Currency != ConfigurationBase.USDollerCurrency;
        }

        MaximumLimit = _collateral switch
        {
            Collateral.AGAINST_SALARY or Collateral.AGAINST_SALARY_USD => _customer.Salary ?? 0,
            Collateral.AGAINST_CORPORATE_CARD => _card.MaxLimit,
            Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_MARGIN => _selectedCardAccount?.AvailableBalance ?? 0,
            _ => 1
        };

        MaxAvailableLimitKFH = MaximumLimit - TotalDuties;
        MaxAvailableLimitCBK = 0;
        T3MaxLimit = MaximumLimit;
        T12MaxLimit = MaximumLimit;

        if (_collateral is Collateral.AGAINST_MARGIN)
        {
            if (!isInSufficientLimit)
            {
                decimal debitAccountBalance = Math.Floor(_selectedDebitAccount?.AvailableBalance ?? 0);
                decimal balanceAfterTenPercentageDeduction = (debitAccountBalance - (debitAccountBalance % 10));
                MaxAvailableLimitKFH = MaximumLimit + balanceAfterTenPercentageDeduction;

                if (UsedMarginAmount(_selectedCardAccount?.Acct, out decimal _usedMarginAmounts))
                {
                    MaxAvailableLimitKFH -= _usedMarginAmounts;
                }
            }

            T3MaxLimit = MaxAvailableLimitKFH;
            T12MaxLimit = MaxAvailableLimitKFH;
        }

        //Tasyeer limits calculation
        if (_collateral is Collateral.AGAINST_SALARY)
        {
            decimal empMaxAllowDue = MaximumLimit * (_customer.IsRetiredEmployee ? ConfigurationBase.MALPercentRetired : ConfigurationBase.MALPercentEmployed);
            decimal empMaxPayableDue = Math.Max(0, empMaxAllowDue - TotalDuties);
            MaxAvailableLimitCBK = empMaxPayableDue;


            T12MaxLimit = Math.Round(empMaxPayableDue, 1) * ConfigurationBase.T12PLF;
            T3MaxLimit = (empMaxPayableDue * (ConfigurationBase.T3PLF / 10)) * 10;
        }


        if (_isUsdCard)
        {
            MaxAvailableLimitKFH_USD = isNotUSDAccount ? Math.Floor(MaxAvailableLimitKFH / Convert.ToDecimal(_cardCurrency?.BuyCashRate)) : MaxAvailableLimitKFH;
        }

        _card.T3MaxLimit = T3MaxLimit;
        _card.T12MaxLimit = T12MaxLimit;
        _card.MaxLimit = _isUsdCard ? MaxAvailableLimitKFH_USD : MaxAvailableLimitKFH;
        _card.MaxPercentage = _customer.IsRetiredEmployee ? ConfigurationBase.MALPercentRetired : ConfigurationBase.MALPercentEmployed;
        await Task.CompletedTask;
        return;
    }

    public async Task<(bool valid, StringBuilder message)> ValidateRequiredLimit()
    {
        StringBuilder message = new();
        isInSufficientLimit = false;

        if (_card.RequiredLimit > _card.MaxLimit)
            message.AppendLine($"The highest limit available ({_card.MaxLimit.ToMoney()}) is less than the requested limit ({_card.RequiredLimit.ToMoney()})");

        //TODO : Check Cinet exception to do the below validation
        //Validation against Tayseer Card 
        if (_cardDefinition!.Duality == 7)
        {
            decimal requiredPercentage = (_card.RequiredLimit / _card.MaxLimit);
            if (requiredPercentage > ConfigurationBase.MaximumRequiredLimitPercentage || requiredPercentage < 0)
                message.AppendLine($"The highest limit available ({_card.MaxLimit.ToMoney()}) is not enough to issue card with limit({_card.RequiredLimit.ToMoney()})");

            await ValidateWithCBKRules();
        }

        if (message.Length > 0)
        {
            //Show insufficient data only for non usd cards
            if (!_isUsdCard)
                isInSufficientLimit = true;

        }

        await Calculate();

        return (message.Length == 0, message);

        async Task ValidateWithCBKRules()
        {
            //if (_collateral != Collateral.AGAINST_SALARY)
            //    return;

            decimal plf = _cardDefinition!.Installments == 3 ? ConfigurationBase.T3PLF : ConfigurationBase.T12PLF;
            decimal maxMonthlyDue = Math.Round(_card.RequiredLimit / plf);
            decimal maxAllowDue = MaximumLimit * (_customer.IsRetiredEmployee ? ConfigurationBase.MALPercentRetired : ConfigurationBase.MALPercentEmployed);
            decimal maxPayableDue = Math.Max(0, maxAllowDue - TotalDuties);

            if (maxMonthlyDue <= maxPayableDue)
                return;

            decimal requiredCBKDepositAmount = (maxMonthlyDue - MaxAvailableLimitCBK) * plf;
            message.AppendLine($"The request violates CBK rules and regulations by{requiredCBKDepositAmount}");

            await Task.CompletedTask;
        }
    }

    public bool IsTransferrable
    {
        get
        {
            if (SelectedApplication?.DaysDelinquent > 30)
                return false;

            //Can add many condition
            return true;
        }
    }

    public bool IsTransferrableTayseer
    {
        get
        {
            if (!IsTransferrable)
                return false;

            return SelectedApplication?.UpgradeMatrix.Split(',').Any(x => x == _card.ProductId.ToString()) ?? false;
        }
    }


}
