using CreditCardsSystem.Domain.Shared.Enums;

namespace CreditCardsSystem.Domain.Shared.Models.BCDPromotions.ConfigParameter
{
    public class ConfigParameterDto
    {
        public decimal Id { get; set; }

        public string ParamName { get; set; } = string.Empty;

        public string ParamValue { get; set; } = string.Empty;

        public decimal ParamType { get; set; }

        public string? ParamDesc { get; set; }

        public ConfigParameterTypes ConfigParameterType => (ConfigParameterTypes)ParamType;
    }
}
