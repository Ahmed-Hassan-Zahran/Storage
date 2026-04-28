namespace Yoxel.Storage.Core.Abstractions;

public interface IStorageEventPublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken ct = default);
}
