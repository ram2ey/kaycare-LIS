using Amazon.S3;
using Amazon.S3.Model;
using KayCareLIS.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KayCareLIS.Infrastructure.Services;

public class S3StorageService : IBlobStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration config)
    {
        _s3Client = s3Client;
        _bucketName = config["AWS:BucketName"] ?? "kaycare-pacs-studies";
    }

    public async Task UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName  = _bucketName,
            Key         = $"{containerName}/{blobPath}",
            InputStream = content,
            ContentType = contentType
        };
        await _s3Client.PutObjectAsync(request, ct);
    }

    public async Task DeleteAsync(string containerName, string blobPath, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key        = $"{containerName}/{blobPath}"
        };
        await _s3Client.DeleteObjectAsync(request, ct);
    }

    public async Task<byte[]?> DownloadAsync(string containerName, string blobPath, CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key        = $"{containerName}/{blobPath}"
            };
            using var response = await _s3Client.GetObjectAsync(request, ct);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Uri GenerateSasUri(string containerName, string blobPath, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key        = $"{containerName}/{blobPath}",
            Expires    = DateTime.UtcNow.Add(expiry)
        };
        return new Uri(_s3Client.GetPreSignedURL(request));
    }
}
