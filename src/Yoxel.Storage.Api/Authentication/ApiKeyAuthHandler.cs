using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Yoxel.Storage.Api.Authentication;

public sealed class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyOptions>
{
    public const string SchemeName = "ApiKey";
    public const string TenantClaim = "tenant";
    private const string HeaderName = "X-Api-Key";

    public ApiKeyAuthHandler(
        IOptionsMonitor<ApiKeyOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values) || values.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var key = values.ToString();
        if (string.IsNullOrWhiteSpace(key) || !Options.Keys.TryGetValue(key, out var entry))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, entry.Name),
            new Claim(TenantClaim, entry.TenantId),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
