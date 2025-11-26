using CreditCardsSystem.Domain.Models.CardDelivery;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace CreditCardsSystem.Application.CardDelivery
{
    public class CardDeliveryAppService : ICardDeliveryAppService
    {
        private readonly string _cardManagementConnectionString;
        public CardDeliveryAppService(IConfiguration configuration)
        {
            _cardManagementConnectionString = configuration.GetConnectionString("CardManagement")!;
        }
        public async Task<List<CardDeliveryDto>?> GetCardDelivery(string civilId)
        {
            if (string.IsNullOrEmpty(civilId) || !Regex.Match(civilId, @"\d{1,12}").Success)
                return null;

            var query = GetDebitCardDeliveryQuery();

            await using var conn = new SqlConnection(_cardManagementConnectionString);

            if (conn.State == ConnectionState.Closed)
            {
                await conn.OpenAsync();
            }

            var result = await conn.QueryAsync<CardDeliveryDto>(query, new { civilId });
            return result.ToList();
        }

        private static string GetDebitCardDeliveryQuery()
        {
            return @"select FinalResult.*,
            case when FinalResult.CardActionClassification_ID in (1, 5) then FinalResult.DeliveryStatusEN else (DeliveryStatusEN + ' - ' + DeliveredByEN) end as 'DeliveryStatusAndMethodEN',
            case when FinalResult.CardActionClassification_ID in (1, 5) then FinalResult.DeliveryStatusAR else (DeliveryStatusAR + ' - ' + DeliveredByAR) end as 'DeliveryStatusAndMethodAr'
           from
           (
           SELECT 
                TempTable3.*,
                TempTable4.CourierReturnDate, 
                TempTable4.SendToBranchAfterReturnDate, 
                TempTable4.SendToBranchReasonAR, 
                TempTable4.SendToBranchReasonEN, 
                TempTable4.CourierReturnReasonAR, 
                TempTable4.CourierReturnReasonEN
            FROM
            (
                SELECT 
                    Card.CreditCardActionMaster.FileReference_ID, 
                    Files.CCEF.CardNumber AS CardNumber, --CardNumber,
                    CE.CivilID,
			        CE.CardType,
                    Files.FileReference.LoadDate,-- issued date 
                    Card.CreditCardActionMaster.ActionDate AS DeliveryDate, --DeliveryDate,                        
                    Metadata.CardActionClassification.NameEN as DeliveryStatusEN,
                    Metadata.CardActionClassification.NameAR as DeliveryStatusAR,
                    --(Metadata.CardActionClassification.NameEN + ' - ' + Metadata.CardDeliveryMethod.NameEN) AS DeliveryStatusEN, --Status
                    --(Metadata.CardActionClassification.NameAR + ' - ' + Metadata.CardDeliveryMethod.NameAR) AS DeliveryStatusAR,            
                    Admin.Courier.NameEN AS CourierNameEN, --CoureirName
                    Admin.Courier.NameAR AS CourierNameAR, --CoureirName
                    TempTable1.BranchName, --BranchName
                    Admin.CreditCardTypes.NameEN,--credit card type
                    Admin.CreditCardTypes.NameAR,--credit card type
                    Card.CreditCardActionMaster.CardActionClassification_ID, 
                    Card.CreditCardActionMaster.CardDeliveryMethod_ID,
                    Metadata.CardDeliveryMethod.NameEN AS DeliveredByEN,
                    Metadata.CardDeliveryMethod.NameAR AS DeliveredByAR, 
                    Card.CreditCardActionMaster.SendBy AS ReceivedBy, 
                    TempTable1.CCEF_ID,  
                    Admin.Reason.NameEN AS IssuanceReasonEN,
                    (select top 1 r.CreateDate from card.CreditCardDeliveryCancelReturn r where r.CreditCardActionMaster_ID=Card.CreditCardActionMaster.ID  order by r.CreateDate desc) 
                    as CancelDeliveryOn
                FROM
                    Card.CreditCardActionMaster 
                    INNER JOIN 
                        Metadata.CardActionClassification 
                        ON Card.CreditCardActionMaster.CardActionClassification_ID = Metadata.CardActionClassification.ID 
                    Left Join
                        Files.FileReference
                        ON Files.FileReference.ID = Card.CreditCardActionMaster.FileReference_ID
                    
                    Left Outer Join 
                        Admin.Courier 
                        ON Card.CreditCardActionMaster.Courier_ID = Admin.Courier.ID

                    Left Outer Join 
                        Metadata.CardDeliveryMethod 
                        ON Card.CreditCardActionMaster.CardDeliveryMethod_ID = Metadata.CardDeliveryMethod.ID            
                    INNER JOIN 
                            (
                                select 
                                [CreditCardActionMaster_ID], 
                                [CCEF_ID], 
                                '' AS BranchName 
                                from [Card].[CreditCardPrint]

                                UNION

                                    select 
                                    [CreditCardActionMaster_ID], 
                                    [CCEF_ID], 
                                    '' AS BranchName 
                                    from [Card].[CreditCardCancel]

                                UNION 
                                    select 
                                    card.CreditCardDeliveryCancelReturn.CreditCardActionMaster_ID,
                                    [Card].[CreditCardDelivery].CCEF_ID,
                                    Branch.NameEN as BranchName 
                                    from card.CreditCardDeliveryCancelReturn 
                                    ----------------------------
                                Inner join card.CreditCardActionMaster on card.CreditCardActionMaster.ID = CreditCardDeliveryCancelReturn.CreditCardActionMaster_ID
                                left join admin.Branch on card.CreditCardActionMaster.Branch_ID = admin.Branch.ID
                                -----------
                                inner join 
                                [Card].[CreditCardDelivery] 
                                on card.CreditCardDeliveryCancelReturn.CreditCardDelivery_ID= [Card].[CreditCardDelivery].ID

                                UNION
                                select 
                                [CreditCardActionMaster_ID], 
                                [CCEF_ID], Admin.Branch.NameEN AS BranchName from [Card].[CreditCardDelivery]
                                Left Outer Join Admin.Branch ON [Card].[CreditCardDelivery].Branch_ID = Admin.Branch.ID

                            )TempTable1 ON Card.CreditCardActionMaster.ID = TempTable1.CreditCardActionMaster_ID

                    INNER JOIN Files.CCEF ON TempTable1.CCEF_ID = Files.CCEF.ID
                    INNER JOIN Card.CardExtension CE on CE.CardNumber = Files.CCEF.CardNumber
                    Left Outer Join 
                        Admin.CreditCardTypes
                        ON CreditCardTypes.ID = Files.CCEF.CardType_ID            
                    INNER JOIN Admin.Reason ON Files.CCEF.Reason_ID = Admin.Reason.ID 
                    WHERE CE.CivilID = @civilId
            )TempTable3
            Left Outer Join
            (
                SELECT        
                    Card.CreditCardActionMaster.ID, 
                    Card.CreditCardActionMaster.FileReference_ID, 
                    TempTable2.CCEF_ID, 
                    Card.CreditCardActionMaster.ActionDate AS CourierReturnDate,
                    Card.CreditCardActionMaster.ActionDate AS SendToBranchAfterReturnDate,
                    R1.NameAR AS CourierReturnReasonAR, 
                    R1.NameEN AS CourierReturnReasonEN,
                    R2.NameAR AS SendToBranchReasonAR, 
                    R2.NameEN AS SendToBranchReasonEN,
                    Card.CreditCardActionMaster.Branch_ID,
                    TempTable2.CreateDate as CancelDeliveryOn,TempTable2.CreateUser_ID as CancelDeliveryBy

                FROM
                    Card.CreditCardActionMaster 
                    INNER JOIN  Admin.Reason AS R1 ON Card.CreditCardActionMaster.CourierReturnReason_ID = R1.ID 
                    INNER JOIN  Admin.Reason AS R2 ON Card.CreditCardActionMaster.SendToBranchReason_ID = R2.ID 
                    INNER JOIN 
                            (
                                Select [Card].[CreditCardDeliveryCancelReturn].[CreditCardActionMaster_ID],[Card].[CreditCardDelivery].[CCEF_ID],
                                [Card].[CreditCardDeliveryCancelReturn].CreateDate,[Card].[CreditCardDeliveryCancelReturn].CreateUser_ID
                                From [Card].[CreditCardDeliveryCancelReturn] 
                                inner join [Card].[CreditCardDelivery] on [Card].[CreditCardDeliveryCancelReturn].CreditCardDelivery_ID = CreditCardDelivery.ID
                            )TempTable2 ON Card.CreditCardActionMaster.ID = TempTable2.CreditCardActionMaster_ID
            )TempTable4
            ON TempTable3.FileReference_ID = TempTable4.FileReference_ID and TempTable3.CCEF_ID = TempTable4.CCEF_ID and TempTable3.CardActionClassification_ID = 2
        ) FinalResult
        ORDER BY CardNumber, DeliveryDate";
        }
    }
}
