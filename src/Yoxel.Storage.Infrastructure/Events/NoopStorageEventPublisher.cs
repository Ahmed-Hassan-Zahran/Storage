using System.Text.Json;
using Microsoft.Extensions.Logging;
using Yoxel.Storage.Core.Abstractions;

namespace Yoxel.Storage.Infrastructure.Events;

// Default publisher: structured-logs the event. In production, swap with a
// Kafka or RabbitMQ publisher in the composition root.
public sealed class NoopStorageEventPublisher : IStorageEventPublisher
{
    private readonly ILogger<NoopStorageEventPublisher> _logger;

    public NoopStorageEventPublisher(ILogger<NoopStorageEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string eventName, object payload, CancellationToken ct = default)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            var json = JsonSerializer.Serialize(payload);
            _logger.LogInformation("storage event {EventName}: {Payload}", eventName, json);
        }
        return Task.CompletedTask;
    }
}
