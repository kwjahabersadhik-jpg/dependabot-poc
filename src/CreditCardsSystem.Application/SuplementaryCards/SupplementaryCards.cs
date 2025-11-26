using CreditCardsSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.SuplementaryCards;

public class SupplementaryCards
{
    private readonly string primaryCardRequestId;
    private readonly string primaryCardNumber;
    private readonly FdrDBContext fdrDBContext;

    public SupplementaryCards(decimal primaryCardRequestId, string primaryCardNumber, FdrDBContext fdrDBContext)
    {
        this.primaryCardRequestId = Convert.ToString(primaryCardRequestId);
        this.primaryCardNumber = primaryCardNumber;
        this.fdrDBContext = fdrDBContext;
    }

    public async Task<int> GetSupplementaryCounts()
    {

        var supplementaryCardsCount = await fdrDBContext.RequestParameters.AsNoTracking().Where(x =>
        x.Parameter == "PRIMARY_CARD_REQUEST_ID" && x.Value == primaryCardRequestId ||
        x.Parameter == "PRIMARY_CARD_NO" && x.Value == primaryCardNumber)
        .Select(x => x.ReqId).Distinct().CountAsync();

        return supplementaryCardsCount;
    }
}
