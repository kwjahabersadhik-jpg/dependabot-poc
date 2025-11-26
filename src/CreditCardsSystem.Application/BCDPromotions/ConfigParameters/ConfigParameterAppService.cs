using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.ConfigParameter;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.BCDPromotions.ConfigParameters;

public class ConfigParameterAppService(FdrDBContext fdrDbContext) : IConfigParameterAppService, IAppService
{
    private readonly FdrDBContext _fdrDbContext = fdrDbContext;

    [HttpGet]
    public async Task<ApiResponseModel<List<ConfigParameterDto>>> Get()
    {
        var parameters = (await _fdrDbContext.ConfigParameters.AsNoTracking().ToListAsync()).Adapt<List<ConfigParameterDto>>();
        return new ApiResponseModel<List<ConfigParameterDto>>().Success(parameters);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<ConfigParameterDto>>> GetByStartsWith(string key)
    {
        var parameters = (await _fdrDbContext.ConfigParameters.AsNoTracking().Where(x => x.ParamName.StartsWith(key))
            .ToListAsync()).Adapt<List<ConfigParameterDto>>();
        return new ApiResponseModel<List<ConfigParameterDto>>().Success(parameters);
    }



    [HttpGet]
    public async Task<ApiResponseModel<List<ConfigParameterDto>>> GetByKey(string key)
    {
        var parameters = (await _fdrDbContext.ConfigParameters.AsNoTracking().Where(x => x.ParamName == key)
            .ToListAsync()).Adapt<List<ConfigParameterDto>>();
        return new ApiResponseModel<List<ConfigParameterDto>>().Success(parameters);
    }


    [HttpPost]
    public async Task<ApiResponseModel<ConfigParameterDto>> Update([FromBody] ConfigParameterDto parameterDto)
    {
        var isExist = await _fdrDbContext.ConfigParameters
            .AnyAsync(p => p.ParamName.Trim().ToLower() == parameterDto.ParamName.Trim().ToLower() && p.Id != parameterDto.Id);

        if (isExist)
            return new ApiResponseModel<ConfigParameterDto>().Fail("parameter name already exists");

        var parameter = await _fdrDbContext.ConfigParameters.FirstOrDefaultAsync();

        parameter!.Id = parameterDto.Id;
        parameter.ParamType = parameterDto.ParamType;
        parameter.ParamName = parameterDto.ParamName;
        parameter.ParamValue = parameterDto.ParamValue;
        parameter.ParamDesc = parameterDto.ParamDesc;

        await _fdrDbContext.SaveChangesAsync();
        return new ApiResponseModel<ConfigParameterDto>().Success(parameterDto);

    }

}