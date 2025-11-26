using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Entities.Admin
{
    public class CardType
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [StringLength(250)]
        public string Name { get; set; } = default!;

        [StringLength(250)]
        public string NameAr { get; set; } = default!;

        public string Type { get; set; } = default!;

        public byte[]? Image { get; set; }

        public string? FileName { get; set; }

        public string? Extension { get; set; }

        public bool IsActive { get; set; }
    }
}
