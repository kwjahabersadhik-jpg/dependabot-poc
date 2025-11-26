using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IWorkflowMethods
{
    public Task Cancel();
    public Task ProcessAction(ActionType actionType, string ReasonForRejection = "");
    public Task<bool> SubmitRequest(CancellationToken? token = default);
    public Task PrintApplication();

}
