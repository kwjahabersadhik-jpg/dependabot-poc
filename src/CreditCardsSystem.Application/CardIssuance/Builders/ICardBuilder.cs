using CreditCardsSystem.Domain.Models.CardIssuance;

namespace CreditCardsSystem.Application.CardIssuance.Builders;

public interface ICardBuilder
{
    /// <summary>
    /// Sending request to builder to construct database data 
    /// </summary>
    /// <param name="cardRequest"></param>
    /// <param name="cardIssuanceApp"></param>
    /// <returns></returns>
    public ICardBuilder WithRequest(BaseCardRequest cardRequest);

    /// <summary>
    /// Validating model and business logics
    /// </summary>
    /// <returns></returns>
    public Task Validate();

    /// <summary>
    /// This method will prepare request table, request parameter table data
    /// </summary>
    /// <returns></returns>
    public Task Prepare();

    /// <summary>
    /// This method will insert all the data into db 
    /// </summary>
    /// <returns>Request Id</returns>
    public Task<decimal> Issue();

    /// <summary>
    /// this method will create workflow
    /// </summary>
    /// <returns></returns>
    public Task InitiateWorkFlow();
}

