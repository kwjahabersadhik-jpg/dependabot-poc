using CreditCardsSystem.Data;
using CreditCardsSystem.Domain;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Application.Report;

public class ReplacementTrackingReportAppService(FdrDBContext fdrDbContext,
                                                 IReportAppService reportAppService,
                                                 IRequestAppService requestAppService,
                                                 IAuthManager authManager,
                                                 ICustomerProfileCommonApi customerProfileCommonApi)
    : BaseApiResponse, IReplacementTrackingReportAppService, IAppService
{

    #region Variables

    private readonly FdrDBContext fdrDbContext = fdrDbContext;
    private readonly IReportAppService reportAppService = reportAppService;
    private readonly IAuthManager _authManager = authManager;
    private readonly ICustomerProfileCommonApi customerProfileCommonApi = customerProfileCommonApi;

    [HttpPost]
    public async Task<ApiResponseModel<ReplacementTrackingReportData>> GetReport([FromBody] ReplacementTrackingReportFilter filter)
    {
        await ValidateBiometricStatus(filter.RequestId);

        if (!_authManager.HasPermission(Permissions.ReplacementTrackingReport.View()))
            return Failure<ReplacementTrackingReportData>(GlobalResources.NotAuthorized);

        await filter.ModelValidationAsync();

        bool canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());


        var reportData = await (from r in fdrDbContext.Requests
                                join p in fdrDbContext.Profiles on r.CivilId equals p.CivilId
                                where r.RequestId == filter.RequestId
                                select new ReplacementTrackingReportData()
                                {
                                    CivilId = r.CivilId,
                                    FdAcctNo = r.FdAcctNo ?? "",
                                    CardNo = canViewCardNumber ? r.CardNo ?? "" : r.CardNo.Masked(6, 6),
                                    HolderName = p.HolderName
                                }).FirstOrDefaultAsync();

        if (reportData is null)
            return Failure<ReplacementTrackingReportData>(message: $"profile not found for this request card holder!");

        var reportDetails = await (from r in fdrDbContext.Requests
                                   join ra in fdrDbContext.RequestActivities on r.RequestId equals ra.RequestId
                                   let oldCardNumber = fdrDbContext.RequestActivityDetails.FirstOrDefault(rad => rad.RequestActivityId == ra.RequestActivityId && rad.Paramter == "credit_card_no")
                                   where r.RequestId == filter.RequestId && ra.CfuActivityId == (int)CFUActivity.Replace_On_Lost_Or_Stolen
                                   select new ReplacementTrackingReportDetail
                                   {
                                       AcctNo = r.AcctNo ?? "",
                                       BranchId = ra.BranchId,
                                       BranchName = ra.BranchName ?? "",
                                       OldCardNumber = canViewCardNumber ? oldCardNumber.Value ?? "" : oldCardNumber.Value.Masked(6, 6),
                                       Mobile = r.Mobile,
                                       TellerId = ra.TellerId,
                                       CreationDate = ra.CreationDate,
                                   }).ToListAsync();

        reportData.Details = reportDetails;

        return Success(reportData);
    }


    [HttpPost]
    public async Task<ApiResponseModel<EFormResponse>> PrintReport([FromBody] ReplacementTrackingReportFilter filter)
    {
        await ValidateBiometricStatus(filter.RequestId);

        if (!_authManager.HasPermission(Permissions.ReplacementTrackingReport.Print()))
            return Failure<EFormResponse>(GlobalResources.NotAuthorized);

        var reportData = await GetReport(filter);
        return await reportAppService.PrintDynamicReport<ReplacementTrackingReportData>(reportData?.Data, filter.FileExtension);

    }




    private async Task ValidateBiometricStatus(long requestId)
    {
        var request = fdrDbContext.Requests.AsNoTracking().FirstOrDefault(x => x.RequestId == requestId) ?? throw new ApiException(message: "Invalid request Id ");
        var bioStatus = await customerProfileCommonApi.GetBiometricStatus(request!.CivilId);
        if (bioStatus.ShouldStop)
            throw new ApiException(message: GlobalResources.BioMetricRestriction);
    }





    #endregion
}

