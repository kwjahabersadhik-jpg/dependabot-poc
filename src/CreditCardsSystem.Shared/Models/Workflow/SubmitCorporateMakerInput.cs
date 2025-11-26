using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Workflow;


public class SubmitCorporateMakerInput
{
    public Guid InstanceId { get; set; }

    public Guid TaskId { get; set; }

    public decimal RequestActivityId { get; set; }

    [RegularExpression(@"^[a-zA-Z\d\s]{1,50}$", ErrorMessage = "Invalid InstallmentFromId")]
    public string InstallmentFromId { get; set; } = default!;

    [RegularExpression(@"^[a-zA-Z\d\s]{1,50}$", ErrorMessage = "Invalid InstallmentTypeId")]
    public string InstallmentTypeId { get; set; } = default!;

    public bool? IsVisaMcRequired { get; set; }

    public List<UploadFilesDTO> UploadFiles { get; set; } = default!;
}


public class ApproveCardRequestInput
{
    public decimal RequestActivityId { get; set; }

}