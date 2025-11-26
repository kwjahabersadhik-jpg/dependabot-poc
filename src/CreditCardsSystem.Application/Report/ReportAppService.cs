using Aspose.Pdf;
using CreditCardsSystem.Domain;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardPayment;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.UserSettings;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Newtonsoft.Json;
using StaticDataInquiriesServiceReference;
using Telerik.DataSource.Extensions;
using Telerik.Reporting;
using Telerik.Reporting.Processing;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using Table = Telerik.Reporting.Table;

namespace CreditCardsSystem.Application.Report
{
    public class ReportAppService(IIntegrationUtility integrationUtility,
        IOptions<IntegrationOptions> options,
        ILogger<ReportAppService> logger,
        IAuditLogger<ReportAppService> auditLogger,
        IRequestAppService requestAppService,
        ICustomerProfileAppService customerProfileAppService,
        ICustomerProfileAppService genericCustomerProfileAppService,
        ICardDetailsAppService cardDetailsAppService,
        IAuthManager authManager,
        IAccountsAppService accountsAppService,
        IEmployeeAppService employeeService,
        IOrganizationClient organizationClient,
        IUserPreferencesClient userPreferencesClient,
        IDocumentAppService documentAppService,
        IConfigurationAppService configurationAppService,
        IConfigParameterAppService configParameterService, IConfiguration configuration) : BaseApiResponse, IReportAppService, IAppService
    {

        #region Variables
        private readonly StaticDataInquiriesServiceClient _staticDataInquiriesServiceClient = integrationUtility.GetClient<StaticDataInquiriesServiceClient>(options.Value.Client, options.Value.Endpoints.StaticDataInquiries, options.Value.BypassSslValidation);

        private readonly IEmployeeAppService _employeeService = employeeService;
        private readonly IOrganizationClient _organizationClient = organizationClient;
        private readonly IUserPreferencesClient _userPreferencesClient = userPreferencesClient;
        private readonly IDocumentAppService documentAppService = documentAppService;
        private readonly IConfigParameterAppService _configParameterService = configParameterService;
        private readonly IRequestAppService _requestAppService = requestAppService;
        private readonly ICustomerProfileAppService _customerProfileAppService = customerProfileAppService;
        private readonly ICustomerProfileAppService _genericCustomerProfileAppService = genericCustomerProfileAppService;
        private readonly ILogger<ReportAppService> _logger = logger;
        private readonly IAuditLogger<ReportAppService> auditLogger = auditLogger;
        private readonly ICardDetailsAppService _cardDetailsAppService = cardDetailsAppService;
        private readonly IAuthManager _authManager = authManager;
        private readonly IAccountsAppService _accountsAppService = accountsAppService;

        public List<UserPreferences>? UserPreferences { get; set; } = null!;
        public UserDto? CurrentUser { get; set; } = null!;
        private async Task BindUserDetails(decimal? kfhId)
        {
            if (kfhId is null or 0)
                kfhId = Convert.ToDecimal(_authManager.GetUser()?.KfhId);

            CurrentUser = await _employeeService.GetCurrentLoggedInUser(kfhId);
            UserPreferences = await _userPreferencesClient.GetUserPreferences(kfhId.ToString()!);

            _ = int.TryParse(UserPreferences?.FromUserPreferences().DefaultBranchIdValue, out int _defaultBranchId);
            var defaultBranches = UserPreferences?.FromUserPreferences().UserBranches;

            if (!defaultBranches.AnyWithNull())
                defaultBranches = await _organizationClient.GetUserBranches(kfhId.ToString()!);

            var defaultBranch = defaultBranches?.FirstOrDefault(x => x.BranchId == _defaultBranchId);

            CurrentUser ??= new();
            CurrentUser.DefaultBranchId = _defaultBranchId;
            CurrentUser.DefaultBranchName = defaultBranch?.Name;
        }
        private static string rootPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

        #endregion

        [HttpPost]
        public async Task<ApiResponseModel<EFormResponse>> GenerateDebitVoucher([FromBody] DebitVoucher voucherData)
        {
            string? kfhId = _authManager.GetUser()?.KfhId;
            await PrepareFormData();
            byte[] eformInBytes = await GetMainPages();

            Guid fileId = Guid.Empty;
            string fileName = $"DebitVoucher_{voucherData.CivilID}_{voucherData.MaskedCardNumber.Masked(6, 6)}";
            Domain.Models.Reports.DocumentFileAttributes attributes = new()
            {
                Type = "Voucher",
                Description = fileName,
                FileName = fileName,
                Extension = FileExtension.pdf,
                FileBytes = eformInBytes,
                KfhId = _authManager.GetUser()?.KfhId
            };
            try
            {
                //Uploading file into server
                fileId = await documentAppService.UploadDocuments(attributes);
            }
            catch (System.Exception ex)
            {
                string message = $"unable to upload Voucher file for {voucherData.MaskedCardNumber} - {voucherData.CivilID}-{ex.Message}";
                _logger.LogInformation(message: message);
            }

            return Success(new EFormResponse() { FileId = fileId, FileName = attributes.FileName, FileBytes = eformInBytes }); ;

            async Task PrepareFormData()
            {
                var requestDetailResponse = await _requestAppService.GetRequestDetail(voucherData.RequestId);
                if (!requestDetailResponse.IsSuccessWithData)
                    throw new ApiException(message: "Invalid RequestId");

                var cardRequest = requestDetailResponse.Data;

                if (string.IsNullOrEmpty(cardRequest.Parameters.MarginTransferReferenceNumber))
                    throw new ApiException(message: "unable to find Margin Transfer Reference Number ");

                var accountDetailResponse = await _accountsAppService.GetDebitAccountsByAccountNumber(cardRequest!.AcctNo!);
                if (!accountDetailResponse.IsSuccess)
                    throw new ApiException(message: "accounts not found");

                var accountDetail = accountDetailResponse!.Data!.FirstOrDefault();
                var cardDef = await _cardDetailsAppService.GetCardWithExtension(cardRequest.CardType);
                (string defaultBranchId, string defaultBranchName) = await BranchDetail(configurationAppService, kfhId, cardRequest);
                _ = decimal.TryParse(cardRequest.Parameters.VoucherAmount.IsNullOrDefault(cardRequest.Parameters.MarginAmount ?? ""), out decimal _marginAmount);

                voucherData = new()
                {
                    RequestId = cardRequest.RequestId,
                    AcctName = accountDetail!.Title!,
                    BranchName = defaultBranchName!,
                    CardLimit = (int)cardRequest.RequestedLimit,
                    TransferDesc = cardDef.Name,
                    Crncy = accountDetail.Currency,
                    DebitAcctNo = accountDetail.Acct + " " + accountDetail.Currency,
                    CreditAcctNo = cardRequest.Parameters.MarginAccountNumber!,
                    IBAN = accountDetail.Iban!,
                    PrintDate = DateTime.Now,
                    ParamFooter = defaultBranchId + " / " + _authManager.GetUser()?.KfhId + " / " + "0000" + " / " + DateTime.Now.ToString(ConfigurationBase.ReportDateFormat).Replace("/", ""),
                    CivilID = cardRequest.City,
                    Tafkeet = cardRequest.RequestedLimit.ToLetter(),
                    Amt = (int)cardRequest.RequestedLimit,
                    ParamHeader = await CreateHeaderString(cardRequest.Parameters.MarginTransferReferenceNumber!, cardRequest.RequestedLimit, "", ConfigurationBase.INTERNAL_TRANSFER_MARGIN_SERVICE_NAME),
                    MaskedCardNumber = cardRequest.CardNo.Masked(6, 6)
                };

                async Task<string> CreateHeaderString(string ChargeReferenceNo, decimal Amt, string channelId, string service_name)
                {
                    String referenceNumber = "", referenceNumberTwo = "";
                    if (ChargeReferenceNo != "")
                    {
                        referenceNumber = Convert.ToInt64(ChargeReferenceNo.Substring(0, 16)).ToString();
                        referenceNumberTwo = ChargeReferenceNo.Substring(16, 4);
                    }

                    var checkNumber = "0";
                    var chargeAmount = (Amt * 1000).ToString().Replace(".", ""); // Example 5 KD = 5000
                    var debitOrCreditFlag = "1";

                    if (channelId == "")
                        channelId = (await _staticDataInquiriesServiceClient.getValueByServiceAndAttributeAsync(new() { serviceName = service_name, attributeName = "channel_id" }))?.getValueByServiceAndAttributeResult ?? "";


                    if (referenceNumber.Length < (int)MiscConstants.REF_NUMBER_LENGTH)
                        referenceNumber = String.Format("{0:N" + ((int)MiscConstants.REF_NUMBER_LENGTH - referenceNumber.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + referenceNumber;

                    if (referenceNumberTwo.Length < (int)MiscConstants.REF_NUMBERTWO_LENGTH)
                        referenceNumberTwo = String.Format("{0:N" + ((int)MiscConstants.REF_NUMBERTWO_LENGTH - referenceNumberTwo.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + referenceNumberTwo;

                    if (checkNumber.Length < (int)MiscConstants.CHECK_NUMBER_LENGTH)
                        checkNumber = String.Format("{0:N" + ((int)MiscConstants.CHECK_NUMBER_LENGTH - checkNumber.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + checkNumber;

                    if (chargeAmount.Length < (int)MiscConstants.AMOUNT_LENGTH)
                        chargeAmount = String.Format("{0:N" + ((int)MiscConstants.AMOUNT_LENGTH - chargeAmount.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + chargeAmount;

                    if (debitOrCreditFlag.Length < (int)MiscConstants.FLAG_LENGTH)
                        debitOrCreditFlag = String.Format("{0:N" + ((int)MiscConstants.FLAG_LENGTH - debitOrCreditFlag.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + debitOrCreditFlag;

                    if (channelId.Length < (int)MiscConstants.CHANNEL_ID_LENGTH)
                        channelId = String.Format("{0:N" + ((int)MiscConstants.CHANNEL_ID_LENGTH - channelId.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + channelId;

                    return $"{referenceNumber}  {referenceNumberTwo}     {checkNumber} {chargeAmount}    {debitOrCreditFlag} + {channelId}";
                    //return  referenceNumber + spaceTwo + referenceNumberTwo + spaceThree + spaceTwo + checkNumber + spaceOne + chargeAmount + spaceTwo + spaceTwo + debitOrCreditFlag + channelId;
                }

                static async Task<(string defaultBranchId, string defaultBranchName)> BranchDetail(IConfigurationAppService configurationAppService, string? kfhId, RequestDto? cardRequest)
                {
                    var defaultBranchId = cardRequest.Parameters.MigratorBranchId;
                    var defaultBranchName = cardRequest.Parameters.MigratorBranchName;
                    if (string.IsNullOrEmpty(defaultBranchName))
                    {
                        var branch = await configurationAppService.GetUserBranch(kfhId.ToDecimal());
                        defaultBranchId = branch.BranchId.ToString();
                        defaultBranchName = branch.Name;
                    }
                    return (defaultBranchId, defaultBranchName);
                }
            }

            

        async Task<byte[]> GetMainPages()
            {
                var byteArr = await File.ReadAllBytesAsync(ConfigurationBase.VoucherDebitReportPath);
                var reportPackager = new ReportPackager();

                using var sourceStream = new MemoryStream(byteArr);
                var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);
                // assigning report sources
                if (report.DataSource is JsonDataSource json)
                {
                    json.Source = JsonConvert.SerializeObject(voucherData);
                }

                RenderingResult result = new ReportProcessor()
                        .RenderReport("PDF", reportSource: new InstanceReportSource() { ReportDocument = report }, null);

                return result.DocumentBytes;
            }

        }

        [HttpPost]
        public async Task<ApiResponseModel<EFormResponse>> GenerateDepositVoucher([FromBody] DepositVoucher voucherData)
        {
            await PrepareFormData();
            byte[] eformInBytes = await GetMainPages();

            Guid fileId = Guid.Empty;
            string fileName = $"DepositVoucher_{voucherData.CivilID}_{voucherData.DebitAcctNo}";
            Domain.Models.Reports.DocumentFileAttributes attributes = new()
            {
                Type = "Voucher",
                Description = fileName,
                FileName = fileName,
                Extension = FileExtension.pdf,
                FileBytes = eformInBytes,
                KfhId = _authManager.GetUser()?.KfhId
            };
            try
            {
                //Uploading file into server
                fileId = await documentAppService.UploadDocuments(attributes);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation($"unable to upload Voucher file for {fileName}-{ex.Message}");
            }

            return Success(new EFormResponse() { FileId = fileId, FileName = attributes.FileName, FileBytes = eformInBytes }); ;

            async Task PrepareFormData()
            {
                var requestDetailResponse = await _requestAppService.GetRequestDetail(voucherData.RequestId);
                if (!requestDetailResponse.IsSuccessWithData)
                    throw new ApiException(message: "Invalid RequestId");

                var cardRequest = requestDetailResponse.Data;


                var cardDef = await _cardDetailsAppService.GetCardWithExtension(cardRequest.CardType);


                var accountDetailResponse = await _accountsAppService.GetDebitAccountsByAccountNumber(cardRequest!.AcctNo!);
                if (!accountDetailResponse.IsSuccess)
                    throw new ApiException(message: "accounts not found");

                var accountDetail = accountDetailResponse!.Data!.FirstOrDefault();
                var defaultBranchId = cardRequest.Parameters.MigratorBranchId;
                var defaultBranchName = cardRequest.Parameters.MigratorBranchName;

                voucherData = new()
                {
                    RequestId = cardRequest.RequestId,
                    AcctName = accountDetail!.Title!,
                    BranchName = defaultBranchName!,
                    Crncy = accountDetail.Currency,
                    DebitAcctNo = accountDetail.Acct + " " + accountDetail.Currency,
                    IBAN = accountDetail.Iban!,
                    PrintDate = DateTime.Now,
                    HoldDesc = $"{cardDef.Name}-{cardRequest.RequestedLimit}",
                    ParamFooter = defaultBranchId + " / " + _authManager.GetUser()?.KfhId + " / " + "0000" + " / " + DateTime.Now.ToString(ConfigurationBase.ReportDateFormat).Replace("/", ""),
                    CivilID = cardRequest.City,
                    ParamHeader = await CreateHeaderString(cardRequest.Parameters.MarginTransferReferenceNumber!, cardRequest.RequestedLimit, ConfigurationBase.INTERNAL_TRANSFER_DEPOSIT_ChannelId, ConfigurationBase.INTERNAL_TRANSFER_DEPOSIT_SERVICE_NAME),
                };

                async Task<string> CreateHeaderString(string ChargeReferenceNo, decimal Amt, string channelId, string service_name)
                {
                    String referenceNumber = "", referenceNumberTwo = "";
                    if (ChargeReferenceNo != "")
                    {
                        referenceNumber = Convert.ToInt64(ChargeReferenceNo.Substring(0, 16)).ToString();
                        referenceNumberTwo = ChargeReferenceNo.Substring(16, 4);
                    }

                    var checkNumber = "0";
                    var chargeAmount = (Amt * 1000).ToString().Replace(".", ""); // Example 5 KD = 5000
                    var debitOrCreditFlag = "1";

                    if (channelId == "")
                        channelId = (await _staticDataInquiriesServiceClient.getValueByServiceAndAttributeAsync(new() { serviceName = service_name, attributeName = "channel_id" }))?.getValueByServiceAndAttributeResult ?? "";


                    if (referenceNumber.Length < (int)MiscConstants.REF_NUMBER_LENGTH)
                        referenceNumber = String.Format("{0:N" + ((int)MiscConstants.REF_NUMBER_LENGTH - referenceNumber.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + referenceNumber;

                    if (referenceNumberTwo.Length < (int)MiscConstants.REF_NUMBERTWO_LENGTH)
                        referenceNumberTwo = String.Format("{0:N" + ((int)MiscConstants.REF_NUMBERTWO_LENGTH - referenceNumberTwo.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + referenceNumberTwo;

                    if (checkNumber.Length < (int)MiscConstants.CHECK_NUMBER_LENGTH)
                        checkNumber = String.Format("{0:N" + ((int)MiscConstants.CHECK_NUMBER_LENGTH - checkNumber.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + checkNumber;

                    if (chargeAmount.Length < (int)MiscConstants.AMOUNT_LENGTH)
                        chargeAmount = String.Format("{0:N" + ((int)MiscConstants.AMOUNT_LENGTH - chargeAmount.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + chargeAmount;

                    if (debitOrCreditFlag.Length < (int)MiscConstants.FLAG_LENGTH)
                        debitOrCreditFlag = String.Format("{0:N" + ((int)MiscConstants.FLAG_LENGTH - debitOrCreditFlag.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + debitOrCreditFlag;

                    if (channelId.Length < (int)MiscConstants.CHANNEL_ID_LENGTH)
                        channelId = String.Format("{0:N" + ((int)MiscConstants.CHANNEL_ID_LENGTH - channelId.Length) + "}", 0).Replace("0.", "").Replace("0", " ") + channelId;

                    return $"{referenceNumber}  {referenceNumberTwo}     {checkNumber} {chargeAmount}    {debitOrCreditFlag} + {channelId}";
                    //return  referenceNumber + spaceTwo + referenceNumberTwo + spaceThree + spaceTwo + checkNumber + spaceOne + chargeAmount + spaceTwo + spaceTwo + debitOrCreditFlag + channelId;
                }


            }


            async Task<byte[]> GetMainPages()
            {
                var byteArr = await File.ReadAllBytesAsync(ConfigurationBase.VoucherDebitReportPath);
                var reportPackager = new ReportPackager();

                using var sourceStream = new MemoryStream(byteArr);
                var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);
                // assigning report sources
                if (report.DataSource is JsonDataSource json)
                {
                    json.Source = JsonConvert.SerializeObject(voucherData);
                }

                RenderingResult result = new ReportProcessor()
                        .RenderReport("PDF", reportSource: new InstanceReportSource() { ReportDocument = report }, null);

                return result.DocumentBytes;
            }

        }

        [HttpGet]
        public async Task<ApiResponseModel<EFormResponse>> GenerateAfterSalesForm(AfterSalesForm afterSalesForm)
        {
            var apiResponse = new ApiResponseModel<EFormResponse>();
            string fileName = $"{afterSalesForm.ActionType}_AfterSalesEForm_{afterSalesForm.CivilID}_{afterSalesForm.CardNo.Masked(6, 6)}";
            afterSalesForm.ActionType = afterSalesForm.ActionType.ToUpper();
            await PrepareFormData();
            byte[] eformInBytes = await GetMainPages();

            Guid fileId = Guid.Empty;
            Domain.Models.Reports.DocumentFileAttributes attributes = new()
            {
                Type = "AfterSales EForm",
                Description = fileName,
                FileName = fileName,
                Extension = FileExtension.pdf,
                FileBytes = eformInBytes,
                KfhId = afterSalesForm.KfhId
            };

            try
            {
                //Uploading file into server
                fileId = await documentAppService.UploadDocuments(attributes);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation($"unable to upload AfterSalesEForm file for {afterSalesForm.ActionType} - {afterSalesForm.CivilID}-{ex.Message}");
            }

            return apiResponse.Success(new() { FileId = fileId, FileName = attributes.FileName, FileBytes = eformInBytes }); ;

            async Task PrepareFormData()
            {

                var requestDetailResponse = await _requestAppService.GetRequestDetail(afterSalesForm.RequestId);
                if (!requestDetailResponse.IsSuccessWithData)
                    throw new ApiException(message: "Invalid RequestId");

                afterSalesForm.MapRequestData(requestDetailResponse.Data);


                var customerProfile = await _customerProfileAppService.GetCustomerProfileFromFdRlocalDb(afterSalesForm.CivilID!);
                afterSalesForm.CustomerName = customerProfile?.Data?.HolderName;

                afterSalesForm.CardNo = afterSalesForm.CardNo?.Masked(6, 6).SplitByFour();

            }
            async Task<byte[]> GetMainPages()
            {
                var byteArr = await File.ReadAllBytesAsync($"{rootPath}/Report/AfterSalesForm.trdp");
                var reportPackager = new ReportPackager();

                using var sourceStream = new MemoryStream(byteArr);
                var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);
                // assigning report sources

                afterSalesForm.IsRebrand = configuration.GetValue<string>("Rebrand")?.Equals("true") ?? true;

                if (report.DataSource is JsonDataSource json)
                {
                    json.Source = JsonConvert.SerializeObject(afterSalesForm);
                }


                RenderingResult result = new ReportProcessor()
                    .RenderReport("PDF", reportSource: new InstanceReportSource() { ReportDocument = report }, null);

                return result.DocumentBytes;
            }

        }

        [HttpGet]
        public async Task<ApiResponseModel<EFormResponse>> GenerateCardIssuanceEForm(decimal RequestId)
        {
            var apiResponse = new ApiResponseModel<EFormResponse>();
            var applicationForm = await PrepareFormData();
            byte[] mainPagesFormBytes = await GetMainPages();
            byte[] termsAndConditionPageBytes = await GetTermsAndCondtionPage();

            List<Stream> pdf = new()
            {
                new MemoryStream(mainPagesFormBytes),
                new MemoryStream(termsAndConditionPageBytes)
            };

            if (IsForeignCurrencyCards())
            {
                byte[] declarationPage = await GetDeclarationPage(applicationForm);

                if (declarationPage is not null)
                    pdf.Add(new MemoryStream(declarationPage));
            }



            var allPagesBytes = await MergedForms();

            Guid fileId = Guid.Empty;
            string fileName = $"EForm_{applicationForm.CivilID}_{applicationForm.ProductName.Replace(" ", "_")}";
            Domain.Models.Reports.DocumentFileAttributes attributes = new()
            {
                Type = "Card Issuance",
                Description = fileName,
                FileName = fileName,
                Extension = FileExtension.pdf,
                FileBytes = allPagesBytes
            };

            try
            {
                //Uploading file into server
                fileId = await documentAppService.UploadDocuments(attributes);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation($"unable to upload Card Issuance EForm application file for {applicationForm.RequestId}-{ex.Message}");
            }

            return apiResponse.Success(new() { FileId = fileId, FileName = attributes.FileName, FileBytes = allPagesBytes }); ;


            bool IsForeignCurrencyCards() => ((applicationForm.ProductId >= 43 && applicationForm.ProductId <= 47) || applicationForm.ProductId == 51);


            async Task<ApplicationForm> PrepareFormData()
            {
                var newRequestResponse = await _requestAppService.GetRequestDetail(RequestId);
                if (!newRequestResponse.IsSuccess)
                    throw new ApiException(message: "Invalid RequestId");

                var newRequest = newRequestResponse.Data;

                var profileTask = _customerProfileAppService.GetCustomerProfileFromFdRlocalDb(newRequest!.CivilId);
                var gProfileTask = _genericCustomerProfileAppService.GetDetailedGenericCustomerProfile(new() { CivilId = newRequest!.CivilId });
                var cardTask = _cardDetailsAppService.GetCardWithExtension(newRequest.CardType);
                var lookupTask = _customerProfileAppService.GetLookupData();
                var userDetailTask = BindUserDetails(newRequest.TellerId);
                var accountTypeConfigTask = _configParameterService.GetByStartsWith(ConfigurationBase.AccountType);



                await Task.WhenAll([profileTask, gProfileTask, cardTask, lookupTask, accountTypeConfigTask]);

                var profile = profileTask.Result.Data ?? throw new ApiException(message: "invalid customer");
                var customerProfile = gProfileTask.Result.Data ?? throw new ApiException(message: "invalid customer");
                var cardDefinition = cardTask.Result;
                var lookup = lookupTask.Result;
                var accountTypeConfigs = accountTypeConfigTask.Result.Data;




                return new ApplicationForm(profile, newRequest, customerProfile, lookup, accountTypeConfigs)
                {
                    ProductName = cardDefinition!.Name,
                    Branch = CurrentUser?.DefaultBranchName ?? "",
                    IsPrepaid = cardDefinition.IsPrepaid
                };
            }




            async Task<byte[]> GetMainPages()
            {
                var byteArr = await File.ReadAllBytesAsync($"{rootPath}/Report/ApplicationForm.trdp");
                var reportPackager = new ReportPackager();

                using var sourceStream = new MemoryStream(byteArr);
                var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);

                applicationForm.IsRebrand = configuration.GetValue<string>("Rebrand")?.Equals("true") ?? true;

                // assigning report sources
                if (report.DataSource is JsonDataSource json)
                {
                    json.Source = JsonConvert.SerializeObject(applicationForm);
                }

                //assign table1 source
                if (report.Items["detail"].Items["panel3"].Items["SupplementaryTable"] is Table supplementaryTable)
                {
                    if (supplementaryTable.DataSource is JsonDataSource supplementaryJson)
                    {
                        supplementaryJson.Source = JsonConvert.SerializeObject(applicationForm.Supplementary);
                    }
                }

                RenderingResult result = new ReportProcessor()
                        .RenderReport("PDF", reportSource: new InstanceReportSource() { ReportDocument = report }, null);

                return result.DocumentBytes;
            }

            async Task<byte[]> GetTermsAndCondtionPage()
            {
                byte[] termsAndConditionPageBytes = Enumerable.Empty<byte>().ToArray();

                try
                {
                    termsAndConditionPageBytes = File.ReadAllBytes($"{rootPath}/Report/TC_PDFs/{applicationForm.ProductId}.pdf");
                }
                catch
                {
                    //Keep empty terms and condition page
                    termsAndConditionPageBytes = File.ReadAllBytes($"{rootPath}/Report/TC_PDFs/Empty.pdf");
                }

                return await Task.FromResult(termsAndConditionPageBytes);
            }
            async Task<byte[]> MergedForms()
            {
                // Set license
                License license = new();
                string licPath = $"{rootPath}/Report/Aspose.PDF.NET.lic";
                license.SetLicense(licPath);

                //merging pdf files
                Document document = new();

                var allPages = pdf.SelectMany(item => new Document(item).Pages);
                document.Pages.AddRange(allPages);

                //convert files to byte array
                MemoryStream stream = new();
                document.Save(stream);
                return await Task.FromResult(stream.ToArray());
            }
        }

        [HttpGet]
        public async Task<ApiResponseModel<EFormResponse>> GenerateDeclartationForm(decimal RequestId)
        {
            var apiResponse = new ApiResponseModel<EFormResponse>();
            var cardRequest = await requestAppService.GetRequestDetail(RequestId) ?? throw new ApiException(message: "Invalid request!");

            var profileTask = _customerProfileAppService.GetCustomerProfileFromFdRlocalDb(cardRequest.Data.CivilId);
            var cardTask = _cardDetailsAppService.GetCardWithExtension(cardRequest.Data.CardType);

            await Task.WhenAll([profileTask, cardTask]);
            var cardDefinition = cardTask.Result;
            var profile = profileTask.Result.Data ?? throw new ApiException(message: "invalid customer");

            var applicationForm = new ApplicationForm(profile: profile, request: cardRequest.Data, genericProfile: new(), lookup: new(), accountTypeConfigs: null)
            {
                ProductName = cardDefinition!.Name,
            };
            byte[] declarationPage = await GetDeclarationPage(applicationForm);



            Guid fileId = Guid.Empty;
            string fileName = $"DeclarationForm_{applicationForm.CivilID}_{applicationForm.ProductName.Replace(" ", "_")}";
            Domain.Models.Reports.DocumentFileAttributes attributes = new()
            {
                Type = "Declaration Form",
                Description = fileName,
                FileName = fileName,
                Extension = FileExtension.pdf,
                FileBytes = declarationPage
            };

            try
            {
                //Uploading file into server
                fileId = await documentAppService.UploadDocuments(attributes);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation($"unable to upload Card Declaration file for {applicationForm.RequestId}-{ex.Message}");
            }

            return apiResponse.Success(new() { FileId = fileId, FileName = attributes.FileName, FileBytes = declarationPage }); ;

        }

        private async Task<byte[]> GetDeclarationPage(ApplicationForm applicationForm)
        {
            var byteArr = await File.ReadAllBytesAsync($"{rootPath}/Report/Declaration.trdp");
            var reportPackager = new ReportPackager();

            using var sourceStream = new MemoryStream(byteArr);
            var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);
            // assigning report sources
            if (report.DataSource is JsonDataSource json)
            {
                json.Source = JsonConvert.SerializeObject(new DeclarationForm()
                {
                    CardHolderName = applicationForm.CardHolderName,
                    CivilID = applicationForm.CivilID,
                    Nationality = applicationForm.Nationality ?? "",
                    IsRebrand = configuration.GetValue<string>("Rebrand")?.Equals("true") ?? true
                });
            }

            RenderingResult result = new ReportProcessor()
                .RenderReport("PDF", reportSource: new InstanceReportSource() { ReportDocument = report }, null);

            return result.DocumentBytes;
        }
        [NonAction]
        public async Task<ApiResponseModel<EFormResponse>> PrintDynamicReport<T>(T reportData, FileExtension fileExtension = FileExtension.pdf) where T : class
        {
            PdfExportAttribute reportAttribute = reportData switch
            {
                EODBranchReportDto => new(reportData as EODBranchReportDto, $"EOD_Report_Branch__{(reportData as EODBranchReportDto)!.BranchName.Trim().Replace(" ", "")}", ConfigurationBase.EODBranchReport),
                EODStaffReportDto => new(reportData as EODStaffReportDto, $"EOD_Report_Staff__{(reportData as EODStaffReportDto)!.TellerName.Trim().Replace(" ", "")}", ConfigurationBase.EODStaffReport),
                ChangeLimitReportDto => new(reportData as ChangeLimitReportDto, $"Statistical_ChangeLimitHistory_Report_{(reportData as ChangeLimitReportDto)!.RequestedDateFrom?.ToString("D")}", ConfigurationBase.ChangeLimitReport),
                SingleReportDto => new(reportData as SingleReportDto, $"SingleReport_{(reportData as SingleReportDto).ReportPeriod}", ConfigurationBase.SingleReport),
                ReplacementTrackingReportData => new(reportData as ReplacementTrackingReportData, $"ReplacementTrackingReport_{(reportData as ReplacementTrackingReportData).CivilId}", ConfigurationBase.CardReplacementTrack),
                _ => new(null, null, null)
            };

            if (reportAttribute.data is RebrandDto)
                (reportAttribute.data as RebrandDto).IsRebrand = configuration.GetValue<string>("Rebrand")?.Equals("true") ?? true;


            byte[] fileBytes = reportData switch
            {
                EODBranchReportDto or EODStaffReportDto or SingleReportDto => await GetFileBytes(reportAttribute.path, data: reportAttribute.data, fileExtension: fileExtension),

                ChangeLimitReportDto => await GetFileBytes(reportAttribute.path, dataList: (reportData as ChangeLimitReportDto).Data, fileExtension: fileExtension),

                _ => await GetFileBytes(reportAttribute.path, data: reportAttribute.data, fileExtension: fileExtension)
            };


            Guid fileId = Guid.Empty;


            DocumentFileAttributes attributes = new()
            {
                Type = "CreditCardReports",
                Description = reportAttribute.fileName,
                FileName = reportAttribute.fileName,
                Extension = fileExtension,
                FileBytes = fileBytes,
                KfhId = _authManager.GetUser()?.KfhId
            };
            try
            {
                //Uploading file into server
                fileId = await documentAppService.UploadDocuments(attributes);
            }
            catch (System.Exception ex)
            {
                string message = $"unable to upload Voucher file for {attributes.FileName}-{ex.Message}";
                _logger.LogInformation(message: message);
            }

            return Success(new EFormResponse() { FileId = fileId, FileName = attributes.FileName, FileBytes = fileBytes }); ;
        }
        async Task<byte[]> GetFileBytes<T>(string reportPath, T? data = null, IEnumerable<T>? dataList = null, FileExtension fileExtension = FileExtension.pdf) where T : class, new()

        {
            if (data is RebrandDto)
                (data as RebrandDto).IsRebrand = configuration.GetValue<string>("Rebrand")?.Equals("true") ?? true;

            if (data is ReplacementTrackingReportData && fileExtension is (FileExtension.xls or FileExtension.xlsx))
            {
                bool IsRebrand = configuration.GetValue<string>("Rebrand")?.Equals("true") ?? true;
                reportPath = reportPath.Replace(".trdp", $"{(IsRebrand ? "V2" : "V1")}.xlsx");
                using MemoryStream memoryStream = new();
                memoryStream.SaveAsByTemplate(reportPath, data);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream.ToArray();
            }


            var byteArr = await File.ReadAllBytesAsync(reportPath);
            var reportPackager = new ReportPackager();

            using var sourceStream = new MemoryStream(byteArr);
            var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);
            // assigning report sources
            if (report.DataSource is JsonDataSource json)
            {

                if (data is not null)
                    json.Source = JsonConvert.SerializeObject(data);
                else
                    json.Source = JsonConvert.SerializeObject(dataList);
            }

            string fileFormat = fileExtension switch
            {
                FileExtension.pdf => "PDF",
                FileExtension.xls => "XLSX",
                FileExtension.xlsx => "XLSX",
                _ => "PDF"
            };


            RenderingResult result = new ReportProcessor()
                    .RenderReport(fileFormat, reportSource: new InstanceReportSource() { ReportDocument = report }, null);

            return result.DocumentBytes;
        }

        [HttpPost]
        public async Task<ApiResponseModel<EFormResponse>> GenerateCardPaymentVoucher([FromBody]PaymentVoucher voucherData)
        {
            byte[] eformInBytes = await GetMainPages();

            Guid fileId = Guid.Empty;
            string fileName = $"PaymentVoucher_{voucherData.MaskedCardNumber}";
            DocumentFileAttributes attributes = new()
            {
                Type = "CreditCardPaymentVoucher",
                Description = fileName,
                FileName = fileName,
                Extension = FileExtension.pdf,
                FileBytes = eformInBytes,
                KfhId = _authManager.GetUser()?.KfhId
            };
            try
            {
                //Uploading file into server
                fileId = await documentAppService.UploadDocuments(attributes);
            }
            catch (System.Exception ex)
            {
                string message = $"unable to upload Voucher file for {voucherData.MaskedCardNumber} - {voucherData.CivilID}-{ex.Message}";
                _logger.LogInformation(message: message);
            }

            return Success(new EFormResponse() { FileId = fileId, FileName = attributes.FileName, FileBytes = eformInBytes }); ;




            async Task<byte[]> GetMainPages()
            {
                var byteArr = await File.ReadAllBytesAsync(ConfigurationBase.CardPaymentVoucherReportPath);
                var reportPackager = new ReportPackager();

                using var sourceStream = new MemoryStream(byteArr);
                var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);
                // assigning report sources
                if (report.DataSource is JsonDataSource json)
                {
                    json.Source = JsonConvert.SerializeObject(voucherData);
                }

                RenderingResult result = new ReportProcessor()
                        .RenderReport("PDF", reportSource: new InstanceReportSource() { ReportDocument = report }, null);

                return result.DocumentBytes;
            }
        }
    }


    public record PdfExportAttribute(object? data, string? fileName, string? path);
    public class DeclarationForm : RebrandDto
    {
        public string CardHolderName { get; set; }
        public string Nationality { get; set; }
        public string CivilID { get; set; }
    }

}
