using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CoBrand;
using CreditCardsSystem.Domain.Shared.Models.Membership;
using Kfh.Aurora.Blazor.Components.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace CreditCardsSystem.Web.Client.Pages.CoBrand;



public partial class MembershipDeleteRequest : IDisposable
{
    [Inject] public IMemberShipAppService memberShipAppService { get; set; } = default!;
    [Inject] public ILookupAppService lookupAppService { get; set; } = default!;



    public MemberShipDeleteRequestFilter SearchFilter { get; set; }
    DataItem<IEnumerable<MembershipDeleteRequestDto>> MemberShipDeleteRequest { get; set; } = new();
    private IEnumerable<MembershipDeleteRequestDto> SelectedRequests { get; set; } = Enumerable.Empty<MembershipDeleteRequestDto>();
    public IEnumerable<CompanyDto> Companies { get; set; }
    private List<BreadcrumbItem> BreadcrumbItems { get; set; } = new();
    public OffCanvas FiltersDrawerRef { get; set; } = default!;
    public EditContext editContext { get; set; }
    public ValidationMessageStore messageStore { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!IsAllowTo(Permissions.MemberShipDeleteRequest.EnigmaApprove()))
        {
            Notification.Failure(GlobalResources.NotAuthorized);
            return;
        }

        SearchFilter ??= new();
        editContext = new(SearchFilter);
        messageStore = new ValidationMessageStore(editContext);
        await LoadCompanies();
    }



    async Task LoadCompanies()
    {
        var response = await lookupAppService.GetAllCompanies();
        if (response.IsSuccessWithData)
            Companies = response.Data?.DistinctBy(x => x.CompanyName).ToList() ?? [];

    }


    async Task ReloadData()
    {
        await Task.CompletedTask;

    }

    async Task ReflectCardStatus(NewCardStatus newCardStatus)
    {
        await Task.CompletedTask;

    }



    public async Task Search()
    {
        if (!Validate())
        {
            return;
        }

        MemberShipDeleteRequest.Loading();

        SearchFilter.ClubMembershipId = string.IsNullOrEmpty(SearchFilter.ClubMembershipId) ? null : SearchFilter.ClubMembershipId;
        SearchFilter.CivilId = string.IsNullOrEmpty(SearchFilter.CivilId) ? null : SearchFilter.CivilId;
        SearchFilter.CompanyId = (SearchFilter.CompanyId == null || SearchFilter.CompanyId <= 0) ? null : SearchFilter.CompanyId;

        var memberResponse = await memberShipAppService.GetMemberShipDeleteRequests(SearchFilter);

        if (memberResponse.IsSuccess)
            MemberShipDeleteRequest.SetData(memberResponse.Data);
        else
            MemberShipDeleteRequest.Error(new(memberResponse.Message));

        if (FiltersDrawerRef.IsOpen)
            await FiltersDrawerRef.ToggleAsync();


    }

    bool updateInProgress { get; set; }
    public async Task Submit()
    {
        updateInProgress = true;
        var updateResponse = await memberShipAppService.UpdateMemberShipDeleteRequests(new()
        {
            Items = SelectedRequests.Select(x => new UpdateMembershipDeleteDto()
            {
                ApproverReason = x.ApproverReason,
                Id = x.Id,
                RequestId = x.RequestId,
                Status = x.Status
            }).ToList()
        });
        updateInProgress = false;
        if (!updateResponse.IsSuccess)
        {
            Notification.Failure(updateResponse.Message);
            return;
        }


        if (updateResponse.Data!.FailedItems.Count != 0)
            Notification.Success("Partially success!");
        else
            Notification.Success("Done!");

        await Search();
    }











    #region FIlters





    #endregion

    #region Validations

    public bool Validate()
    {
        messageStore.Clear();




        return editContext.Validate();
    }

    public void Dispose()
    {
        //Notification.Hide();
    }

    #endregion




}
