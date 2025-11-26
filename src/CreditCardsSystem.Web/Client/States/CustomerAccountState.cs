using Bloc.Models;

namespace CreditCardsSystem.Web.Client.States;

public record CustomerAccountsState(string CivilId) : BlocState;

public record CustomerAccountsLoading(string CivilId) : CustomerAccountsState(CivilId);

public record CustomerAccountsLoaded(string CivilId, List<string> CustomerAccountNumbers) : CustomerAccountsState(CivilId);

public record CustomerAccountsNoData(string CivilId) : CustomerAccountsState(CivilId);

public record CustomerAccountsError(string CivilId) : CustomerAccountsState(CivilId);