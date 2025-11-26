using Bloc.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Domain.Shared.Interfaces.Workflow;
using CreditCardsSystem.Web.Client.Components;
using Kfh.Aurora.Common.Components.UI.Settings.Cubits;
using Kfh.Aurora.Common.Components.UI.Settings.States;
using Kfh.Aurora.Workflow.Dto;
using Microsoft.AspNetCore.Components;
using Telerik.DataSource.Extensions;
using Collateral = CreditCardsSystem.Domain.Enums.Collateral;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components
{
    public partial class ApprovalForm : IWorkflowMethods
    {

        [Inject] ICardRequestApprovalAppService CardRequestApprovalAppService { get; set; } = null!;
        [Inject] IRequestAppService RequestAppService { get; set; } = null!;
        [Inject] IAddressAppService AddressService { get; set; } = null!;
        [Inject] ICardDetailsAppService CardDetailsAppService { get; set; } = null!;
        [Inject] IWorkflowAppService WorkflowAppService { get; set; } = null!;
        [Inject] ICustomerProfileAppService CustomerProfileAppService { get; set; } = null!;
        [Inject] BlocBuilder<UserSettingsCubit, UserSettingsState> userSettingsBuilder { get; set; }
        #region Variables
        CardDefinitionDto? CardDefinition { get; set; }
        ProcessCardRequest Model { get; set; }
        Collateral? collateral { get; set; } = Collateral.NONE;

        AccountDetailsDto? SelectedCardAccount { get; set; }
        bool IsTayseerSalaryException { get; set; }
        bool IsSupplementaryCard { get; set; }
        TayseerCardFields TayseerCardFieldRef { get; set; } = default!;
        BCDForm BCDFormRef { get; set; } = default!;

        DataItem<List<SupplementaryCardDetail>> Supplementary { get; set; } = new();
        IEnumerable<SupplementaryCardDetail> SelectedItems { get; set; } = [];
        private List<SupplementaryCardDetail> ExistingSupplementries { get; set; } = new();
        private decimal SupplementaryMaxApproveLimit { get; set; }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            //Notification.Loading($"Loading data...");
            await BindTaskDetail();
            Notification.Hide();
        }
        private async Task BindTaskDetail()
        {
            //var rd = (await RequestAppService.GetRequestDetail(RequestActivity.RequestId))?.Data;
            //SelectedCard = base.SelectedCard;// (await CardDetailsAppService.GetCardInfo(RequestActivity.RequestId))?.Data ?? new();

            if (SelectedCard.CardStatus is CreditCardStatus.Active or CreditCardStatus.Approved)
            {
                Notification.Failure(GlobalResources.InvalidTask);

                //adding comments to mention reason for auto task completion
                var Comments = (await WorkflowAppService.GetComments(TaskDetail.InstanceId))?.Result;
                Comments?.Add(new()
                {
                    Comment = new CommentResult()
                    {
                        Value = "Approved from SSO",
                        CommentBy = AuthManager.GetUser()?.KfhId ?? "",
                        CreatedAt = DateTime.Now
                    }
                });

                await WorkflowAppService.CompleteTask(new CompleteTaskRequest()
                {
                    TaskId = TaskDetail.Id,
                    Assignee = TaskDetail?.Assignee ?? "",
                    Comments = Comments?.Select(x => x.Comment.Value).ToList(),
                    InstanceId = TaskDetail.InstanceId,
                    Payload = TaskDetail.Payload,
                    Status = ActionType.Approved.ToString(),
                });

                NavigateTo("/cc-tasks");
                return;
            }

            SelectedCard.BillingAddress = (await AddressService.GetRecentBillingAddress(requestId: RequestActivity.RequestId))?.Data ?? new();
            await InvokeAsync(StateHasChanged);

            if (Enum.TryParse(SelectedCard.Collateral, out Collateral _collateral))
                collateral = _collateral;

            IsTayseerSalaryException = SelectedCard.ProductType is ProductTypes.Tayseer && (collateral is Domain.Enums.Collateral.AGAINST_SALARY or Domain.Enums.Collateral.EXCEPTION);
            IsSupplementaryCard = SelectedCard?.Parameters?.IsSupplementaryOrPrimaryChargeCard?.ToUpper()?.Equals("S", StringComparison.InvariantCultureIgnoreCase) == true;

            Model = new()
            {
                ActionType = ActionType.None,
                RequestActivityId = RequestActivity.RequestActivityId,
                RequestId = RequestActivity.RequestId,
                TaskId = TaskDetail?.Id,
                WorkFlowInstanceId = TaskDetail?.InstanceId,
                ApprovedLimit = SelectedCard.ApproveLimit,

                BCDParameters = new()
                {
                    NewStatus = CreditCardStatus.Approved,
                    CardNumber = SelectedCard.CardNumberDto ?? "",
                    FdrAccountNumber = SelectedCard.FdrAccountNumber ?? "",
                    OldCardNumber = SelectedCard.Parameters?.OldCardNumberEncrypted ?? "",
                },
                CreditCheckModel = new() { EntryType = (int)EntryType.CreditChecking, RequestId = SelectedCard.RequestId }
            };


            if (collateral is not Collateral.EXCEPTION)// && SelectedCard.BranchId != CurrentState.BranchId)
            {
                _ = int.TryParse(userSettingsBuilder.State.BranchId, out int _userBranchId);
                if (SelectedCard.BranchId != _userBranchId)
                {
                    //_userPreferencesClient.GetUserPreference(AuthManager.GetUser().KfhId, "");
                    Notification.Failure("Sorry you cannot appove or reject the card which is issued in different branch.");
                    await Listen.InvokeAsync(new Domain.Models.ActionStatus() { IsSuccess = false, IsAccessDenied = true, Message = "Sorry you cannot appove or reject the card which is issued in different branch." });
                    return;
                }
            }


            if (Model is not null && !IsAllowTo(Permissions.AccountBoardingRequest.Edit()) && SelectedCard is { CardStatus: CreditCardStatus.CardUpgradeStarted or CreditCardStatus.AccountBoardingStarted })
            {
                Notification.Failure("You do not have the necessary permission!");
                await ReadyForAction.InvokeAsync(false);
                return;
            }

            List<Task> taskList = [BindCustomerProfileData(), GetCardDefinition()];

            if (SelectedCard.IsPrimaryCard)
                taskList.Add(GetSupplementaryCards());

            if (IsSupplementaryCard)
            {
                taskList.Add(LoadExistingSupplementaryCards());
            }

            await Task.WhenAll(taskList);

            await InvokeAsync(StateHasChanged);
        }
        private async Task LoadExistingSupplementaryCards()
        {
            // Get all Supplementary cards for primary civil ID
            var response = await CardDetailsAppService.GetSupplementaryCardsByRequestId(SelectedCard!.Parameters!.PrimaryCardRequestId.ToDecimal());
            var primaryCard = await CardDetailsAppService.GetCardInfoMinimal(SelectedCard!.Parameters!.PrimaryCardRequestId.ToDecimal());

            if (response.IsSuccessWithData)
            {
                ExistingSupplementries = response.Data ?? new();
            }

            decimal existingCardsLimit = ExistingSupplementries.Where(x => x.CardStatus != CreditCardStatus.Closed).Sum(x => x.CardData?.ApprovedLimit ?? 0);
            SupplementaryMaxApproveLimit = (primaryCard.Data?.ApprovedLimit ?? 0) - (existingCardsLimit - SelectedCard.RequestedLimit);
        }
        async Task GetCardDefinition()
        {
            CardDefinition = await CardDetailsAppService.GetCardWithExtension(SelectedCard!.CardType!);
        }

        public async Task ProcessAction(ActionType actionType, string ReasonForRejection = "")
        {
            if (IsTayseerSalaryException && SelectedCard is { CardStatus: not (CreditCardStatus.Pending or CreditCardStatus.Approved) })
            {
                await TayseerCardFieldRef.BindCreditCheckModel();
                Model.CreditCheckModel = TayseerCardFieldRef.CreditCheckModel;
            }

            Model.ActionType = actionType;
            Model.ReasonForRejection = ReasonForRejection;

            var processResponse = await CardRequestApprovalAppService.ProcessCardRequest(Model);
            Notification.Hide();

            if (processResponse.IsSuccess)
            {
                await Listen.NotifyStatus(processResponse);
                return;
            }

            if (processResponse.Message.StartsWith(GlobalResources.AssignToBCDTeam, StringComparison.InvariantCultureIgnoreCase))
            {
                await Listen.NotifyStatus(GlobalResources.AssignToBCDTeam);
                await Task.Delay(1000);
                Notification.Hide();
                NavigateTo("/cc-tasks");
            }
            else if (processResponse.Message.StartsWith(GlobalResources.RedirectToTaskListForBCDUser, StringComparison.InvariantCultureIgnoreCase))
            {
                processResponse.Message = GlobalResources.RedirectToTaskListForBCDUser;
                await BindCustomerProfileData();
                await Listen.NotifyStatus(processResponse);
                await Task.Delay(1000);
                Notification.Hide();
                NavigateTo("/cc-tasks");
            }
            else
            {
                if (BCDFormRef is not null)
                    await Listen.HandleApiResponse(processResponse, BCDFormRef.EditFormContext, BCDFormRef.MessageStore);
                else
                    await Listen.NotifyStatus(processResponse);
            }
        }
        public async Task Cancel()
        {
            var response = await RequestAppService.CancelRequest(RequestActivity.RequestId);
            await Listen.NotifyStatus(response);
        }


        async Task OnChangeRequestLimit(string newAmount)
        {
            if (decimal.TryParse(newAmount, out decimal _newAmount))
            {

                bool invalidLimit = IsSupplementaryCard ? SupplementaryMaxApproveLimit < _newAmount : (CardDefinition!.MaxLimit < _newAmount || CardDefinition!.MinLimit > _newAmount);

                if (invalidLimit)
                {
                    Notification.Failure("Invalid Approve Limit!");
                    return;
                }

                Model.ApprovedLimit = _newAmount;
            }

            await Task.CompletedTask;
        }
        async Task GetSupplementaryCards()
        {
            Supplementary.Loading();
            var nonClosedSupplementary = (await CardDetailsAppService.GetSupplementaryCardsByRequestId(SelectedCard.RequestId))?.Data?.Where(x => x.CardStatus is not CreditCardStatus.Closed)?.ToList();
            Supplementary.SetData(nonClosedSupplementary);
            Notification.Hide();
        }
        async Task BindCustomerProfileData()
        {
            var response = await CustomerProfileAppService.GetCustomerProfileFromFdRlocalDb(RequestActivity.CivilId);
            Notification.Hide();
            if (Model.BCDParameters is not null)
            {
                Model.BCDParameters.CustomerNumber = response?.Data?.CustomerNo ?? "";
            }
            Model.DateOfBirth = response?.Data?.Birth ?? new();
        }

        public Task PrintApplication() => throw new NotImplementedException();
        public Task<bool> SubmitRequest(CancellationToken? cancellationToken = default) => throw new NotImplementedException();
    }

}