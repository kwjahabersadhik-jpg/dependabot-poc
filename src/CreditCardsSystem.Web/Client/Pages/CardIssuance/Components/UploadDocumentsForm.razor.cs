using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Reports;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardIssuance.Components
{
    public partial class UploadDocumentsForm
    {
        [Inject] public IRequestAppService RequestAppService { get; set; }
        [Inject] public IDocumentAppService DocumentAppService { get; set; }


        [Parameter, SupplyParameterFromQuery]
        public decimal? RequestId { get; set; }



        private List<DocumentDto> Documents { get; set; }
        private bool IsFileUploaded { get; set; }
        private bool IsFileValid { get; set; }
        private List<string> AllowedExtensions { get; set; } = Enum.GetNames<FileExtension>().Select(x => "." + x.ToString()).ToList();

        async Task Submit()
        {
            if (RequestId is null)
            {
                Notification.Failure("Please set RequestId in component parameter");
                return;
            }

            if (Documents is null || Documents.Count == 0)
            {
                Notification.Failure("There is no files to upload!");
                return;
            }

            var request = await RequestAppService.GetRequestDetail((decimal)RequestId!);

            await UploadDoc(Documents);

            //Retry upload only failed documents
            var failedDocuments = Documents.Where(x => x.IsUploaded == false);
            await UploadDoc(failedDocuments);

            Documents.RemoveAll(x => x.IsUploaded == true);
            failedDocuments = Documents.Where(x => x.IsUploaded == false);

            if (failedDocuments.Any())
                Notification.Failure($"Failed to upload documents{string.Join(",", failedDocuments.Select(x => x.FileName).ToArray())}!");
            else
                Notification.Success("Successfully uploaded!");

            async Task UploadDoc(IEnumerable<DocumentDto> documents)
            {
                foreach (var item in documents)
                {
                    Notification.Loading($"Uploading {item.FileName}");
                    try
                    {
                        Domain.Models.Reports.DocumentFileAttributes attribute = new()
                        {
                            Type = "NewCardDocument",
                            Description = item.FileName,
                            FileName = item.FileName,
                            Extension = item.FileExtension,
                            FileBytes = item.Content,
                            KfhId = authManager.GetUser()?.KfhId,
                            RequestId = RequestId.ToString(),
                            FileServer = FileServer.Docuware //This file will stored in the docuware and not in arora file server
                        };


                        attribute.MetaData = new()
                        {
                            { "CIVIL_ID", request.Data?.CivilId! },
                            { "REQ_ID", request.Data?.RequestId.ToString("0")??"" },
                            { "CREATION_DATE", request.Data?.ReqDate! }
                        };

                        await DocumentAppService.UploadDocuments(attribute);
                        item.IsUploaded = true;
                    }
                    catch (Exception ex)
                    {
                        item.IsUploaded = false;
                        item.Message = ex.Message;
                    }
                }
            }
        }

        private async Task SelectedFiles(FileSelectEventArgs args)
        {
            if (args.Files.Count == 0)
                return;


            var file = args.Files.FirstOrDefault();
            var byteArray = new byte[file.Size];
            await using MemoryStream ms = new MemoryStream(byteArray);
            await file.Stream.CopyToAsync(ms);
            var fileByteArray = ms.ToArray();
            IsFileValid = Helpers.IsFileHeaderValid(fileByteArray);
            IsFileUploaded = IsFileValid;

            if (!IsFileValid)
                return;

            Documents ??= new();
            _ = Enum.TryParse<FileExtension>
                ($"{file.Extension.Replace(".", "")}", out FileExtension fileExtension);

            Documents.Add(new DocumentDto(Content: fileByteArray, FileName: file.Name, FileExtension: fileExtension));
        }
        private void ClearData()
        {
            IsFileUploaded = false;
        }

    }
}
