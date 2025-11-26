using Bloc.Models;
using Kfh.Aurora.ReportHistory.Models;

namespace CreditCardsSystem.Web.Client.States;

public abstract record PrintHistoryState(List<ReportHistoryResult> Reports) : BlocState;

public record PrintHistoryInitialState() : PrintHistoryState(new List<ReportHistoryResult>());

public record PrintHistoryLoadingState() : PrintHistoryState(new List<ReportHistoryResult>());

public record PrintHistoryLoadedState(List<ReportHistoryResult> Reports) : PrintHistoryState(Reports);

public record PrintHistoryErrorState(string ErrorMessage) : PrintHistoryState(new List<ReportHistoryResult>());