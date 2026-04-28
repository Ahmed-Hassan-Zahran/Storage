using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yoxel.Storage.Core.Abstractions;
using Yoxel.Storage.Core.Exceptions;
using Yoxel.Storage.Core.Models;

namespace Yoxel.Storage.Core.Services;

public sealed class StorageService : IStorageService
{
    private readonly IFileStorage _storage;
    private readonly IFileMetadataRepository _repository;
    private readonly IStorageEventPublisher _events;
    private readonly ILogger<StorageService> _logger;

    public StorageService(
        IFileStorage storage,
        IFileMetadataRepository repository,
        IStorageEventPublisher events,
        ILogger<StorageService> logger)
    {
        _storage = storage;
        _repository = repository;
        _events = events;
        _logger = logger;
    }

    public async Task<FileMetadata> UploadAsync(UploadRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);

        var id = Guid.NewGuid();
        var storageKey = BuildStorageKey(request.TenantId, id, request.FileName);

        // Buffer to a temp file so we can compute SHA-256 + size in one
        // pass and then re-read the buffer to push to the backend without
        // loading the whole file in memory.
        var tempPath = Path.GetTempFileName();
        await using var buffer = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        await request.Content.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(buffer, ct);
        var sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var size = buffer.Length;
        buffer.Position = 0;

        await _storage.SaveAsync(storageKey, buffer, request.ContentType, ct);

        var metadata = new FileMetadata
        {
            Id = id,
            TenantId = request.TenantId,
            FileName = SanitizeFileName(request.FileName),
            ContentType = string.IsNullOrWhiteSpace(request.ContentType)
                ? "application/octet-stream"
                : request.ContentType,
            SizeBytes = size,
            Sha256 = sha256,
            StorageKey = storageKey,
            Description = request.Description,
            Tags = request.Tags?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new(),
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.AddAsync(metadata, ct);

        await _events.PublishAsync("storage.file.uploaded", new
        {
            metadata.Id,
            metadata.TenantId,
            metadata.FileName,
            metadata.ContentType,
            metadata.SizeBytes,
            metadata.Sha256,
            metadata.CreatedBy,
            metadata.CreatedAt,
        }, ct);

        _logger.LogInformation(
            "Uploaded file {FileId} ({Size} bytes, sha256={Sha}) for tenant {Tenant}",
            metadata.Id, metadata.SizeBytes, metadata.Sha256, metadata.TenantId);

        return metadata;
    }

    public async Task<(StoredFile File, FileMetadata Metadata)> DownloadAsync(
        Guid id, string tenantId, CancellationToken ct = default)
    {
        var metadata = await GetOwnedAsync(id, tenantId, ct);
        var stream = await _storage.OpenReadAsync(metadata.StorageKey, ct);
        var stored = new StoredFile(stream, metadata.ContentType, metadata.FileName, metadata.SizeBytes);
        return (stored, metadata);
    }

    public async Task<FileMetadata?> GetMetadataAsync(Guid id, string tenantId, CancellationToken ct = default)
    {
        var metadata = await _repository.GetAsync(id, ct);
        return metadata is null || metadata.TenantId != tenantId ? null : metadata;
    }

    public Task<IReadOnlyList<FileMetadata>> ListAsync(string tenantId, int skip, int take, CancellationToken ct = default)
        => _repository.ListAsync(tenantId, skip, take, ct);

    public async Task DeleteAsync(Guid id, string tenantId, CancellationToken ct = default)
    {
        var metadata = await GetOwnedAsync(id, tenantId, ct);
        await _repository.SoftDeleteAsync(metadata.Id, ct);

        await _events.PublishAsync("storage.file.deleted", new
        {
            metadata.Id,
            metadata.TenantId,
            DeletedAt = DateTimeOffset.UtcNow,
        }, ct);

        _logger.LogInformation("Soft-deleted file {FileId} for tenant {Tenant}", metadata.Id, metadata.TenantId);
    }

    public async Task<Uri?> GetDownloadUrlAsync(Guid id, string tenantId, TimeSpan validity, CancellationToken ct = default)
    {
        var metadata = await GetOwnedAsync(id, tenantId, ct);
        return await _storage.GetPresignedDownloadUrlAsync(metadata.StorageKey, validity, ct);
    }

    private async Task<FileMetadata> GetOwnedAsync(Guid id, string tenantId, CancellationToken ct)
    {
        var metadata = await _repository.GetAsync(id, ct);
        if (metadata is null || metadata.TenantId != tenantId)
        {
            // Same response whether the file is missing or belongs to another
            // tenant — don't leak existence across tenant boundaries.
            throw new StorageFileNotFoundException(id);
        }
        return metadata;
    }

    private static string BuildStorageKey(string tenantId, Guid id, string fileName)
    {
        var safe = SanitizeFileName(fileName);
        return $"{tenantId}/{id:N}/{safe}";
    }

    private static string SanitizeFileName(string name)
    {
        var stripped = Path.GetFileName(name);
        return string.IsNullOrWhiteSpace(stripped) ? "file" : stripped;
    }
}
