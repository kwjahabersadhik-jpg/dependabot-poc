namespace CreditCardsSystem.Domain.Models.BCDPromotions.Requests
{
    public class RequestDto<T>
    {

        public int ActivityForm { get; set; }
        public int ActivityType { get; set; }
        public T NewData { get; set; } = default!;
        public T OldData { get; set; } = default!;

        public string Title { get; set; }
        public string Description { get; set; }
    }
}
