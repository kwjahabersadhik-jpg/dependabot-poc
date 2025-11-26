using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components
{
    public partial class ChangeStatusForm : IWorkflowMethods
    {

        [Inject] IChangeStatusAppService changeStatusAppService { get; set; } = null!;



        [CascadingParameter(Name = "Reload")]
        public EventCallback ReloadCardInfo { get; set; }



        [Parameter]

        public ListItem[] RequestStatuses { get; set; } = null!;
        public int? SelectedStatus { get; set; }



        private bool isAllowToChange = true;
        private string defaultText = "Loading..";


        protected override async Task OnInitializedAsync()
        {

            //Notification.Loading($"Loading data...");

            await BindDropDownItems();

            Notification.Hide();


        }

        private async Task LoadValidStatusesBasedOnCurrentStatus()
        {
            var requestStatusResult = await _requestAppService.GetAllRequestStatus();
            if (!requestStatusResult.IsSuccess || !requestStatusResult.Data.AnyWithNull())
            {
                defaultText = "N/A";
                return;
            }

            var allStatus = requestStatusResult.Data!.Select(x => new ListItem() { Text = x.EnglishDescription, Value = (int)x.StatusId });

            if (SelectedCard.CardStatus == CreditCardStatus.Approved)
                allStatus = allStatus.Where(x => x.Value != (int)CreditCardStatus.Approved && x.Value != (int)CreditCardStatus.Pending);

            if (SelectedCard.CardStatus is CreditCardStatus.AccountBoardingStarted or CreditCardStatus.CardUpgradeStarted)
                allStatus = Enumerable.Empty<ListItem>();

            if (SelectedCard.CardType == ConfigurationBase.AlOsraPrimaryCardTypeId && SelectedCard.SupplementaryCardCount > 0)
                allStatus = Enumerable.Empty<ListItem>();

            RequestStatuses = allStatus.ToArray();
            isAllowToChange = RequestStatuses.Any();
            defaultText = isAllowToChange ? "Select Status" : "N/A";

            await ReadyForAction.InvokeAsync(isAllowToChange);
        }

        private async Task BindDropDownItems()
        {
            await LoadValidStatusesBasedOnCurrentStatus();
        }


        private void OnChangeRequestHandler()
        {
            ReloadCardInfo.InvokeAsync();
        }



        public Task ProcessAction(ActionType actionType, string ReasonForRejection = "")
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
        {
            
            if (!await IsValidForm())
            {
                Notification.Failure(GlobalResources.InvalidInput);
                return false;
            }

            await Listen.NotifyStatus("Changing status in-progress");
            var response = await changeStatusAppService.ChangeStatus(new()
            {
                RequestID = SelectedCard.RequestId,
                NewStatus = (CreditCardStatus)(SelectedStatus ?? -1)
            });

            await NotifyStatus(response.IsSuccess, response.IsSuccess ? "Success" : response.Message);

            if (response.IsSuccess)
                await ReloadCardInfo.InvokeAsync();

            return response.IsSuccess;

            async Task<bool> IsValidForm()
            {
                if (SelectedStatus is null)
                    return await Task.FromResult(false);

                return await Task.FromResult(SelectedStatus != (int)SelectedCard.CardStatus);
            }

        }

        public Task PrintApplication()
        {
            throw new NotImplementedException();
        }

        async Task NotifyStatus(bool isSuccess = false, string message = "", bool isStarted = false)
        {
            if (isStarted)
            {
                await Listen.InvokeAsync(new(ProcessStatus: DataStatus.Loading, CloseDialog: false));
                return;
            }

            ActionStatus actionStatus = new(IsSuccess: isSuccess, Message: message, ProcessStatus: isSuccess ? DataStatus.Success : DataStatus.Error);
            await Listen.InvokeAsync(actionStatus);
        }

        public Task Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
