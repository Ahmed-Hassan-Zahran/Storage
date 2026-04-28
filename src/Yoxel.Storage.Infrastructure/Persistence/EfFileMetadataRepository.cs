using Microsoft.EntityFrameworkCore;
using Yoxel.Storage.Core.Abstractions;
using Yoxel.Storage.Core.Models;

namespace Yoxel.Storage.Infrastructure.Persistence;

public sealed class EfFileMetadataRepository : IFileMetadataRepository
{
    private readonly StorageDbContext _db;

    public EfFileMetadataRepository(StorageDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(FileMetadata metadata, CancellationToken ct = default)
    {
        await _db.Files.AddAsync(metadata, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<FileMetadata?> GetAsync(Guid id, CancellationToken ct = default)
        => _db.Files.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct);

    public async Task<IReadOnlyList<FileMetadata>> ListAsync(string tenantId, int skip, int take, CancellationToken ct = default)
        => await _db.Files.AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task UpdateAsync(FileMetadata metadata, CancellationToken ct = default)
    {
        _db.Files.Update(metadata);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Files.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entity is null)
        {
            return;
        }

        entity.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
