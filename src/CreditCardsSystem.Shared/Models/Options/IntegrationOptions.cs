using Kfh.Aurora.Integration.Models;

namespace CreditCardsSystem.Domain.Models.Options;

public class IntegrationOptions
{
    public const string Integration = "Integration";
    public bool BypassSslValidation { get; set; }
    public ClientOptions Client { get; set; } = default!;
    public ServiceEndpoints Endpoints { get; set; } = default!;
}


public class DocuwareOptions
{
    public const string Section = "Docuware";

    public string? ApiKey { get; set; }
    public string? ApplicationId { get; set; }
    public string? WindowId { get; set; }
    public string? CabinetName { get; set; }
    public string? UserId { get; set; }
    public string? AllowedExtensions { get; set; }
}
