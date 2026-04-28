namespace Yoxel.Storage.Core.Models;

public sealed record StoredFile(
    Stream Content,
    string ContentType,
    string FileName,
    long SizeBytes);
