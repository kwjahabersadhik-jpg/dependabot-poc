using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardUpdateServiceReference;
using Kfh.Aurora.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Web.Mvc;
using HttpGetAttribute = System.Web.Mvc.HttpGetAttribute;
using HttpPostAttribute = System.Web.Mvc.HttpPostAttribute;

namespace CreditCardsSystem.Application.AddressService;

public class AddressAppService : BaseApiResponse, IAddressAppService, IAppService
{
    private readonly FdrDBContext _fdrDBContext;
    private readonly CreditCardUpdateServicesServiceClient _updateServicesServiceClient;
    public AddressAppService(FdrDBContext fdrDBContext, IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options)
    {
        _fdrDBContext = fdrDBContext;
        _updateServicesServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
    }
    // test vulnerable code cwe 20
    public IActionResult Authenticate(string user)
    {
        string query = "SELECT * FROM Users WHERE Username = '" + user + "'";

        // an attacker can bypass authentication by setting user to this special value
        // user = "' or 1=1 or ''='";

        var userExists = false;
        if (_fdrDBContext.Database.ExecuteSqlRaw(query) > 0) // Noncompliant
        {
            userExists = true;
        }

        var data = new JsonResult();
        var tt = new JsonResult { Data = data.Data };
        return (IActionResult)tt;
    }

    //[HttpGet]
    //public async Task<ApiResponseModel<BillingAddressModel>> GetBillingAddressByRequestId(decimal requestId)
    //{

    //    var recentBillingAddress = (await _fdrDBContext.Requests.Where(x => x.RequestId == requestId)
    //        .OrderByDescending(x => x.ReqDate)
    //        .Select(x => new BillingAddressModel()
    //        {
    //            City = x.City,
    //            Mobile = x.Mobile,
    //            PostOfficeBoxNumber = x.PostOfficeBoxNumber,
    //            Street = x.Street,
    //            HomePhone = x.HomePhone,
    //            WorkPhone = x.WorkPhone,
    //            PostalCode = x.PostalCode
    //        })
    //        .FirstOrDefaultAsync()) ?? throw new ApiException(message: "Invalid RequestId");


    //    if (recentBillingAddress is not null)
    //    {
    //        string street = recentBillingAddress.Street;

    //        int blockIndex = street.IndexOf("Blk");
    //        int streetIndex = street.IndexOf("st");
    //        int jdaIndex = street.IndexOf("Jda");
    //        int houseIndex = street.IndexOf("House");

    //        if (blockIndex > 0)
    //            recentBillingAddress.Block = street.Substring(blockIndex += 3, streetIndex - 3).Trim();

    //        if (streetIndex > 0)
    //            recentBillingAddress.StreetNo_NM = street.Substring(streetIndex += 2, jdaIndex - streetIndex).Trim();

    //        if (jdaIndex > 0)
    //            recentBillingAddress.Jada = street.Substring(jdaIndex += 3, houseIndex - jdaIndex).Trim();

    //        if (houseIndex > 0)
    //            recentBillingAddress.House = street[(houseIndex += 5)..].Trim();
    //    }

    //    return Success(recentBillingAddress!);
    //}


    [HttpGet]
    public async Task<ApiResponseModel<BillingAddressModel>> GetRecentBillingAddress(string? civilId, decimal? requestId)
    {
        var recentBillingAddress = await _fdrDBContext.Requests.Where(x => x.CivilId == civilId || x.RequestId == requestId)
            .OrderByDescending(x => x.ReqDate)
            .Select(x => new BillingAddressModel()
            {
                City = x.City,
                Mobile = x.Mobile,
                PostOfficeBoxNumber = x.PostOfficeBoxNumber,
                Street = x.Street,
                HomePhone = x.HomePhone,
                WorkPhone = x.WorkPhone,
                PostalCode = x.PostalCode
            })
            .FirstOrDefaultAsync();


        if (recentBillingAddress is not null)
        {
            string street = recentBillingAddress.Street;

            int blockIndex = street.IndexOf("Blk");
            int streetIndex = street.IndexOf("st");
            int jdaIndex = street.IndexOf("Jda");
            int houseIndex = street.IndexOf("House");

            if (blockIndex > 0)
                recentBillingAddress.Block = street.Substring(blockIndex += 3, streetIndex - 3).Trim();

            if (streetIndex > 0)
                recentBillingAddress.StreetNo_NM = street.Substring(streetIndex += 2, jdaIndex - streetIndex).Trim();

            if (jdaIndex > 0)
                recentBillingAddress.Jada = street.Substring(jdaIndex += 3, houseIndex - jdaIndex).Trim();

            if (houseIndex > 0)
                recentBillingAddress.House = street[(houseIndex += 5)..].Trim();
        }

        return Success(recentBillingAddress ?? new());
    }



    [HttpPost]
    public async Task<ApiResponseModel<BillingAddressModel>> UpdateBillingAddress(UpdateBillingAddressRequest request)
    {
        var address = (await _updateServicesServiceClient.updateBillingAddressAsync(new()
        {
            billingAddress = new()
            {
                cardNo = request.CardNumber,
                city = request.BillingAddress.City,
                continuation_1 = request.BillingAddress.AddressLine1,
                continuation_2 = request.BillingAddress.AddressLine2,
                street = request.BillingAddress.Street,

                postCode = request.BillingAddress.PostalCode ?? 0,
                POBox = request.BillingAddress.PostOfficeBoxNumber ?? 0,
                homePhone = request.BillingAddress.HomePhone ?? 0,
                mobile = request.BillingAddress.Mobile ?? 0,
                workPhone = request.BillingAddress.WorkPhone ?? 0
            }
        }))?.updateBillingAddressResult ?? throw new ApiException(message: "Unable to update billing address");

        if (!address.isSuccessful)
            return Failure<BillingAddressModel>(message: address.description);

        return Success<BillingAddressModel>(new());
    }

}
