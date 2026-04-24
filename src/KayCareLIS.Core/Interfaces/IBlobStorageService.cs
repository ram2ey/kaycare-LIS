namespace KayCareLIS.Core.Interfaces;

public interface IBlobStorageService
{
    Task UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string containerName, string blobPath, CancellationToken ct = default);
    Uri GenerateSasUri(string containerName, string blobPath, TimeSpan expiry);
    Task<byte[]?> DownloadAsync(string containerName, string blobPath, CancellationToken ct = default);
}
