using Bloc.Models;

namespace CreditCardsSystem.Web.Client.States;


public record SharedParameterState(IDictionary<string, object> Data) : BlocState
{
    public T? GetParameter<T>(string key) where T : class
    {
        if (Data.TryGetValue(key, out var value))
        {
            return value as T;
        }
        return null;
    }
}