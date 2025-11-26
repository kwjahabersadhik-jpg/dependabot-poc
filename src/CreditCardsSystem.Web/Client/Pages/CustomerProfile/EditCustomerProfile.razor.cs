using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Customer;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CreditCardsSystem.Web.Client.Pages.CustomerProfile
{
    public partial class EditCustomerProfile
    {
        [Inject] public required ICustomerProfileAppService GenericCustomerProfileAppService { get; set; }
        [Inject] public required ICustomerProfileAppService CustomerProfileAppService { get; set; }

        public bool ValidSubmit { get; set; } = false;
        public bool IsValidProfileToCreate { get; set; } = true;

        [Parameter]
        [SupplyParameterFromQuery(Name = "CivilId")]
        public required string CivilId
        {
            get { return civilId.Decode()!; }
            set { civilId = value; }
        }

        private string civilId;

        [Parameter]
        [SupplyParameterFromQuery]
        public string? ReturnUrl { get; set; }

        [Parameter]
        public bool ShowButtons { get; set; } = true;



        [Parameter]
        public bool IsSupplementaryCustomer { get; set; } = false;

        [Parameter]
        public GenericCustomerProfileDto? CustomerProfileFromPhenix { get; set; }

        public EditContext? editContext { get; set; }

        public string? Message { get; set; }

        public ProfileDto? Model { get; set; } = new();
        private CustomerLookupData? lookups { get; set; }

        GenericCustomerProfileDto? profile { get; set; }


        void GoHome() => NavigationManager.NavigateTo("/");
        public DataStatus FormDataStatus { get; set; } = new();

        private record TitleRecord(string Text, int Value);

        private List<TitleRecord> titles = new() { new("Mr", 1), new("Ms", 2) };
        protected override async Task OnInitializedAsync()
        {


            await GetCustomerProfile();

        }

        async Task GetCustomerProfile()
        {
            try
            {
                State ??= new();
                //var localProfileResponse = await CustomerProfileAppService.GetCustomerProfileFromFdRlocalDb(CivilId);
                //if (localProfileResponse.IsSuccess)
                //{
                //    Message = "Profile found in FDR";
                //    IsValidProfileToCreate = false;
                //    return;
                //}

                FormDataStatus = DataStatus.Loading;

                if (IsSupplementaryCustomer)
                {
                    profile = CustomerProfileFromPhenix;
                    await BindCustomerProfileFromPhenix();
                    FormDataStatus = DataStatus.Success;
                    return;
                }

                if (CurrentState.GenericCustomerProfile is null || (CurrentState.GenericCustomerProfile is not null && CurrentState.GenericCustomerProfile.CivilId != CivilId))
                {


                    State.GenericCustomerProfile.Loading();
                    //Loading Primary card profile
                    var customerProfile = await GenericCustomerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = CivilId });
                    //if (!customerProfile.IsSuccess || customerProfile.Data?.RimNo == 0)
                    if (customerProfile is { IsSuccess: false, Data: null })
                    {
                        Notification.Failure(Message);
                        Message = "Profile not found in Phenix";
                        State.GenericCustomerProfile.Error(new(Message));
                        FormDataStatus = DataStatus.Error;
                        StateHasChanged();
                        return;
                    }
                    State.GenericCustomerProfile.SetData(customerProfile?.Data!);
                    CurrentState.GenericCustomerProfile = State.GenericCustomerProfile.Data!;
                }
                else
                {
                    State.GenericCustomerProfile.SetData(CurrentState.GenericCustomerProfile!);
                }

                profile = State.GenericCustomerProfile.Data;
                await BindCustomerProfileFromPhenix();
                FormDataStatus = DataStatus.Success;

            }
            catch (System.Exception ex)
            {
                State.GenericCustomerProfile.Error(ex);
            }
            finally
            {
                StateHasChanged();
            }
        }


        [Parameter]
        public ProfileDto? CustomerProfileFromFDR { get; set; }
        async Task BindCustomerProfileFromPhenix()
        {
            lookups = await CustomerProfileAppService.GetLookupData();
            var address = profile!.CustomerAddresses?.FirstOrDefault(x => x.AddressId == 1);


            if (!IsSupplementaryCustomer)
            {
                var localProfile = await CustomerProfileAppService.GetCustomerProfileFromFdRlocalDb(profile?.CivilId!);
                if (localProfile is not null && localProfile.IsSuccessWithData)
                    CustomerProfileFromFDR = localProfile.Data;
            }

            Model = CustomerProfileFromFDR;

            if (Model is null)
            {
                Model = new()
                {
                    CivilId = profile.CivilId,
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    Title = profile.TitleId ?? 1,
                    FullName = profile.EnglishName,
                    ArabicName = profile.ArabicName,
                    Birth = profile.BirthDate ?? DateTime.MinValue,
                    Gender = profile.Gender == "M" ? 1 : 0,
                    Country = Convert.ToInt16(address?.CountryCode),
                    Nationality = Convert.ToInt16(profile?.NationalityId!),
                    EmployerName = profile?.EmployerName!,
                    Email = address?.EmailAddress,
                    Area = "-1",
                    Street = address?.Street,
                    BlockNo = address?.BlockNumber,
                    Buildno = address?.House,
                    Flatno = address?.FlatNumber,
                    CIDExpiryDate = profile?.CIDExpiryDate!,
                };
            }
            else
            {
                Model.CIDExpiryDate = Model.CIDExpiryDate ?? profile.CIDExpiryDate;
                Model.FirstName = Model.FirstName ?? profile.FirstName;
                Model.LastName = Model.LastName ?? profile.LastName;
                Model.Title = Model.Title ?? profile.TitleId ?? 1;
                Model.FullName = Model.FullName ?? profile.EnglishName;
                Model.ArabicName = Model.ArabicName ?? profile.ArabicName;
                Model.Country = Model.Country ?? Convert.ToInt16(address?.CountryCode);
                Model.Nationality = Model.Nationality ?? Convert.ToInt16(profile.NationalityId);
                Model.EmployerName = Model.EmployerName ?? profile.EmployerName ?? "";
                Model.Email = Model.Email ?? address?.EmailAddress;
                Model.Area = Model.Area ?? "-1";
                Model.Street = Model.Street ?? address?.Street;
                Model.BlockNo = Model.BlockNo ?? address?.BlockNumber;
                Model.Buildno = Model.Buildno ?? address?.House;
                Model.Flatno = Model.Flatno ?? address?.FlatNumber;
                Model.CIDExpiryDate = Model.CIDExpiryDate ?? profile.CIDExpiryDate;
            }

            editContext = new(Model);


        }

        async Task HandleValidSubmit()
        {
            if (Model is null)
                return;

            await SubmitRequest();
        }

        void HandleInvalidSubmit()
        {
            ValidSubmit = false;
        }

        public async Task DeleteProfile()
        {
            if (Model is null)
                return;

            var result = await CustomerProfileAppService.DeleteCustomerProfileInFdR(Model.CivilId!);
            Message = result.Message;

            if (result.IsSuccess)
            {
                Notification.Success(result.Message);
                ValidSubmit = true;
            }
            else
                Notification.Failure(result.Message);
        }

        public async Task<bool> SubmitRequest()
        {
            if (Model is null)
                return false;

            if (!(editContext?.Validate() ?? false))
            {
                Notification.Failure("Please check the input fields");
                ValidSubmit = false;
                return false;
            }

            Notification.Loading("Profile creation is in process..");
            var result = await CustomerProfileAppService.CreateCustomerProfileInFdR(Model);
            if (!result.IsSuccess)
            {
                Message = result.Message;
                Notification.Failure(result.Message);
                return false;
            }


            Notification.Success("Profile successfully created.");
            ValidSubmit = true;

            if (!IsSupplementaryCustomer)
                NavigateTo("/customer-view?civilId=" + Model.CivilId.Encode());

            return true;
        }



    }
}
