namespace CreditCardsSystem.Domain.Models.Reports;


public class SupplementaryRequestDetailsDTO : RequestDto
{
    public string RelationID { get; set; }
    public string SpendingLimit { get; set; }
    public string cardType_ { get; set; }

    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    public string LastName { get; set; }
    public string HolderName { get; set; }


    public string RelationText { get; set; }

    public new string Photo
    {
        get
        {
            if (base.Photo) return "True";
            else return "False";
        }
    }




    public int CardStatus = 0;
    public string PrimaryCivilID = "";
    public string PrimaryCardNo = "";
    public int cardTypeID = 0;
    public string ExternalStatus = "";
    public string InternalStatus = "";
    public string CardBlockStatus = "";
    private string _InsertStatus = "";

    public string InsertStatus
    {
        get { return _InsertStatus; }
        set { _InsertStatus = value; }
    }

    private string _DelegateStatus = "";

    public string DelegateStatus
    {
        get { return _DelegateStatus; }
        set { _DelegateStatus = value; }
    }



    private int _IsKFHCustomer = 0;

    public int KFHCustomer
    {
        get
        {
            return _IsKFHCustomer;
        }
        set
        {
            _IsKFHCustomer = value;
        }
    }

    private int _CustomerClassCode = 0;

    public int CustomerClassCode
    {
        get
        {
            return _CustomerClassCode;
        }
        set
        {
            _CustomerClassCode = value;
        }
    }

    public string PCTFlag { get; set; } = "";
    public string bcdFlag { get; set; } = "-";
    public string promotionName { get; set; } = "-";
    public int Service_No { get; set; }
    public int Service_No_Months { get; set; }
    public int collateralId { get; set; }
    public int pctId { get; set; }
    public double earlyClosurePercentage { get; set; }
    public double earlyClosureFees { get; set; }

    public double earlyClosureMonths { get; set; }
}