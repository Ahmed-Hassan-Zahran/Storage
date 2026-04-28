namespace Yoxel.Storage.Core.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);

    Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default);

    Task<Uri?> GetPresignedDownloadUrlAsync(string storageKey, TimeSpan validity, CancellationToken ct = default);
}
