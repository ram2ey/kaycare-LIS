namespace KayCareLIS.Core.DTOs.Documents;

public record FileUploadInfo(
    Stream Content,
    string FileName,
    string ContentType,
    long   SizeBytes
);
