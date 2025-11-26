namespace CreditCardsSystem.Domain.Entities;

public class Cache
{
    public string Id { get; set; } = null!;
    public byte[] Value { get; set; } = null!;
    public DateTimeOffset ExpiresAtTime { get; set; }
    public long? SlidingExpirationInSeconds { get; set; }
    public DateTimeOffset? AbsoluteExpiration { get; set; }
}