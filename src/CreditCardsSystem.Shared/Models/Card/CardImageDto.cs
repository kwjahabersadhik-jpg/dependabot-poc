namespace CreditCardsSystem.Domain.Models.Card
{
    public class CardTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string NameAr { get; set; } = default!;
        public string Type { get; set; } = default!;
        public byte[]? Image { get; set; }
        public string? FileName { get; set; }
        public string? Extension { get; set; }
        public bool IsActive { get; set; }
        public int ProductId { get; set; } = default!;
    }


    public class CardImage
    {
        public string CardType { get; set; }
        public string? Extension { get; set; }
        public string ImageBase64 { get; set; }
    }
}
