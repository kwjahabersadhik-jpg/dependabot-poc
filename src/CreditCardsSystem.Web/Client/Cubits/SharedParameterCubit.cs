using Bloc.Models;
using CreditCardsSystem.Web.Client.States;

namespace CreditCardsSystem.Web.Client.Cubits;

public class SharedParameterCubit : Cubit<SharedParameterState>
{
    public SharedParameterCubit() : base(new(new Dictionary<string, dynamic>()))
    {
    }

    public void UpdateParameter(string key, object data)
    {
        IDictionary<string, object> currentParams = State.Data;
        try
        {
            currentParams[key] = data;
        }
        finally
        {
            Emit(new SharedParameterState(currentParams));
        }
    }

    public void AddOrUpdateParameter<T>(string key, object data) where T : class
    {
        IDictionary<string, object> currentParams = State.Data;
        if (!State.Data.ContainsKey(key))
        {
            currentParams.Add(key, data);
        }

        else
        {
            (currentParams[key]) = data;
        }

        Emit(new SharedParameterState(currentParams));
    }

    public void RemoveParameter(string key)
    {
        IDictionary<string, object> currentParams = State.Data;
        try
        {
            currentParams.Remove(key);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        Emit(new SharedParameterState(currentParams));
    }

    public void Dispose()
    {
        Emit(new SharedParameterState(new Dictionary<string, object>()));
    }
}
