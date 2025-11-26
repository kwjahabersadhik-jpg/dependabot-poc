using Autofac;
using System.Reflection;

namespace CreditCardsSystem.ExternalService;

public class ExternalServiceModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assemply = Assembly.GetExecutingAssembly();
        builder.RegisterAssemblyTypes(assemply).AsImplementedInterfaces();
    }
}