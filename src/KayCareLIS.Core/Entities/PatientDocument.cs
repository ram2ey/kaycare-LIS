namespace KayCareLIS.Core.Entities;

public class PatientDocument : TenantEntity
{
    public Guid    DocumentId       { get; set; }
    public Guid    PatientId        { get; set; }
    public Guid    UploadedByUserId { get; set; }
    public string  FileName         { get; set; } = string.Empty;
    public string  ContentType      { get; set; } = string.Empty;
    public long    FileSizeBytes    { get; set; }
    public string  Category         { get; set; } = "Other";
    public string? Description      { get; set; }
    public string  BlobPath         { get; set; } = string.Empty;
    public string  ContainerName    { get; set; } = string.Empty;

    public Patient Patient    { get; set; } = null!;
    public User    UploadedBy { get; set; } = null!;
}
