using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.CardDelivery;
using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CreditCardsSystem.Application.CardInquiry
{
    public class CardInquiryAppService(IConfiguration configuration) : BaseApiResponse, ICardInquiryAppService, IAppService
    {
        private readonly string _fdrConnectionString = configuration.GetConnectionString("FdrOracleConnection")!;

        [HttpGet]
        public async Task<ApiResponseModel<List<CardInquiryDto>>> Inquiry(string civilId)
        {

            await using var conn = new OracleConnection(_fdrConnectionString);

            if (conn.State == ConnectionState.Closed)
            {
                await conn.OpenAsync();
            }

            var result = (await conn.QueryAsync<CardInquiryDto>(@"select SUBSTR(F.CARDNUMBER,0,6) ||  'XXXXXX' || SUBSTR(F.CARDNUMBER,13,4) as CardNumber,
                        case TO_NUMBER(F.ACCOUNT_ORG_ID) when 786 then  (TO_NUMBER(F.AMOUNTDELINQUENT) /1000) ELSE (TO_NUMBER((F.AMOUNTDELINQUENT) /100)) END  as AmountDelinquent  , 
                        F.DAYSDELINQUENT as DaysDelinquent , 
                        case TO_NUMBER(F.ACCOUNT_ORG_ID) when 786 then  (TO_NUMBER(F.PAYMENTDUE) /1000) ELSE (TO_NUMBER((F.PAYMENTDUE) /100)) END
                        as PaymentDue , case TO_NUMBER(F.ACCOUNT_ORG_ID) when 786 then (TO_NUMBER(F.CURRENTBALANCE)  /1000) ELSE (TO_NUMBER(F.CURRENTBALANCE)  /100) END as CurrentBalance , R.Card_Type as CardType , 
                        CD.fees as CardFees , 
                        R.Approve_limit as ApprovedLimit , R.CIVIL_ID as CivilId,
                        R.REQ_STATUS  AS CardStatus
                        From FDR.FCMF F 
                        inner join Request R on (R.card_no = f.cardnumber)
                        inner join card_def CD on (CD.Card_type = R.Card_Type)
                        where trim(F.CIVIL_ID) = :civilId AND R.REQ_STATUS in (select * from FDR.split_String(:REQ_STATUS,','))", new { civilId, REQ_STATUS = ConfigurationBase.AllowedCreditCardsStatus })).ToList();

            return Success(result);
        }

    }
}
