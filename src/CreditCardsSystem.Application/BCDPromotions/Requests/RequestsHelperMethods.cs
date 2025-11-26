using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace CreditCardsSystem.Application.BCDPromotions.Requests;

public class RequestsHelperMethods : IRequestsHelperMethods
{
    private readonly string _promoConnectionString;

    public RequestsHelperMethods(IConfiguration configuration)
    {
        _promoConnectionString = configuration.GetConnectionString("FdrOracleConnection")!;
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<long> GetNewRequestId(string dbSequence)
    {
        var query = string.Empty;
        await using var conn = new OracleConnection(_promoConnectionString);

        if (dbSequence == "promo.CARDTYPE_ELIGIBILITY_MATIX")
            query = string.Format($" select NVL(max(ID),0)+1 as NewID from {dbSequence}");

        else
            query = @"select " + dbSequence + ".nextval as ID from sys.dual";


        var result = (await conn.QueryAsync<long>(query)).ToList();
        var id = result.AsList()[0];
        conn.Close();
        return id;
    }

}