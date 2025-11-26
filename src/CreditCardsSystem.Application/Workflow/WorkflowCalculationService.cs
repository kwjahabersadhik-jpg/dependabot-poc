using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces.Workflow;
using CreditCardsSystem.Domain.Models.CardIssuance;

namespace CreditCardsSystem.Application.Workflow;

public class WorkflowCalculationService : IWorkflowCalculationService
{
    private readonly ICardDetailsAppService cardDetailsAppService;
    private readonly IRequestAppService requestAppService;

    public WorkflowCalculationService(ICustomerProfileAppService customerProfileAppService, ICardDetailsAppService cardDetailsAppService, IRequestAppService requestAppService)
    {
        this.cardDetailsAppService = cardDetailsAppService;
        this.requestAppService = requestAppService;
    }

    /// <summary>
    /// Calculating Percentage based on Collateral and Card Definition (Duality,MonthlyMaxDue, etc) 
    /// </summary>
    /// <param name="requestId"></param>
    /// <returns></returns>
    /// <exception cref="ApiException"></exception>
    public async Task<decimal> GetPercentage(decimal requestId)
    {


        var requestDetail = (await requestAppService.GetRequestDetail(requestId))?.Data ?? throw new ApiException(message: "Invalid RequestId");
        _ = Enum.TryParse(requestDetail.Parameters.Collateral, out Collateral _collateral);


        if (_collateral is Collateral.EXCEPTION)
            return 1.5M;

        CardDefinitionDto cardDetail = await cardDetailsAppService.GetCardWithExtension(requestDetail.CardType);

        //TODO: Check
        //requestDetail.Parameters.MaxLimit ??= cardDetail.MaxLimit.ToString();

        _ = decimal.TryParse(requestDetail.Parameters.MaxLimit, out decimal _maxLimit);

        if (cardDetail.Duality != ConfigurationBase.DualityFlag)
            return requestDetail.RequestedLimit / (_maxLimit <= 0 ? 1 : _maxLimit);



        //if (_collateral is not Collateral.EXCEPTION)
        //    return 0;


        _ = bool.TryParse(requestDetail.Parameters.IsCBKRulesViolated, out bool _isCBKRulesViolated);
        _ = decimal.TryParse(requestDetail.Parameters.TotalFixedDuties, out decimal _totalDue);

        if (_isCBKRulesViolated)
        {
            decimal maxMonthlyDue = cardDetail.MonthlyMaxDue ?? 0;
            return (_totalDue + maxMonthlyDue) / _maxLimit;
        }


        return 0;


        //decimal getMaxLimit()
        //{
        //    if (_collateral is Collateral.AGAINST_SALARY or Collateral.AGAINST_SALARY_USD)
        //        return requestDetail!.Salary ?? 0;

        //    if (cardDetail.Eligibility!.IsCorporate)
        //        return cardDetail.MaxLimit ?? 0;

        //    if (_collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD)
        //        return requestDetail!.DepositAmount ?? 0;

        //    if (_collateral is Collateral.AGAINST_MARGIN)
        //    {
        //        _ = decimal.TryParse(requestDetail!.Parameters.MarginAmount, out decimal _marginAmount);
        //        return _marginAmount;
        //    }

        //    return 1;
        //}
    }




}
