using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Workflow
{
    public class UploadFilesDTO
    {
        [RegularExpression(@"^[a-zA-Z\d\s]{1,250}$", ErrorMessage = "Invalid Id")]
        public string Id { get; set; }

        [RegularExpression(@"^[a-zA-Z\d\s]{1,250}$", ErrorMessage = "Invalid Name")]
        public string Name { get; set; }

        [RegularExpression(@"^[a-zA-Z\d\s]{1,10}$", ErrorMessage = "Invalid Extension")]
        public string Extension { get; set; }
        public long Size { get; set; }
        public bool InvalidExtension { get; set; }
        public byte[]? FileData { get; set; }
    }
}
