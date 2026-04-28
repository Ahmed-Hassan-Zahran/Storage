using Microsoft.AspNetCore.Authentication;

namespace Yoxel.Storage.Api.Authentication;

public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    // Map of {api-key -> tenant + caller name}. Bound from configuration.
    // In production, replace with a hashed-secret store backed by a vault.
    public Dictionary<string, ApiKeyEntry> Keys { get; set; } = new(StringComparer.Ordinal);
}

public sealed class ApiKeyEntry
{
    public string TenantId { get; set; } = "default";
    public string Name { get; set; } = string.Empty;
}
