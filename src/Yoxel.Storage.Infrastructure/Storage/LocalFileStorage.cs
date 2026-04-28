using Microsoft.Extensions.Options;
using Yoxel.Storage.Core.Abstractions;

namespace Yoxel.Storage.Infrastructure.Storage;

public sealed class LocalFileStorageOptions
{
    public string RootPath { get; set; } = "./data/storage";
}

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<LocalFileStorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var fs = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fs, ct);
        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Storage object not found", path);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
        => Task.FromResult(File.Exists(ResolvePath(storageKey)));

    public Task<Uri?> GetPresignedDownloadUrlAsync(string storageKey, TimeSpan validity, CancellationToken ct = default)
        // Local FS has no signed-URL concept; callers must use the streaming endpoint.
        => Task.FromResult<Uri?>(null);

    private string ResolvePath(string storageKey)
    {
        // Reject path-traversal and absolute paths. Resolve, then assert
        // the result still lives inside the configured root.
        var safeKey = storageKey.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_root, safeKey));
        if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Path traversal detected.");
        }
        return fullPath;
    }
}
