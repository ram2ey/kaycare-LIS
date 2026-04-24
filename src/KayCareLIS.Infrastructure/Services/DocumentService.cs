using KayCareLIS.Core.DTOs.Documents;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Exceptions;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCareLIS.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private static readonly TimeSpan SasExpiry = TimeSpan.FromMinutes(15);

    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IBlobStorageService _blob;

    public DocumentService(AppDbContext db, ITenantContext tenantContext, ICurrentUserService currentUser, IBlobStorageService blob)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _currentUser   = currentUser;
        _blob          = blob;
    }

    public async Task<DocumentResponse> UploadAsync(UploadDocumentRequest request, FileUploadInfo file, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == request.PatientId, ct)
            ?? throw new NotFoundException("Patient not found.");

        var container = ContainerName();
        var blobPath  = $"documents/{Guid.NewGuid()}/{file.FileName}";
        await _blob.UploadAsync(container, blobPath, file.Content, file.ContentType, ct);

        var uploader = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == _currentUser.UserId, ct);

        var doc = new PatientDocument
        {
            DocumentId       = Guid.NewGuid(),
            PatientId        = request.PatientId,
            UploadedByUserId = _currentUser.UserId,
            FileName         = file.FileName,
            ContentType      = file.ContentType,
            FileSizeBytes    = file.SizeBytes,
            Category         = request.Category,
            Description      = request.Description?.Trim(),
            BlobPath         = blobPath,
            ContainerName    = container,
        };
        _db.PatientDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);

        return new DocumentResponse
        {
            DocumentId     = doc.DocumentId,
            PatientId      = patient.PatientId,
            PatientName    = $"{patient.FirstName} {patient.LastName}",
            FileName       = doc.FileName,
            ContentType    = doc.ContentType,
            FileSizeBytes  = doc.FileSizeBytes,
            Category       = doc.Category,
            Description    = doc.Description,
            UploadedByName = uploader == null ? string.Empty : $"{uploader.FirstName} {uploader.LastName}",
            CreatedAt      = doc.CreatedAt,
        };
    }

    public async Task<IReadOnlyList<DocumentResponse>> GetByPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var docs = await _db.PatientDocuments
            .Include(d => d.Patient)
            .Include(d => d.UploadedBy)
            .AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return docs.Select(d => new DocumentResponse
        {
            DocumentId     = d.DocumentId,
            PatientId      = d.PatientId,
            PatientName    = $"{d.Patient.FirstName} {d.Patient.LastName}",
            FileName       = d.FileName,
            ContentType    = d.ContentType,
            FileSizeBytes  = d.FileSizeBytes,
            Category       = d.Category,
            Description    = d.Description,
            UploadedByName = $"{d.UploadedBy.FirstName} {d.UploadedBy.LastName}",
            CreatedAt      = d.CreatedAt,
        }).ToList();
    }

    public async Task<string> GetDownloadUrlAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.PatientDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct)
            ?? throw new NotFoundException("Document not found.");

        return _blob.GenerateSasUri(doc.ContainerName, doc.BlobPath, SasExpiry).ToString();
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.PatientDocuments.FirstOrDefaultAsync(d => d.DocumentId == documentId, ct)
            ?? throw new NotFoundException("Document not found.");

        await _blob.DeleteAsync(doc.ContainerName, doc.BlobPath, ct);
        _db.PatientDocuments.Remove(doc);
        await _db.SaveChangesAsync(ct);
    }

    private string ContainerName()
    {
        var sanitized = new string(
            _tenantContext.TenantCode.ToLower().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (sanitized.Contains("--")) sanitized = sanitized.Replace("--", "-");
        sanitized = sanitized.Trim('-');
        return $"tenant-{sanitized}";
    }
}
