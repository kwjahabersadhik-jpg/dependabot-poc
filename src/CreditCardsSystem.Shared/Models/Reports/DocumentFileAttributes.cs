using CreditCardsSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Reports;
public class DownloadDocumentAttributes
{
    public required string DocId { get; set; }
    public FileServer FileServer { get; set; } = FileServer.Aurora;
}
public class SearchDocumentAttributes
{
    public string MetaDataKey { get; set; }
    public string MetaDataValue { get; set; }
    public string? KfhId { get; set; }
    public FileServer FileServer { get; set; } = FileServer.Aurora;
    public Dictionary<string, string>? MetaData { get; set; }
}
public class DocumentFileAttributes
{
    private string fileName = string.Empty;

    public string FileName { get => $"{fileName}@{DateTime.Now.ToString(ConfigurationBase.FileDateFormat)}.{Extension}"; set => fileName = value; }

    [RegularExpression(@"^[a-zA-Z\d\s]{1,100}$", ErrorMessage = "Invalid Description")]
    public string Description { get; set; } = string.Empty;

    [RegularExpression(@"^[a-zA-Z\d\s]{1,100}$", ErrorMessage = "Invalid Type")]
    public string Type { get; set; } = string.Empty;
    public byte[]? FileBytes { get; set; }

    [RegularExpression(@"^[a-zA-Z\d\s]$", ErrorMessage = "Invalid FilePath")]
    public string? FilePath { get; set; }

    [RegularExpression(@"^[a-zA-Z\d\.]{1,25}$", ErrorMessage = "Invalid RequestId")]
    public string RequestId { get; set; } = string.Empty;

    [RegularExpression(@"^\d{1,8}", ErrorMessage = "Invalid KfhId")]
    public string? KfhId { get; set; }
    public FileExtension Extension { get; set; }
    public FileServer FileServer { get; set; } = FileServer.Aurora;
    public Dictionary<string, object>? MetaData { get; set; }
}

public enum FileServer
{
    Aurora = 1,
    Docuware
}
public enum FileExtension
{
    gif,
    png,
    jpeg,
    jpg,
    pdf,
    tif,
    tiff,
    docx,
    xls,
    xlsx
}

public record DocumentDto(byte[] Content, string FileName, FileExtension FileExtension, string FilePath = "")
{
    public bool IsUploaded { get; set; } = false;
    public string Message { get; set; } 
}

