namespace CreditCardsSystem.Web.Client.Components
{
    public class ComponentMetadata
    {
        public Type? T { get; set; }
        //public string? Name { get; set; }
        public Dictionary<string, object> Parameters { get; set; } =
            new Dictionary<string, object>();
    }
}
