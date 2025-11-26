namespace CreditCardsSystem.Domain.Models.Reports
{

    public class DocumwareBaseEntity
    {
        public string? AppID { get; set; }
        public string? CallingAppUserID { get; set; }
        public string? WindowID { get; set; }
    }

    public class StoreBytes : DocumwareBaseEntity
    {
        public string? Extension { get; set; }
        public string? KeyValueList1 { get; set; }
        public string? KeyValueList2 { get; } = "[{\"key\":\"\",\"value\":\"\"}]";
        public string? sClientMachineIP { get; set; }
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }
    }


    public class GetAllDocumentRequest : DocumwareBaseEntity
    {
        public string[] sKeys { get; set; }
        public string[] sValues { get; set; }
    }



    public class DocuwareDocumentDto
    {
        public string CabinetName { get; set; }
        public string Type { get; set; }
        public string DOCID { get; set; }
    }

    public class DocumentFileRequest : DocumwareBaseEntity
    {
        public string DocId { get; set; }
    }

}
