namespace Yoxel.Storage.Core.Exceptions;

public sealed class StorageFileNotFoundException : Exception
{
    public Guid FileId { get; }

    public StorageFileNotFoundException(Guid fileId)
        : base($"File '{fileId}' was not found.")
    {
        FileId = fileId;
    }
}
