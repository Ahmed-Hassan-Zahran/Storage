using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Yoxel.Storage.Core.Abstractions;

namespace Yoxel.Storage.Infrastructure.Storage;

public sealed class S3StorageOptions
{
    public string BucketName { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? ServiceUrl { get; set; }      // for MinIO / non-AWS
    public bool ForcePathStyle { get; set; }     // MinIO needs this
}

public sealed class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly S3StorageOptions _options;

    public S3FileStorage(IAmazonS3 s3, IOptions<S3StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> SaveAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        };

        await _s3.PutObjectAsync(request, ct);
        return storageKey;
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(_options.BucketName, storageKey, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        await _s3.DeleteObjectAsync(_options.BucketName, storageKey, ct);
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_options.BucketName, storageKey, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task<Uri?> GetPresignedDownloadUrlAsync(string storageKey, TimeSpan validity, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.Add(validity),
            Verb = HttpVerb.GET,
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult<Uri?>(new Uri(url));
    }
}
