using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CreditCardsSystem.Utility.DependencyInjection;

public static class DiHelpers
{
    public static IServiceCollection ReplaceTransient<TService, TImplementation>(this IServiceCollection services)
     where TService : class
     where TImplementation : class, TService
    {
        return services.Replace(ServiceDescriptor.Transient<TService, TImplementation>());
    }
}