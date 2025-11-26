using Bloc.Models;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.States;

namespace CreditCardsSystem.Web.Client.Cubits;

public class CustomerAccountsCubit : Cubit<CustomerAccountsState>
{
    private readonly IAccountsAppService _accountsAppService;

    public CustomerAccountsCubit(IAccountsAppService accountsAppService) : base(new CustomerAccountsState(""))
    {
        _accountsAppService = accountsAppService;
    }

    public async Task Load(string civilId)
    {
        if (!String.IsNullOrEmpty(civilId))
        {
            Emit(new CustomerAccountsLoading(civilId));
            var accounts = await _accountsAppService.GetAllAccounts(civilId);
            if (accounts.AnyWithNull())
            {
                var accountNumbers = accounts!.Select(e => e.Acct).ToList();
                Emit(new CustomerAccountsLoaded(State.CivilId, accountNumbers));
            }
            else
            {
                Emit(new CustomerAccountsNoData(State.CivilId));
            }
        }
    }
}