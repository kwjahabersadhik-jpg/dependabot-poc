using Bloc.Models;
using CreditCardsSystem.Web.Client.States;
using Kfh.Aurora.Common.Shared.Interfaces.Print;

namespace CreditCardsSystem.Web.Client.Cubits;

public class PrintHistoryCubit : Cubit<PrintHistoryState>
{
    private readonly string _genericErrorMessage = "Failed to load data";
    private readonly IPrintHistoryCommonApi _printHistoryCommonApi;

    public PrintHistoryCubit(IPrintHistoryCommonApi printHistoryCommonApi) : base(new PrintHistoryInitialState())
    {
        _printHistoryCommonApi = printHistoryCommonApi;
    }

    public async void Load()
    {
        try
        {
            Emit(new PrintHistoryLoadingState());
            var reportsHistory = await _printHistoryCommonApi.GetPrintHistory();
            if (reportsHistory is null || !reportsHistory.Success || reportsHistory.Result is null)
            {
                var errorMessage = reportsHistory?.ErrorDescription ?? _genericErrorMessage;
                Emit(new PrintHistoryErrorState(errorMessage));
            }
            else
            {
                Emit(new PrintHistoryLoadedState(reportsHistory!.Result!));
            }
        }
        catch (Exception e)
        {
            Emit(new PrintHistoryErrorState(_genericErrorMessage));
        }
    }
}
