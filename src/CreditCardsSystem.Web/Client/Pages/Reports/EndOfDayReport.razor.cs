using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.BCDPromotions.Groups;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.RequestActivity;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CardDetails;
using CreditCardsSystem.Web.Client.Pages.Reports.Components;
using Kfh.Aurora.Blazor.Components.UI;
using Kfh.Aurora.Utilities;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Telerik.Blazor.Components;
using Telerik.DataSource.Extensions;


namespace CreditCardsSystem.Web.Client.Pages.Reports;



public partial class EndOfDayReport : IDisposable
{
    [Inject] public IEODReportsAppService ReportAppService { get; set; } = default!;
    [Inject] public IGroupsAppService GroupsAppService { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery]
    public string RequestId { get; set; }

    private CardDetailState cardDetailsState = new();
    private CardDetailsResponse? Card => cardDetailsState?.MyCard?.Data;

    private List<BreadcrumbItem> BreadcrumbItems { get; set; } = new();
    public OffCanvas FiltersDrawerRef { get; set; } = default!;
    public EditContext editContext { get; set; }
    public ValidationMessageStore messageStore { get; set; }

    public EODBranchReport? eodBranchReportRef { get; set; }

    public Dictionary<FilterPropertyEnum, string> filterSummary { get; set; } = new();
    private bool IsAllowedToView => IsAllowTo(Permissions.EndOfDayReport.View());
    private bool IsAllowedToPrint => IsAllowTo(Permissions.EndOfDayReport.Print());
    public enum FilterPropertyEnum
    {
        All,
        Type,
        BranchId,
        StaffId,
        ActivityId,
        Duration
    }
    protected override async Task OnInitializedAsync()
    {
        if (!IsAllowedToView)
        {
            return;
        }


        endOfDayFilter ??= new();
        editContext = new(endOfDayFilter);
        messageStore = new ValidationMessageStore(editContext);
    }

    public enum GenerateFor
    {
        ViewOnly = 1,
        PrintOnly
    }

    EODBranchReportDto eodBranchReportDto = default!;
    EODBranchReportDto filteredEodBranchReportDto = default!;
    DataItem<EODBranchReportDto> EODBranchReport { get; set; } = new();

    EODStaffReportDto? eodStaffReportDto = default!;
    EODStaffReportDto? filteredEodStaffReportDto = default!;
    DataItem<EODStaffReportDto> EODStaffReport { get; set; } = new();


    public TelerikDateRangePicker<DateTime?> DatePicker { get; set; } = null!;


    public class EndOfDayFilter
    {
        public string Type { get; set; } = "branch";
        public decimal? ActivityId { get; set; }
        public string? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? StaffId { get; set; }
    }

    public EndOfDayFilter endOfDayFilter { get; set; } = new();
    public IEnumerable<GroupAttributeLookupDto> Branches { get; set; }
    public IEnumerable<CFUActivityDto> CFUActivities { get; set; }


    async Task ReloadData()
    {
        await Task.CompletedTask;

    }

    async Task ReflectCardStatus(NewCardStatus newCardStatus)
    {
        await Task.CompletedTask;

    }


    private void LoadBreadCrums()
    {
        BreadcrumbItems =
        [
               new() { Text = CurrentState?.CustomerProfile?.FullName() ?? "", Url = "/customer-view?CivilId=" + CurrentState?.CurrentCivilId?.Encode() },
            new() { Text = DisplayCardNumber(Card?.CardNumberDto) }
        ];
    }



    protected override async Task OnParametersSetAsync()
    {
        if (!IsAllowTo(Permissions.EndOfDayReport.View()))
        {
            return;
        }

        await PrepareFilterInputs();
    }

    public async Task SearchEODReport(GenerateFor type)
    {
        if (!Validate())
        {
            //if (FiltersDrawerRef.IsOpen)
            //    await FiltersDrawerRef.ToggleAsync();
            return;
        }

        viewSubReport = false;
        subReports = null;

        await UpdateFilterSummary();

        if (FiltersDrawerRef.IsOpen)
            await FiltersDrawerRef.ToggleAsync();

        if (endOfDayFilter.Type.Equals("Branch", StringComparison.InvariantCultureIgnoreCase))
            await SearchEODBranchReport();

        if (endOfDayFilter.Type.Equals("Staff", StringComparison.InvariantCultureIgnoreCase))
            await SearchEODStaffReport();



        Notification.Hide();

        if (eodBranchReportRef is not null)
            await eodBranchReportRef.RefreshGrid();

    }


    async Task SearchEODStaffReport()
    {
        EODStaffReportRequest request = BindRequest().Adapt<EODStaffReportRequest>();
        EODStaffReport.Loading();
        var data = await ReportAppService.GetEODStaffReport(request);
        if (!data.IsSuccess)
        {

            Notification.Failure(data.Message);
            EODStaffReport.Error(new(data.Message));
            StateHasChanged();
            return;
        }
        eodStaffReportDto = data.Data!;
        await ApplyStaffFilter();
    }

    async Task SearchEODBranchReport()
    {
        EODBranchReportRequest request = BindRequest().Adapt<EODBranchReportRequest>();
        EODBranchReport.Loading();
        var data = await ReportAppService.GetEODBranchReport(request);
        if (!data.IsSuccess)
        {
            Notification.Failure(data.Message);
            EODBranchReport.Error(new(data.Message));
            StateHasChanged();
            return;
        }
        eodBranchReportDto = data.Data!;

        await ApplyBranchFilter();
    }


