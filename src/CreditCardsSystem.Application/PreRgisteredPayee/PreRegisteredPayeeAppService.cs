using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Utility.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.PreRgisteredPayee
{
    public class PreRegisteredPayeeAppService : BaseApiResponse, IPreRegisteredPayeeAppService, IAppService
    {
        private readonly FdrDBContext _fdrDBContext;
        private readonly ICardDetailsAppService _cardDetailsAppService;
        private static readonly string[] visaCardStartingNumbers = ConfigurationBase.VisaCardStartingNumbers.Split(',', StringSplitOptions.TrimEntries);
        private static readonly string[] masterCardStartingNumbers = ConfigurationBase.MasterCardStartingNumbers.Split(',', StringSplitOptions.TrimEntries);
        private static readonly int cardNumberLength = ConfigurationBase.CreditCardNumberLength;
        public PreRegisteredPayeeAppService(FdrDBContext fdrDbContext, ICardDetailsAppService cardDetailsAppService)
        {
            _fdrDBContext = fdrDbContext;
            _cardDetailsAppService = cardDetailsAppService;
        }

        [NonAction]
        public async Task<bool> UpdatePreregisteredPayee(CardDetailsResponse cardRequest)
        {
            PreregisteredPayee? payee = await _fdrDBContext.PreregisteredPayees.FirstOrDefaultAsync(x => x.CardNo == cardRequest.CardNumber);
            if (payee == null) return false;

            var customerProfile = await _fdrDBContext.Profiles.FirstOrDefaultAsync(x => x.CivilId == cardRequest.CivilId);
            var productTypeId = await _cardDetailsAppService.GetPayeeProductType(cardRequest.CardType);
            payee.FullName = customerProfile?.FullName ?? "";
            payee.StatusId = ConfigurationBase.SupplementaryCardCloserStatusId;
            payee.TypeId = productTypeId;
            await _fdrDBContext.SaveChangesAsync();

            return true;
        }

        [NonAction]
        public async Task<bool> AddPreregisteredPayee(PreregisteredPayee payee)
        {

            await _fdrDBContext.PreregisteredPayees.AddAsync(
                new PreregisteredPayee()
                {
                    CivilId = payee.CivilId,
                    CardNo = payee.CardNo,
                    FullName = payee.FullName,
                    Description = payee.Description,
                    StatusId = payee.StatusId,
                    TypeId = payee.TypeId,
                    CreationDate = DateTime.Parse(DateTime.Now.ToString("dd-MMM-yy"))
                });
            var save = await _fdrDBContext.SaveChangesAsync();
            return save > 0;

        }

        [NonAction]
        public async Task<IEnumerable<PreregisteredPayee>> GetPreregisteredPayeeByCardNumber(string cardNumber)
        {
            var payees = await (from p in _fdrDBContext.PreregisteredPayees.AsNoTracking().Where(x => x.CardNo == cardNumber)
                                join ps in _fdrDBContext.PreregisteredPayeeStatuses.AsNoTracking() on p.StatusId equals ps.StatusId
                                join pt in _fdrDBContext.PreregisteredPayeeTypes.AsNoTracking() on p.TypeId equals pt.TypeId
                                select p).ToListAsync();
            return payees;
        }

        [HttpGet]
        public async Task<ApiResponseModel<List<SupplementaryCardDetail>>> GetPreregisteredPayeeByCivilId(string civilId, int? payeeTypeId = null, decimal primaryCardRequestId = 0)
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
                                             CardNumberDto = payee.CardNo.Masked(6, 6),
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
                    return Success(new List<SupplementaryCardDetail>());
                }
            }
            else
            {
                preRegisterPayeeQuery = preRegisterPayeeQuery.Where(x => x.CivilId == civilId);
            }


            return Success(await preRegisterPayeeQuery.ToListAsync());


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
    }
}
