namespace KayCareLIS.Core.DTOs.Documents;

public class UploadDocumentRequest
{
    public Guid    PatientId   { get; set; }
    public string  Category    { get; set; } = "Other";
    public string? Description { get; set; }
}
