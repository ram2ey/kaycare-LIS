using KayCareLIS.Core.Constants;
using KayCareLIS.Core.DTOs.Documents;
using KayCareLIS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents) => _documents = documents;

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
        => Ok(await _documents.GetByPatientAsync(patientId, ct));

    [HttpGet("{id:guid}/download-url")]
    public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken ct)
    {
        var url = await _documents.GetDownloadUrlAsync(id, ct);
        return Ok(new { url });
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.LabTechnician}")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentRequest request,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest(new { error = "No file provided." });
        if (file.Length > 20 * 1024 * 1024) return BadRequest(new { error = "File exceeds 20 MB limit." });

        await using var stream = file.OpenReadStream();
        var info = new FileUploadInfo(stream, Path.GetFileName(file.FileName), file.ContentType, file.Length);

        var doc = await _documents.UploadAsync(request, info, ct);
        return CreatedAtAction(nameof(GetDownloadUrl), new { id = doc.DocumentId }, doc);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _documents.DeleteAsync(id, ct);
        return NoContent();
    }
}
