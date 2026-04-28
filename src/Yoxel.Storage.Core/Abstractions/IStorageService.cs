using Yoxel.Storage.Core.Models;

namespace Yoxel.Storage.Core.Abstractions;

public interface IStorageService
{
    Task<FileMetadata> UploadAsync(UploadRequest request, CancellationToken ct = default);

    Task<(StoredFile File, FileMetadata Metadata)> DownloadAsync(Guid id, string tenantId, CancellationToken ct = default);

    Task<FileMetadata?> GetMetadataAsync(Guid id, string tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<FileMetadata>> ListAsync(string tenantId, int skip, int take, CancellationToken ct = default);

    Task DeleteAsync(Guid id, string tenantId, CancellationToken ct = default);

    Task<Uri?> GetDownloadUrlAsync(Guid id, string tenantId, TimeSpan validity, CancellationToken ct = default);
}

public sealed record UploadRequest(
    string TenantId,
    string FileName,
    string ContentType,
    Stream Content,
    string CreatedBy,
    string? Description = null,
    IReadOnlyDictionary<string, string>? Tags = null);
