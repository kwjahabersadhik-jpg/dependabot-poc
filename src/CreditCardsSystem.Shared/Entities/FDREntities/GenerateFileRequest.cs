namespace CreditCardsSystem.Data.Models
{
    using CreditCardsSystem.Domain.Enums;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_GENERATE_FILE")]
    public partial class GenerateFileRequest
    {
        [Key, Column("ID")]
        public int Id { get; set; }

        [Column("GENERATE_FOR")]
        public string GenerateFor
        {
            get
            {
                return RequestType.ToString();
            }
            private set
            {
                RequestType = (GenerateFileRequestType)Enum.Parse(typeof(GenerateFileRequest), GenerateFor);
            }
        }

        [NotMapped]
        public GenerateFileRequestType RequestType { get; set; }

        [Column("LOAD_ID")]
        public int LoadId { get; set; }

        [Column("IS_NEW_REQUEST")]
        public bool IsNewRequest { get; set; }

        [Column("TIME_STAMP")]
        public DateTime CreateOn { get; set; }
    }
}