    async Task UpdateFilterSummary()
    {
        filterSummary.Clear();
        if (endOfDayFilter.StaffId is not null)
            filterSummary.Add(FilterPropertyEnum.StaffId, endOfDayFilter.StaffId);

        if (endOfDayFilter is { FromDate: not null, ToDate: not null })
            filterSummary.Add(FilterPropertyEnum.Duration, $"{endOfDayFilter.FromDate} -{endOfDayFilter.ToDate}");

        if (endOfDayFilter.ActivityId is not null)
            filterSummary.Add(FilterPropertyEnum.ActivityId, endOfDayFilter.ActivityId.ToString() ?? "");

        if (endOfDayFilter.BranchId is not null)
            filterSummary.Add(FilterPropertyEnum.BranchId, endOfDayFilter.BranchId);
    }

    private async Task ApplyStaffFilter(Dictionary<FilterPropertyEnum, string> filterSummary = null)
    {
        filteredEodStaffReportDto = eodStaffReportDto;
        EODStaffReport.SetData(filteredEodStaffReportDto!);
        StateHasChanged();
        Notification.Success();
    }
    private async Task ApplyBranchFilter(Dictionary<FilterPropertyEnum, string> filterSummary = null)
    {
        filteredEodBranchReportDto = eodBranchReportDto;

        var items = filteredEodBranchReportDto.Items;

        if (filterSummary is not null && items is { })
        {
            foreach (var filter in filterSummary)
            {
                items = filter.Key switch
                {
                    FilterPropertyEnum.BranchId => items.Where(x => x.Summaries.Any(x => x.BranchId == Convert.ToDecimal(filter.Value))),
                    FilterPropertyEnum.ActivityId => items.Where(x => x.ActivityId == Convert.ToDecimal(filter.Value)),
                    FilterPropertyEnum.StaffId => items.Where(x => x.Summaries.Any(x => x.TellerId == Convert.ToDecimal(filter.Value))),
                    _ => items
                };
            }
        }

        filteredEodBranchReportDto.Items = items;

        EODBranchReport.SetData(filteredEodBranchReportDto);

        Notification.Success();
    }

    private EODBaseRequest BindRequest()
    {
        return new EODBaseRequest()
        {
            TellerId = string.IsNullOrEmpty(endOfDayFilter.StaffId) ? null : Convert.ToInt64(endOfDayFilter.StaffId),
            FromCreationDate = endOfDayFilter.FromDate != DateTime.MinValue ? endOfDayFilter.FromDate : null,
            ToCreationDate = endOfDayFilter.ToDate != DateTime.MinValue ? endOfDayFilter.ToDate : null,
            CfuActivityId = endOfDayFilter.ActivityId is not null ? (int?)endOfDayFilter.ActivityId : null,
            BranchId = endOfDayFilter.BranchId is not null ? Convert.ToInt16(endOfDayFilter.BranchId) : 0
        };
    }
    private bool viewSubReport;
    EODSubReport? subReports { get; set; } = default!;
    public async Task ViewDetails(EODReportSummary activity)
    {
        var request = BindRequest().Adapt<EODSubReportRequest>();
        request.TellerId = (int?)activity.TellerId;
        request.CfuActivityId = (int)activity.CfuActivityId;
        subReports = (await ReportAppService.GetEODSubReport(request))?.Data;
        viewSubReport = true;
    }

    async Task PrintReport()
    {

        Notification.Loading("Downloading report...");

        var eFormResponse = endOfDayFilter.Type.Equals("Branch", StringComparison.InvariantCultureIgnoreCase) ?
                    await ReportAppService.PrintEODBranchReport(EODBranchReport.Data) :
                  await ReportAppService.PrintEODStaffReport(EODStaffReport.Data);

        if (!eFormResponse.IsSuccess)
        {
            Notification.Failure("Unable to generate Form, try again later from card list");
            return;
        }

        var formResponse = eFormResponse.Data!;
        var streamData = new MemoryStream(formResponse!.FileBytes!);
        using var streamRef = new DotNetStreamReference(stream: streamData);
        await Js.InvokeVoidAsync("downloadFileFromStream", $"{formResponse?.FileName}", streamRef);
        Notification.Success("Downloaded!");
    }


    #region FIlters

    async Task PrepareFilterInputs()
    {
        LoadBreadCrums();
        Branches = await GroupsAppService.GetLocations();
        Branches = Branches?.Where(x => x.Value != "0")?.OrderBy(x => x.Value) ?? Enumerable.Empty<GroupAttributeLookupDto>();
        CFUActivities = await ReportAppService.GetCFUActivities();

        endOfDayFilter.BranchId = Branches.OrderBy(x => x.Value).FirstOrDefault()?.Value;
        StateHasChanged();
        await Task.CompletedTask;
    }





    #endregion

    #region Validations

    public bool Validate()
    {
        messageStore.Clear();

        if (endOfDayFilter.Type == "staff")
        {
            if (string.IsNullOrEmpty(endOfDayFilter.StaffId))
            {
                editContext.AddAndNotifyFieldError(messageStore, () => endOfDayFilter.StaffId!, "Please enter staff id", true);
            }
        }


        return editContext.Validate();
    }

    public void Dispose()
    {
        Notification.Hide();
    }

    #endregion




}
