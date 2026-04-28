using Yoxel.Storage.Core.Models;

namespace Yoxel.Storage.Core.Abstractions;

public interface IFileMetadataRepository
{
    Task AddAsync(FileMetadata metadata, CancellationToken ct = default);

    Task<FileMetadata?> GetAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<FileMetadata>> ListAsync(string tenantId, int skip, int take, CancellationToken ct = default);

    Task UpdateAsync(FileMetadata metadata, CancellationToken ct = default);

    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
