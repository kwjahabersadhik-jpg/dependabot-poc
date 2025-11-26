using CreditCardsSystem.Application.SuplementaryCards;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CreditReverse;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Domain.Models.UserSettings;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using CreditCardTransactionInquiryServiceReference;
using DocumentFormat.OpenXml.Drawing;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CreditCardsSystem.Application.CardDetail
{
    public interface ISecureData
    {
        string GetSecureCardNumber(string value);
        string GetSecureAvailableBalance(string value);
    }

    public class SecureData(IAuthManager authManager) : ISecureData
    {
        public string GetSecureAvailableBalance(string value)
        {
            var isView = authManager.HasPermission(Permissions.AccountsBalance.View());
            if (isView) return value;

            return value.SaltThis();
        }

        public string GetSecureCardNumber(string value)
        {
            var isView = authManager.HasPermission(Permissions.CreditCardsNumber.View());
            if (isView) return value;

            return value.Masked(6, 6).SaltThis();
        }
    }





    public class CardDetailsAppService : BaseApiResponse, IAppService, ICardDetailsAppService
    {
        private readonly FdrDBContext _fdrDBContext;
        private readonly ICustomerProfileAppService _customerProfileAppService;

        private readonly IAuthManager _authManager;
        private readonly IConfigurationAppService _configurationAppService;
        private readonly IUserPreferencesClient _userPreferencesClient;
        private readonly ICurrencyAppService _CurrencyAppService;
        private readonly ICustomerProfileCommonApi _customerProfileCommonApi;
        private readonly ISecureData _secureData;

        private string[] CardNotFoundMessages = ["Either DB CardCurrency Or CardNo Not found", "credit card was not found"];
        private readonly CreditCardInquiryServicesServiceClient _creditCardInquiryServiceClient;

        //private readonly CreditCardUpdateServicesServiceClient _updateServicesServiceClient;
        private static readonly string[] visaCardStartingNumbers = ConfigurationBase.VisaCardStartingNumbers.Split(',', StringSplitOptions.TrimEntries);
        private static readonly string[] masterCardStartingNumbers = ConfigurationBase.MasterCardStartingNumbers.Split(',', StringSplitOptions.TrimEntries);
        private static readonly int cardNumberLength = ConfigurationBase.CreditCardNumberLength;

        private readonly bool canViewCardNumber;
        public CardDetailsAppService(IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options, FdrDBContext fdrDBContext,
            ICustomerProfileAppService customerProfileAppService, IAuthManager authManager, IConfigurationAppService configurationAppService, IUserPreferencesClient userPreferencesClient, ICurrencyAppService currencyAppService, ICustomerProfileCommonApi customerProfileCommonApi, ISecureData secureData)
        {
            _creditCardInquiryServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);

            //_updateServicesServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
            _fdrDBContext = fdrDBContext;
            _customerProfileAppService = customerProfileAppService;
            _authManager = authManager;
            _configurationAppService = configurationAppService;
            _userPreferencesClient = userPreferencesClient;
            _CurrencyAppService = currencyAppService;
            _customerProfileCommonApi = customerProfileCommonApi;
            canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());
            _secureData = secureData;
        }


        [HttpGet]
        public async Task<ApiResponseModel<CardDetailsMinimal>> GetCardInfoMinimal(decimal? requestId, string cardNumber = "")
        {

            var cardRequest = await _fdrDBContext.Requests.AsNoTracking()
                .Where(x => x.RequestId == requestId || x.CardNo == cardNumber)
                .Include(x => x.Parameters).FirstOrDefaultAsync()
                ?? throw new ApiException(message: "Invalid RequestId");

            CardDetailsResponse cardDetails = new();
            await BindProductType(cardRequest, cardDetails);

            var requestParameters = await GetRequestParameter(cardRequest);
            int? payeeTypeId = GetPayeeTypeID(cardRequest.CardType, requestParameters);

            var cardDetailMinimal = new CardDetailsMinimal()
            {
                CivilId = cardRequest.CivilId,
                CardType = cardRequest.CardType,
                CardNumber = cardRequest.CardNo,
                CardNumberDto = cardRequest.CardNo.SaltThis(),// _secureData.GetSecureCardNumber(cardRequest.CardNo),
                RequestId = cardRequest.RequestId,
                ProductType = cardDetails.ProductType,
                IssuanceType = cardDetails.IssuanceType,
                ProductName = cardDetails.ProductName,
                BankAccountNumber = cardRequest.AcctNo,
                IsPrimaryCard = payeeTypeId != null,
                CardStatus = (CreditCardStatus)cardRequest.ReqStatus,
                ApprovedLimit = cardRequest.ApproveLimit ?? 0,
                IsCorporateCard = requestParameters.Collateral == Collateral.AGAINST_CORPORATE_CARD.ToString(),
                IsSupplementary = requestParameters.IsSupplementaryOrPrimaryChargeCard == "S"
            };


            if (Convert.ToBoolean(cardRequest.IsAUB))
            {
                var aubMapping = await _fdrDBContext.AubCardMappings.AsNoTracking().FirstOrDefaultAsync(aub => aub.KfhCardNo == cardRequest.CardNo && aub.IsRenewed == false);
                if (aubMapping is not null)
                {
                    cardDetailMinimal.AUBCardNumber = aubMapping.AubCardNo;
                    cardDetailMinimal.AUBCardNumberDto = aubMapping.AubCardNo?.SaltThis();
                }
            }


            return Success(cardDetailMinimal);
        }


        [HttpGet]
        public async Task<ApiResponseModel<CardDetailsResponse>> GetCardInfo(decimal? requestId,
            string cardNumber = "",
            bool includeCardBalance = false,
           string kfhId = "",
            CancellationToken cancellationToken = default)
        {

            if (!string.IsNullOrEmpty(cardNumber) && !Regex.Match(cardNumber, @"\d{1,17}").Success)
            {
                throw new ApiException(message: "Invalid cardNumber");
            }

            if (!string.IsNullOrEmpty(kfhId) && !Regex.Match(kfhId, @"\d{1,8}").Success)
            {
                throw new ApiException(message: "Invalid kfhId");
            }



            var response = new ApiResponseModel<CardDetailsResponse>();

            cancellationToken.ThrowIfCancellationRequested();

            if (cancellationToken.IsCancellationRequested) return response;

            var cardRequest = await _fdrDBContext.Requests.AsNoTracking()
                .Where(x => x.RequestId == requestId || x.CardNo == cardNumber)
                .Include(x => x.Parameters).FirstOrDefaultAsync()
                ?? throw new ApiException(message: "Invalid RequestId");

            var cardInfo = await GetCardInformationInDetail(cardRequest, includeCardBalance, kfhId);

            return response.Success(cardInfo);
        }


        [HttpGet]
        public async Task<ApiResponseModel<List<SupplementaryCardDetail>>> GetSupplementaryCardsByRequestId(decimal primaryCardRequestId, CancellationToken cancellationToken = default)
        {

            cancellationToken.ThrowIfCancellationRequested();

            var response = new ApiResponseModel<List<SupplementaryCardDetail>>();

            var cardRequest = await _fdrDBContext.Requests.AsNoTracking().Where(x => x.RequestId == primaryCardRequestId).Include(x => x.Parameters).FirstOrDefaultAsync(cancellationToken);
            if (cardRequest == null)
                return response.Fail("Invalid RequestId");

            var requestParameter = await GetRequestParameter(cardRequest);

            int? payeeTypeId = GetPayeeTypeID(cardRequest.CardType, requestParameter);

            if (payeeTypeId is null)
                return response.Success(null, message: "Supplementary Cards Not Found!, due to this card is not primary");

            var supplementaryCards = await LoadSupplementaryByCivilId(cardRequest.CivilId, payeeTypeId, primaryCardRequestId, cancellationToken: cancellationToken);

            var validCards = supplementaryCards.Where(x => x.CardData != null).ToList();

            return response.Success(validCards);
        }


        [HttpGet]
        public async Task<ApiResponseModel<SecondaryCardDetail>> GetMasterSecondaryCardDetails(decimal requestId)
        {

            var secondaryCardNo = (await _fdrDBContext.RequestParameters.AsNoTracking().FirstOrDefaultAsync(x => x.ReqId == requestId && x.Parameter == ReportingConstants.KEY_SECONDARY_CARD_NO))?.Value;

            var response = new ApiResponseModel<SecondaryCardDetail>();

            if (string.IsNullOrEmpty(secondaryCardNo))
                return response.Fail("Incorrect secondaryCardNo !");

            var getBalanceStatusCardDetailResult = (await _creditCardInquiryServiceClient.getBalanceStatusCardDetailAsync(new getBalanceStatusCardDetailRequest()
            {
                cardNo = secondaryCardNo,
            }))?.getBalanceStatusCardDetailResult;


            var secondaryCardDetailsResponse = getBalanceStatusCardDetailResult.Adapt<CardDetailsResponse>();

            if (secondaryCardDetailsResponse is null)
                return response.Fail("data not found!");
            // card status should be external status - SA
            var status = secondaryCardDetailsResponse.AccountActiviationFlag == "Y" ? "Pending Activation" : "Activated";
            //var secondaryCardRequest = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(f => f.CardNo == secondaryCardNo);


            return response.Success(new(requestId, status));
        }



        [HttpGet]
        public async Task<ApiResponseModel<List<SupplementaryCardDetail>>> GetSupplementaryCardsByCivilId(string civilId, int? payeeTypeId = null)
        {
            var response = new ApiResponseModel<List<SupplementaryCardDetail>>();
            var supplementaryCards = await LoadSupplementaryByCivilId(civilId, payeeTypeId);
            return response.Success(supplementaryCards);
        }

        [HttpGet]
        public async Task<ApiResponseModel<CardDefinitionExtentionDto>> GetCardDefinitionExtensionsByProductId(int productId)
        {
            var response = new ApiResponseModel<CardDefinitionExtentionDto>();

            var cardDefinitionExtensionInKeyValue = await (from defExt in _fdrDBContext.CardDefExts.AsNoTracking()
                                                           where defExt.CardType == productId
                                                           select new KeyValueTable
                                                           {
                                                               ColumnName = defExt.Attribute,
                                                               ColumnValue = defExt.Value!
                                                           }).ToListAsync();
            var cardDefinitionExtension = DictionaryExtension.ConvertKeyValueDataToObject<CardDefinitionExtentionDto>(cardDefinitionExtensionInKeyValue);

            return response.Success(cardDefinitionExtension);
        }

        [HttpGet]
        [NonAction]
        public async Task<RequestParameterDto> GetRequestParameter(Request cardRequest)
        {
            if (cardRequest is null)
                return null;

            var requestParameter = DictionaryExtension.ConvertKeyValueDataToObject<RequestParameterDto>(cardRequest.Parameters.Select(x => new KeyValueTable
            {
                ColumnName = x.Parameter,
                ColumnValue = x.Value!
            }));

            return await Task.FromResult(requestParameter);
        }

        [NonAction]
        public async Task<int> GetPayeeProductType(int? cardType)
        {
            var creditCardDefinition = await _fdrDBContext.CardDefs.FirstOrDefaultAsync(x => x.CardType == cardType);
            if (creditCardDefinition is null) return -1;

            if (Helpers.IsPrepaid(creditCardDefinition.CardType))
                return ConfigurationBase.PrimaryPrepaidCardPayeeTypeId;

            var productType = Helpers.GetProductType(creditCardDefinition.Duality, Convert.ToDecimal(creditCardDefinition.MinLimit), Convert.ToDecimal(creditCardDefinition.MaxLimit));
            if (productType == ProductTypes.ChargeCard)
                return ConfigurationBase.PrimaryChargeCardPayeeTypeId;

            return -1;
        }

        [HttpGet]
        public async Task<IssuedCardCounts> GetIssuedSupplementaryCardCounts(string CivilID, int productId)
        {
            var existingCards = await _customerProfileAppService.GetCustomerCards(new()
            {
                CivilId = CivilID
            });

            if (!existingCards.IsSuccessWithData)
                return new IssuedCardCounts();

            var nonClosedCards = existingCards.Data?.Where(d => d.StatusId != (int)CreditCardStatus.Closed && d.StatusId != (int)CreditCardStatus.Lost);
            int alOusra = nonClosedCards.Where(d => (d.CardType == ConfigurationBase.AlOsraPrimaryCardTypeId.ToString() || d.CardType == ConfigurationBase.AlOsraSupplementaryCardTypeId.ToString()))?.Count() ?? 0;

            int total = nonClosedCards.Count(x => x.CardCategory == CardCategoryType.Supplementary);
            //int chargeCard = total - alOusra;
            int sameCard = nonClosedCards?.Count(x => x.CardType == productId.ToString()) ?? 0;
            return new IssuedCardCounts(total, alOusra, sameCard);
        }


        [HttpGet]
        //[NonAction]
        public async Task<CardDefinitionDto> GetCardWithExtension(int productId)
        {
            var cardDefinition = (await _fdrDBContext.CardDefs.AsNoTracking().FirstOrDefaultAsync(cd => cd.CardType == productId))?.Adapt<CardDefinitionDto>();
            cardDefinition!.Extension = (await GetCardDefinitionExtensionsByProductId(productId))?.Data;
            cardDefinition.ProductType = Helpers.GetProductType(cardDefinition.Duality, cardDefinition.MinLimit, cardDefinition.MaxLimit);
            return cardDefinition;
        }
        #region PrivateMethods
        private async Task<List<SupplementaryCardDetail>> LoadSupplementaryByCivilId(string civilId, int? payeeTypeId = null, decimal primaryCardRequestId = 0, CancellationToken cancellationToken = default)
        {

            var preRegisterPayeeQuery = (from payee in _fdrDBContext.PreregisteredPayees.AsNoTracking().Where(x => payeeTypeId == null || x.TypeId == payeeTypeId)//&& x.StatusId == (int)PayeeStatus.ACTEVATED)
                                         from request in _fdrDBContext.Requests.AsNoTracking().Include(x => x.Parameters).AsNoTracking()
                                         where (payee.CardNo == request.CardNo || payee.CardNo == request.RequestId.ToString())
                                         join profile in _fdrDBContext.Profiles on request.CivilId equals profile.CivilId
                                         let isValidCardNumber = IsValidCardNumber(payee.CardNo)
                                         select new SupplementaryCardDetail
                                         {
                                             CivilId = request.CivilId,
                                             SourceRequestId = request.RequestId,
                                             CardNumber = payee.CardNo,
                                             CardNumberDto = payee.CardNo.SaltThis(),
                                             RequestId = !isValidCardNumber ? Convert.ToInt64(payee.CardNo) : null,
                                             FullName = payee.FullName,
                                             StatusId = payee.StatusId,
                                             CardStatus = (CreditCardStatus)request.ReqStatus,
                                             Description = payee.Description,
                                             TypeId = payee.TypeId,
                                             HolderName = profile.HolderName,
                                             Relation = request.Parameters.First(x => x.Parameter == "RELATION").Value,
                                             CardData = new()
                                             {
                                                 ApprovedLimit = request.ApproveLimit ?? 0,
                                                 BankAcctNo = request.AcctNo,
                                                 CardType = request.CardType,
                                                 BranchID = request.BranchId,
                                                 RequestDate = request.ReqDate,
                                                 Remark = request.Remark,
                                                 Expiry = request.Expiry == null || (request.Expiry != null && request.Expiry!.Trim() == "0000") ? null : DateTime.ParseExact(request.Expiry!, ConfigurationBase.ExpiryDateFormat, CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1)
                                             }
                                         }).AsQueryable();


            if (primaryCardRequestId > 0)
            {
                string primaryRequestId = primaryCardRequestId.ToString();
                var supplementaryRequestIds = _fdrDBContext.RequestParameters.Where(x => x.Parameter == "PRIMARY_CARD_REQUEST_ID" && x.Value == primaryRequestId)?
                .Select(rp => new { rp.ReqId });

                if (supplementaryRequestIds.AnyWithNull())
                {
                    preRegisterPayeeQuery = preRegisterPayeeQuery.Where(pre => supplementaryRequestIds.Any(s => pre.SourceRequestId == s.ReqId));
                }
                else
                {
                    return new();
                }
            }
            else
            {
                preRegisterPayeeQuery = preRegisterPayeeQuery.Where(x => x.CivilId == civilId);
            }


            return await preRegisterPayeeQuery.ToListAsync(cancellationToken);


        }

        private async Task<CardDetailsResponse> GetCardInformationInDetail(Request? cardRequest, bool includeCardBalance = true, string? kfhId = null)
        {
            CardDetailsResponse? result = new();

            if (includeCardBalance && cardRequest is not null && cardRequest.CardNo is not null)
            {
                try
                {
                    var balanceStatusCardDetailsResponse = await _creditCardInquiryServiceClient.getBalanceStatusCardDetailAsync(new getBalanceStatusCardDetailRequest()
                    {
                        cardNo = cardRequest.CardNo,
                    });

                    if (balanceStatusCardDetailsResponse == null) return new();

                    result = balanceStatusCardDetailsResponse.getBalanceStatusCardDetailResult.Adapt<CardDetailsResponse>();
                    result.IsExternalStatusLoaded = true;
                }
                catch (System.Exception ex)
                {
                    result.IsCardNotFound = CardNotFoundMessages.Any(msg => ex.Message.ToLower().Contains(msg.ToLower()));

                    if (!result.IsCardNotFound)
                        throw;
                }
            }


            //if (cardRequest.CivilId != result?.CivilID)
            //    throw new ApiException(message: "Invalid request id and found civilId mismatch with integration service !");

            List<Task> tasks =
            [
                BindAUBCardNumber(cardRequest!, result),
                BindDeliveryStatus(cardRequest!, result),
                BindDetailFromRequestParameters(cardRequest!, result, includeCardBalance),
                BindSupplementaryCardsCount(cardRequest!, result),
                BindProductType(cardRequest!, result),
                BindPendingActivities(cardRequest!, result),
                //BindSecondaryCardDetails(balanceStatusCardDetailsResponse),
                //BindCustomerType(balanceStatusCardDetailsResponse)
            ];

            await Task.WhenAll(tasks);

            if (!result.IsCardNotFound && includeCardBalance) //TODO : Create parameter to add eligible actions in the response
                await BindEligibleActions(cardRequest!, result, kfhId);

            await SecureData();

            return result;

            async Task SecureData()
            {

                result.CardNumberDto = result.CardNumber.SaltThis();
                result.AUBCardNumberDto = result.AUBCardNumber.SaltThis();
                result.SecondaryCardNoDto = result.SecondaryCardNo.SaltThis();
                if (result.Parameters is not null)
                    result.Parameters.SecondaryCardNumber = result.Parameters.SecondaryCardNumber.SaltThis();
            }
        }
        private async Task BindSupplementaryCardsCount(Request cardRequest, CardDetailsResponse result)
        {
            result.SupplementaryCardCount = await new SupplementaryCards(cardRequest.RequestId, cardRequest.CardNo!, _fdrDBContext).GetSupplementaryCounts();
        }

        private async Task BindAUBCardNumber(Request cardRequest, CardDetailsResponse result)
        {
            if (!Convert.ToBoolean(cardRequest.IsAUB))
                return;

            var aubMapping = await _fdrDBContext.AubCardMappings.AsNoTracking().FirstOrDefaultAsync(aub => aub.KfhCardNo == cardRequest.CardNo && aub.IsRenewed == false);
            if (aubMapping is not null)
                result.AUBCardNumber = aubMapping.AubCardNo;
        }

        private async Task BindProductType(Request cardRequest, CardDetailsResponse result)
        {
            var cardDef = await _fdrDBContext.CardDefs.FirstOrDefaultAsync(x => x.CardType == cardRequest.CardType);
            if (cardDef is null) return;
            result.ProductType = Helpers.GetProductType(cardDef.Duality, Convert.ToDecimal(cardDef.MinLimit), Convert.ToDecimal(cardDef.MaxLimit));
            result.IssuanceType = Helpers.GetIssuanceType(result.ProductType);
            result.ProductName = cardDef.Name;
            result.Installment = Helpers.GetMinCardLimit(cardDef.Duality, cardDef.Installments, result.ApproveLimit);
            result.PreviousKFHCardLimit = 0;
            result.PrevKFHCardInstallment = 0;
            result.MinLimit = cardDef.MinLimit;
            result.MaxLimit = cardDef.MaxLimit;
        }



        async Task BindEligibleActions(Request cardRequest, CardDetailsResponse result, string? kfhId = null)
        {
            List<ListItemGroup<RequestType>> PendingCardActions = [new(RequestType.DownloadEForm, RequestTypeGroup.General), new(RequestType.Cancel, RequestTypeGroup.Critical)];
            List<ListItemGroup<RequestType>> NonPendingCardActions = [new(RequestType.Detail, RequestTypeGroup.General)];

            bool IsChargeCard = result.ProductType == ProductTypes.ChargeCard;
            bool IsTayseerCard = result.ProductType == ProductTypes.Tayseer;
            bool IsYouthCard = ConfigurationBase.YouthCardTypes.Split(",").Any(x => x == result!.CardType.ToString());
            bool IsPrepaid = result.ProductType == ProductTypes.PrePaid;

            if (result.CardStatus is CreditCardStatus.Lost)
            {
                result.EligibleActionsLoaded = true;
                result.EligibleActions = new();
                //if (await IsAllowedToClose())
                //    result.EligibleActions.Add(new(RequestType.CardClosure, RequestTypeGroup.Critical));
                return;
            }

            //because all the transaction moved to new card for this lost one
            if (result.CardStatus is CreditCardStatus.Closed)
            {
                result.EligibleActionsLoaded = true;
                result.EligibleActions = new();
                if (await IsAllowedCreditReverse())
                    result.EligibleActions.Add(new(RequestType.CreditReverse, RequestTypeGroup.General));

                return;
            }

            if (result.CardStatus is (CreditCardStatus.Pending or CreditCardStatus.PendingForCreditCheckingReview))
            {
                result.EligibleActionsLoaded = true;
                result.EligibleActions = PendingCardActions;
                return;
            }

            if (result.IsExternalStatusLoaded == false)
            {
                result.EligibleActions = NonPendingCardActions;
                return;
            }


            kfhId = _authManager.GetUser()?.KfhId ?? kfhId;

            if (kfhId is null)
                throw new ApiException(message: "Invalid kfhId");

            var userPreference = await _userPreferencesClient.GetUserPreferences(kfhId!);
            _ = int.TryParse(userPreference?.FromUserPreferences().DefaultBranchIdValue, out int userBranchId);
            _ = Enum.TryParse(result.Collateral, ignoreCase: true, out Collateral _collateral);



            #region Eligible Actions
            if (IsAllowMoreDetail())
                result.EligibleActions.Add(new(RequestType.Detail, RequestTypeGroup.General));

            if (await IsAllowedToActivation())
                result.EligibleActions.Add(new(RequestType.Activate, RequestTypeGroup.General));


            if (await IsAllowedReplacementTrackReport())
                result.EligibleActions.Add(new(RequestType.ReplaceTrackReport, RequestTypeGroup.General));

            if (await IsAllowedCreditReverse())
                result.EligibleActions.Add(new(RequestType.CreditReverse, RequestTypeGroup.General));

            if (IsAllowedMigration())
                result.EligibleActions.Add(new(RequestType.Migration, RequestTypeGroup.General));

            if (IsAllowedPayment())
                result.EligibleActions.Add(new(RequestType.CardPayment, RequestTypeGroup.General));

            if (IsAllowedStandingOrder())
                result.EligibleActions.Add(new(RequestType.StandingOrder, RequestTypeGroup.General));



            if (IsAllowedToReActivation())
                result.EligibleActions.Add(new(RequestType.ReActivate, RequestTypeGroup.General));

            if (await IsAllowedToClose())
                result.EligibleActions.Add(new(RequestType.CardClosure, RequestTypeGroup.Critical));

            if (await IsAllowedToReplaceDamage())
                result.EligibleActions.Add(new(RequestType.ReplacementForDamage, RequestTypeGroup.Update));

            if (await IsAllowedToReplaceLostOrStolen())
                result.EligibleActions.Add(new(RequestType.ReplacementForLost, RequestTypeGroup.Update));

            if (IsAllowedToChangeAddress())
                result.EligibleActions.Add(new(RequestType.ChangeAddress, RequestTypeGroup.Update));

            if (IsAllowedToChangeCardHolderName())
                result.EligibleActions.Add(new(RequestType.ChangeCardHolderName, RequestTypeGroup.Update));

            if (IsAllowedToChangeLinkAccountNumber())
                result.EligibleActions.Add(new(RequestType.ChangeLinkAccountNumber, RequestTypeGroup.Update));

            if (IsAllowedToStopCard())
            {
                result.EligibleActions.Add(new(RequestType.ReportLostOrStolen, RequestTypeGroup.Critical));
                result.EligibleActions.Add(new(RequestType.StopCard, RequestTypeGroup.Critical));
            }

            if (await IsAllowedToChangeLimit())
            {
                result.EligibleActions.Add(new(RequestType.ChangeLimit, RequestTypeGroup.Update));
            }

            result.EligibleActionsLoaded = true;
            #endregion

            #region private methods

            async Task<bool> IsAllowedToChangeLimit()
            {
                if (!_authManager.HasPermission(Permissions.ChangeLimit.Request()))
                    return false;

                bool isPrepaidCard = (_collateral is Collateral.PREPAID_CARDS or Collateral.FOREIGN_CURRENCY_PREPAID_CARDS);// (_collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_MARGIN or Collateral.AGAINST_SALARY or Collateral.EXCEPTION);
                if (isPrepaidCard)
                    return false;

                if (result.CardStatus != CreditCardStatus.Active)
                    return false;

                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                //If it is not tayseer or charge card
                if (!(IsChargeCard || IsTayseerCard))
                    return false;

                //we are allowing Delinquent upto 120 days , P mean "60 Days in Delinquent", F mean "Delinquent 120 Days"
                bool isDelinquentChargeOff = (result.InternalStatus == ConfigurationBase.Status_Delinquent || result.InternalStatus == ConfigurationBase.Status_ChargeOff)
                                                 && (result.InternalStatusCode != "P" || result.InternalStatusCode != "F");

                if (isDelinquentChargeOff)
                    return false;

                return true;
            }

            bool IsAllowedToStopCard()
            {
                if (!_authManager.HasPermission(Permissions.StopCard.Request()))
                    return false;

                if (result.CardStatus is CreditCardStatus.Approved)
                    return true;

                if (result.CardStatus is CreditCardStatus.TemporaryClosed or CreditCardStatus.TemporaryClosedbyCustomer)
                    return false;

                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                if (!string.IsNullOrEmpty(result.ExternalStatusCode))
                    return false;

                bool isDelinquentChargeOff = (result.InternalStatus == ConfigurationBase.Status_Delinquent || result.InternalStatus == ConfigurationBase.Status_ChargeOff)
                    && (result.InternalStatusCode != "P" || result.InternalStatusCode != "F");

                bool IsNoPlasticActionWithNonDelinquent = result.PlasticActionEnum == PlasticActions.NoAction && !isDelinquentChargeOff; // check internal status is not delinquent more than 90 days

                if (!IsNoPlasticActionWithNonDelinquent)
                    return false;

                //Lessthan 90
                bool IsLostStolenCardWithNonDelinquent = result.CardBlockStatus == ConfigurationBase.Status_LostOrStolenCode && !isDelinquentChargeOff;
                if (IsLostStolenCardWithNonDelinquent)
                    return false;

                return true;
            }

            bool IsAllowedToChangeCardHolderName()
            {
                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                return true;
            }

            bool IsAllowedToChangeLinkAccountNumber()
            {
                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                return true;
            }

            bool IsAllowedToChangeAddress()
            {

                if (!_authManager.HasPermission(Permissions.ChangeBillingAddress.Request()))
                    return false;

                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                return true;
            }

            bool IsAllowedStandingOrder()
            {
                if (!_authManager.HasPermission(Permissions.StandingOrder.Create()))
                    return false;

                return result.IsAllowStandingOrder;
            }

            bool IsAllowedMigration()
            {
                if (!_authManager.HasPermission(Permissions.MigrateCollateral.Request()))
                    return false;

                if (result.CardStatus == CreditCardStatus.Active && _collateral is (Collateral.AGAINST_SALARY or Collateral.EXCEPTION))
                    return true;

                return false;
            }

            async Task<bool> IsAllowedCreditReverse()
            {
                if (!_authManager.HasPermission(Permissions.CreditReverse.Request()))
                    return false;

                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                var pendingRequest = await GetPendingCreditReverseRequest(result.CardNumber);
                decimal RemainingRefundAmount = (result?.AvailableLimit - result?.Limit) - pendingRequest?.Sum(x => x.Amount) ?? 0;


                bool validRefundBalance = RemainingRefundAmount > 0;

                if (validRefundBalance)
                    return true;

                return false;
            }

            async Task<bool> IsAllowedReplacementTrackReport()
            {
                if (!_authManager.HasPermission(Permissions.ReplacementTrackingReport.View()))
                    return false;


                return await Task.FromResult(true);
            }


            bool IsAllowedPayment()
            {
                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                return _authManager.HasPermission(Permissions.CardPayment.Create());

                //if (result.IsExternalStatusLoaded && result.Balance <= 0)
                //    return true;

                //return false;
            }
            bool IsAllowMoreDetail()
            {
                return result?.CardStatus is not CreditCardStatus.Closed;
            }

            async Task<bool> IsAllowedToActivation()
            {
                if (!_authManager.HasPermission(Permissions.CardActivation.Request()))
                    return false;

                if (result.CardStatus is CreditCardStatus.Approved)
                    return true;

                if (result.CardStatus is CreditCardStatus.Active)
                    return false;

                if (IsYouthCard)
                    return false;

                return result?.AccountActiviationFlag == "Y" && string.IsNullOrEmpty(result?.ExternalStatusCode) && string.IsNullOrEmpty(result?.CardBlockStatus);

                //var cardActivationStatus = result?.AccountActiviationFlag == "Y" && string.IsNullOrEmpty(result?.ExternalStatusCode) && string.IsNullOrEmpty(result?.CardBlockStatus);

                //if (result!.ProductType == ProductTypes.Tayseer)
                //{
                //    if (!string.IsNullOrEmpty(result.SecondaryCardNo))
                //    {
                //        var secondaryCardResponse = await GetMasterSecondaryCardDetails(result.SecondaryCardNo!);
                //        if (secondaryCardResponse.IsSuccess)
                //        {
                //            if (secondaryCardResponse.Data?.ActivationFlag != "Activated")
                //                return true;
                //        }
                //    }
                //}

                //return cardActivationStatus;

            }

            bool IsAllowedToReActivation()
            {
                if (!_authManager.HasPermission(Permissions.CardReActivate.Request()))
                    return false;

                if (IsYouthCard)
                    return false;

                if (!string.IsNullOrEmpty(result?.ExternalStatusCode))
                {
                    if (result.ExternalStatusCode.Equals("X", StringComparison.InvariantCultureIgnoreCase) ||
                        result.ExternalStatusCode.Equals("A", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (result.CardStatus is (CreditCardStatus.TemporaryClosed or CreditCardStatus.Stopped or CreditCardStatus.TemporaryClosedbyCustomer))
                            return true;
                    }
                }

                return false;
            }
            async Task<bool> IsAllowedToReplaceDamage()
            {
                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                //TODO: check the ncard bin and return false
                if (!string.IsNullOrEmpty(result.AUBCardNumber))
                    return false;


                if (!_authManager.HasPermission(Permissions.ReplaceOnDamage.Request()))
                    return false;

                bool isDelinquentNotMoreThan90Days = result.InternalStatusCode != "E" && result.InternalStatusCode != "G" && result.InternalStatusCode != "H" && result.InternalStatusCode != "I" && result.InternalStatusCode != "Z";
                bool isAccountActivated = result.AccountActiviationFlag == "Y" && result.CardStatus == CreditCardStatus.Active && result.PlasticActionEnum == PlasticActions.NoAction;
                bool isNotLostOrStolen = result.CardBlockStatus != ConfigurationBase.Status_LostOrStolenCode;

                bool isAllowed = isAccountActivated && isDelinquentNotMoreThan90Days && isNotLostOrStolen;

                return await Task.FromResult(isAllowed);


                bool isDelinquentChargeOff = result.InternalStatus is ConfigurationBase.Status_Delinquent or ConfigurationBase.Status_ChargeOff && (result.InternalStatusCode != "P" || result.InternalStatusCode != "F");

                bool IsNoPlasticActionWithNonDelinquent = result.PlasticActionEnum == PlasticActions.NoAction && !isDelinquentChargeOff; // check internal status is not delinquent more than 90 days

                if (!IsNoPlasticActionWithNonDelinquent)
                    return false;

                //check internal status is not delinquent more than 90 days
                bool IsLostStolenCardWithNonDelinquent = result.CardBlockStatus == ConfigurationBase.Status_LostOrStolenCode && !isDelinquentChargeOff;
                if (IsLostStolenCardWithNonDelinquent)
                    return false;


                bool IsActivated = result.AccountActiviationFlag == "N" && result.CardStatus == CreditCardStatus.Active && result.PlasticActionEnum == PlasticActions.NoAction;
                //TODO: bool IsAuthorized =  IsAccountBoardingAuthorized() && authManager.InRole(Roles.CardClosureAndLinkedAccountMaker);
                bool IsNotLostStolenWithNonDelinquent = result.CardBlockStatus != ConfigurationBase.Status_LostOrStolenCode && !isDelinquentChargeOff;

                if (!(IsActivated && IsNotLostStolenWithNonDelinquent))
                    return false;


                return await Task.FromResult(true);
            }


            async Task<bool> IsAllowedToReplaceLostOrStolen()
            {

                if (!_authManager.HasPermission(Permissions.ReplaceOnLostStolen.Request()))
                    return false;

                if (!string.IsNullOrEmpty(result?.ExternalStatusCode))
                {
                    if (result.ExternalStatusCode.Equals("X", StringComparison.InvariantCultureIgnoreCase) ||
                        result.ExternalStatusCode.Equals("A", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (result.CardStatus is (CreditCardStatus.TemporaryClosed or CreditCardStatus.Stopped or CreditCardStatus.TemporaryClosedbyCustomer))
                            return true;
                    }
                }


                return await Task.FromResult(false);
            }

            async Task<bool> IsAllowedToClose()
            {

                if (IsYouthCard && !_authManager.HasPermission(Permissions.TAMCardAccess))
                    return false;

                bool isAuthorized = result.IsSupplementaryCard ? _authManager.HasPermission(Permissions.SecondaryCardClosure.Request()) : _authManager.HasPermission(Permissions.PrimaryCardClosure.Request());

                if (!isAuthorized)
                    return false;

                if (result.DelinquentAmount != 0)
                    return false;

                if (result.CardStatus is CreditCardStatus.Approved)
                    return false;

                //TODO: is BCD USER

                //TODO : RestrictClosureOnlyToBCD
                if (result.ExternalStatus == ConfigurationBase.Status_CancelOrClose)
                    return false;

                //check internal status is not delinquent more than 90 days
                if (result.CardBlockStatus == ConfigurationBase.Status_LostOrStolenCode
                    && result.InternalStatus is not ConfigurationBase.Status_Delinquent and ConfigurationBase.Status_ChargeOff)
                    return false;

                if (result.IsSupplementaryCard)
                    return await IsValidSupplementaryToClose();
                else
                    return IsValidPrimaryToClose();
            }

            bool IsValidPrimaryToClose()
            {
                bool isCardNotPrinted = result.PlasticActionEnum == PlasticActions.NoAction;

                bool IsExternalStatusIsEmpty = isCardNotPrinted && result.ExternalStatusCode == "";

                bool IsExternalStatusTemporallyClosed = isCardNotPrinted
                    && (result.ExternalStatus == ConfigurationBase.Status_TemporaryClosed
                    || result.ExternalStatus == ConfigurationBase.Status_AuthorizationProhibited
                    || result.CardBlockStatus == ConfigurationBase.Status_TemporaryClosed);

                if (!IsExternalStatusIsEmpty || IsExternalStatusTemporallyClosed)
                    return false;

                return true;
            }

            async Task<bool> IsValidSupplementaryToClose()
            {

                bool isCardNotPrinted = result.PlasticActionEnum == PlasticActions.NoAction;

                bool IsCardBlockStatusIsEmpty = isCardNotPrinted && result.CardBlockStatus == "";

                bool IsBlockStatusTemporallyClosed = isCardNotPrinted
                    && (result.CardBlockStatus == ConfigurationBase.Status_TemporaryClosed || result.CardBlockStatus == ConfigurationBase.Status_AuthorizationProhibited);

                if (!IsCardBlockStatusIsEmpty || IsBlockStatusTemporallyClosed)
                    return false;

                var supplementaryCards = await GetSupplementaryCardsByRequestId(result.RequestId);
                if (CanDeleteSupplementaryCards(supplementaryCards.Data))
                    return true;

                return true;
            }

            bool CanDeleteSupplementaryCards(List<SupplementaryCardDetail> cards)
            {
                try
                {
                    if (cards.Any(card => card.CardType != ConfigurationBase.AlOsraSupplementaryCardTypeId.ToString()))
                        return true;

                    int totalCards = cards?.Count ?? 0;
                    int totalRejectedCards = cards?.Count(x => x.CardStatus == CreditCardStatus.Rejected) ?? 0;
                    int remainingCards = totalCards - totalRejectedCards;//   Cards.Data.Any(x => x.CardStatus != CreditCardStatus.Closed);
                    return remainingCards > 1;
                }
                catch (System.Exception ex)
                {

                    return false;
                }

            }
            #endregion
        }
        private async Task BindPendingActivities(Request cardRequest, CardDetailsResponse result)
        {
            var pendingActivities = await (from ra in _fdrDBContext.RequestActivities.AsNoTracking()
                                           join req in _fdrDBContext.Requests.AsNoTracking() on ra.RequestId equals req.RequestId
                                           where ra.RequestActivityStatusId == (int)RequestActivityStatus.Pending && req.CardNo == result.CardNumber
                                           select ra).ToListAsync();

            result.PendingActivities = pendingActivities?.DistinctBy(x => x.CfuActivityId)?.ToDictionary(activity => (CFUActivity)activity.CfuActivityId, activity => true);
        }

        private static bool IsValidCardNumber(string value)
        {
            var cardFirstDigit = value[0].ToString();
            bool IsVisaCard = visaCardStartingNumbers.Any(x => x == cardFirstDigit);
            bool isMasterCard = masterCardStartingNumbers.Any(x => x == cardFirstDigit);

            if (value.Length == cardNumberLength && (isMasterCard || IsVisaCard))
                return true;

            return false;
        }
        private int? GetPayeeTypeID(int cardType, RequestParameterDto requestParameter)
        {
            var isPrimaryPrepaidCard = cardType == ConfigurationBase.AlOsraPrimaryCardTypeId;
            var isPrimaryChargeCard = requestParameter.IsSupplementaryOrPrimaryChargeCard == "P";

            if (isPrimaryPrepaidCard)
                return ConfigurationBase.PrimaryPrepaidCardPayeeTypeId;

            if (isPrimaryChargeCard)
                return ConfigurationBase.PrimaryChargeCardPayeeTypeId;

            return null;
        }

        private async Task BindDeliveryStatus(Request cardRequest, CardDetailsResponse response)
        {
            if (cardRequest.CardNo is null) return;
            var cardDeliveryStatus = await _fdrDBContext.RequestDeliveries.AsNoTracking().Where(x => x.Request.CardNo == cardRequest.CardNo).OrderByDescending(x => x.RequestDeliveryId).FirstOrDefaultAsync();
            if (cardDeliveryStatus != null) response.DeliveryStatus = (DeliveryStatus)cardDeliveryStatus.RequestDeliveryStatusId;
        }

        private async Task BindDetailFromRequestParameters(Request cardRequest, CardDetailsResponse response, bool includeCardBalance = true)
        {
            response.Balance = response.Limit - response.AvailableLimit;
            response.CardStatus = (CreditCardStatus)cardRequest.ReqStatus;
            response.CardType = cardRequest.CardType;
            response.BranchId = cardRequest.BranchId;
            response.TellerId = cardRequest.TellerId;
            response.ServicePeriod = cardRequest.ServicePeriod;
            response.ReqDate = cardRequest.ReqDate;
            response.RequestedLimit = cardRequest.RequestedLimit;
            response.Photo = cardRequest.Photo;
            response.ApproveLimit = cardRequest.ApproveLimit ?? 0;
            response.DepositAmount = cardRequest.DepositAmount;
            response.CardNumber = cardRequest.CardNo;
            response.RequestId = (long)cardRequest.RequestId;
            response.Expiry = response.Expiry?.ToExpiryDate(saltIt: true);
            response.CivilId = cardRequest.CivilId;
            response.HolderAddressCity = cardRequest.City;
            response.HolderAddressLine1 = cardRequest.Street;
            response.HolderAddressLine2 = cardRequest.AddressLine1;
            response.HolderAddressLine3 = cardRequest.AddressLine2;
            response.OpenDate = cardRequest.ApproveDate ?? DateTime.MinValue;


            //if (!includeCardBalance)
            //{
            response.BankAccountNumber = cardRequest.AcctNo;
            response.FdrAccountNumber = cardRequest.FdAcctNo;
            //}

            var requestParameters = await GetRequestParameter(cardRequest);
            if (requestParameters == null) return;

            response.Parameters = requestParameters;
            response.Collateral = requestParameters.Collateral;
            response.PcdFlag = requestParameters.PCTFlag;
            response.PromotionName = requestParameters.PromotionName;
            response.IsCorporateCard = requestParameters.Collateral == Collateral.AGAINST_CORPORATE_CARD.ToString();
            response.CorporateCivilId = response.IsCorporateCard ? requestParameters.CorporateCivilId : "";

            int? payeeTypeId = GetPayeeTypeID(cardRequest.CardType, requestParameters);
            response.IsPrimaryCard = payeeTypeId != null;
            response.IsSupplementaryCard = requestParameters.IsSupplementaryOrPrimaryChargeCard == "S" | response.CardType == ConfigurationBase.AlOsraSupplementaryCardTypeId | requestParameters.CardType == ConfigurationBase.SupplementaryChargeCard;
            response.PrimaryCardRequestId = requestParameters.PrimaryCardRequestId?.ToString().Replace(".0", "");
            response.PrimaryCardCivilId = requestParameters.PrimaryCardCivilId;
            response.PrimaryCardHolderName = requestParameters.PrimaryCardHolderName;

            response.ExternalStatusCode = response.ExternalStatus ?? "";
            response.InternalStatusCode = response.InternalStatus ?? "";
            response.CardBlockStatus = response.CardBlockStatus ?? "";
            response.ExternalStatus = GetExternalStatus(response.ExternalStatus, response.CardBlockStatus, response.IsSupplementaryCard, payeeTypeId);
            response.InternalStatus = GetInternalStatus(response.InternalStatus!);

            response.EarlyClosureFees = requestParameters.EarlyClosureFees;
            response.EarlyClosureMonths = requestParameters.EarlyClosureMonths;
            response.EarlyClosurePercentage = requestParameters.EarlyClosurePercentage;
            response.PCTId = requestParameters.PCTId;
            response.Currency = await _CurrencyAppService.GetCardCurrency(response.CardType);
            var allowedCardTypes = (await _configurationAppService.GetValue(ConfigurationBase.SO_AllowedCardTypes))?.Split(",") ?? Array.Empty<string>();

            response.IsAllowStandingOrder = allowedCardTypes.Any(so => so == cardRequest.CardType.ToString());
        }

        private static string GetInternalStatus(string internalStatus)
        {
            string internalStatusCode = internalStatus?.Trim().ToUpper() ?? "";

            internalStatusCode = internalStatusCode switch
            {
                "F" or "G" or "H" or "I" or "P" => "E",
                _ => internalStatusCode
            };

            return internalStatusCode switch
            {
                "D" => ConfigurationBase.Status_InArrears,
                "O" => ConfigurationBase.Status_OverLimit,
                "E" => ConfigurationBase.Status_Delinquent,
                "Z" => ConfigurationBase.Status_ChargeOff,
                "" => ConfigurationBase.Status_Normal,
                _ => "?",
            };
        }
        private static string GetExternalStatus(string? externalStatus, string? cardBlockStatus, bool isSupplementaryChargeCard, int? payeeTypeId)
        {
            bool isPrimaryChargeCard = payeeTypeId == ConfigurationBase.PrimaryChargeCardPayeeTypeId;

            string externalStatusCode = externalStatus?.ToUpper()!;

            if (!string.IsNullOrEmpty(cardBlockStatus) && cardBlockStatus != "L" && isSupplementaryChargeCard)
                externalStatusCode = cardBlockStatus;

            if (isPrimaryChargeCard && externalStatus == "" && cardBlockStatus == "X")
                externalStatusCode = cardBlockStatus;


            externalStatusCode = externalStatusCode switch
            {
                "U" => "L",
                "F" or "G" or "H" or "I" => "E",
                _ => cardBlockStatus == "L" ? "L" : externalStatusCode
            };

            return externalStatusCode switch
            {
                "C" => ConfigurationBase.Status_CancelOrClose,
                "L" => ConfigurationBase.Status_LostOrStolen,
                "X" => ConfigurationBase.Status_TemporaryClosed,
                "A" => ConfigurationBase.Status_AuthorizationProhibited,
                "E" => ConfigurationBase.Status_Delinquent,
                "Z" => ConfigurationBase.Status_ChargeOff,
                "" => ConfigurationBase.Status_Normal,
                _ => "?",
            };
        }
        private async Task<CardDefinitionExtentionDto> GetCardExtensionParameter(Request cardRequest)
        {
            var requestParameter = DictionaryExtension.ConvertKeyValueDataToObject<CardDefinitionExtentionDto>(cardRequest.Parameters.Select(x => new KeyValueTable
            {
                ColumnName = x.Parameter,
                ColumnValue = x.Value!
            }));

            return await Task.FromResult(requestParameter);
        }


        async Task<List<CreditReverseDto>> GetPendingCreditReverseRequest(string cardNumber)
        {
            var pendingRequest = await (from req in _fdrDBContext.Requests.AsNoTracking().Where(x => x.CardNo == cardNumber)
                                        join cr in _fdrDBContext.CreditReverses.AsNoTracking().Where(x => x.Status == 0) on req.RequestId equals cr.ReqId
                                        join pr in _fdrDBContext.Profiles.AsNoTracking() on req.CivilId equals pr.CivilId
                                        select new CreditReverseDto()
                                        {
                                            CardCurrency = cr.CardCurrency,
                                            ID = cr.Id,
                                            RequestID = cr.ReqId,
                                            Amount = cr.Amount,
                                            AmountKDMaker = cr.AmountKdMaker,
                                            AmountKDChecker = cr.AmountKdChecker ?? 0,
                                            RateChecker = cr.RateChecker,
                                            RateMaker = cr.RateMaker,
                                            RequestedBy = cr.RequestedBy,
                                            ApprovedBy = cr.ApprovedBy,
                                            RequestorReason = cr.RequestorReason,
                                            ApproverReason = cr.ApproverReason,
                                            RejectDate = cr.RequestDate,
                                            AppoveDate = cr.ApproveDate,
                                            RequestDate = cr.RejectDate,
                                            Status = cr.Status,
                                            AccountNo = cr.AcctNo,
                                            CustomerNameEn = pr.FullName,
                                            CardNo = req.CardNo,
                                            ApproveLimit = req.ApproveLimit,
                                            CivilID = req.CivilId,
                                            CardType = req.CardType,
                                            CustomerNameAr = pr.ArabicName,
                                            IsLocked = cr.Islocked ?? false
                                        }
                                ).ToListAsync();
            return pendingRequest;
        }
        #endregion
    }
}

