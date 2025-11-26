namespace CreditCardsSystem.Domain.Interfaces.Workflow
{
    public interface IWorkflowCalculationService
    {
        /// <summary>
        /// Calculating Percentage based on Collateral and Card Definition (Duality,MonthlyMaxDue, etc) 
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        /// <exception cref="ApiException"></exception>
        /// 
        Task<decimal> GetPercentage(decimal requestId);
    }

}
