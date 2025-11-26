using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.DirectDebit;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.DirectDebit;

public class DirectDebitAppService(FdrDBContext fdrDBContext) : BaseApiResponse, IDirectDebitAppService, IAppService
{
    private readonly FdrDBContext _fdrDBContext = fdrDBContext;

    [HttpGet]
    public async Task<ApiResponseModel<DirectDebitOptionDto>> Get()
    {
        var result = await _fdrDBContext.DirectdebitGenerationoptions
            .AsNoTracking()
            .Where(x => x.GenerationStatus == "P")
            .ProjectToType<DirectDebitOptionDto>().FirstOrDefaultAsync();
        return Success(result);
    }

    [HttpPost]
    public async Task<ApiResponseModel<string>> Create([FromBody] DirectDebitOptionDto request)
    {

        await _fdrDBContext.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO VPBCD.DIRECTDEBIT_GENERATIONOPTIONS
                (ENTRY_DATE, GENERATION_OPTIONS, IS_FILE_LOAD_REQ, GENERATION_STATUS, IS_REVERSAL_PAYMENT)
                VALUES({DateTime.Now}, {request.GenerationOptions}, 
               {request.IsFileLoadReq}, {request.GenerationStatus}, {request.IsReversalPayment})");

        return Success<string>("Done!");
    }
}
