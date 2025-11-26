using CreditCardsSystem.Domain.Models.CardIssuance;

namespace CreditCardsSystem.Domain.Common;



public class AccountDetailComparer : IEqualityComparer<AccountDetailsDto>
{
    public bool Equals(AccountDetailsDto? x, AccountDetailsDto? y)
    {
        //First check if both object reference are equal then return true
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        //If either one of the object refernce is null, return false
        if (x is null || y is null)
        {
            return false;
        }

        //Comparing all the properties one by one
        return x.Acct == y.Acct;
    }

    public int GetHashCode(AccountDetailsDto obj)
    {
        //If obj is null then return 0
        if (obj == null)
        {
            return 0;
        }

        //Get the ID hash code value
        int IDHashCode = obj.Acct.GetHashCode();

        ////Get the Name HashCode Value
        //int NameHashCode = obj.Name == null ? 0 : obj.Name.GetHashCode();

        return IDHashCode;
    }
}
