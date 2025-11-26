namespace CreditCardsSystem.Domain.Models.Promotions;


public class CreditCardPromotionDto
{
    public int cardType { get; set; }
    public int promotionId { get; set; }
    public string promoName { get; set; }
    public string promoDescription { get; set; }
    public string flag { get; set; }
    public DateTime startDate { get; set; }
    public bool startDateSpecified { get; set; }
    public DateTime endDate { get; set; }
    public bool endDateSpecified { get; set; }
    public string pctFlag { get; set; }
    public int noOfWavedMonths { get; set; }
    public float fees { get; set; }
    public string pctDescription { get; set; }
    public int isStaff { get; set; }
    public int serviceNo { get; set; }
    public string serviceDescription { get; set; }
    public int numberOfMonths { get; set; }
    public int collateralId { get; set; }
    public string earlyClosurePercentage { get; set; }
    public string earlyClosureFees { get; set; }
    public string earlyClosureMonths { get; set; }
    public string pctId { get; set; }
    public bool isSupplementaryChargeCard { get; set; }
}
