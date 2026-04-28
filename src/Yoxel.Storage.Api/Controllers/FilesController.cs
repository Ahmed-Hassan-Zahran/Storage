using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yoxel.Storage.Api.Authentication;
using Yoxel.Storage.Core.Abstractions;

namespace Yoxel.Storage.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthHandler.SchemeName)]
[Route("api/v1/files")]
[Produces("application/json")]
public sealed class FilesController : ControllerBase
{
    private const long MaxUploadBytes = 5L * 1024 * 1024 * 1024; // 5 GB

    private readonly IStorageService _service;

    public FilesController(IStorageService service)
    {
        _service = service;
    }

    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? description,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "A non-empty 'file' part is required." });
        }

        await using var stream = file.OpenReadStream();
        var metadata = await _service.UploadAsync(new UploadRequest(
            TenantId: GetTenant(),
            FileName: file.FileName,
            ContentType: string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            Content: stream,
            CreatedBy: GetCallerName(),
            Description: description), ct);

        return CreatedAtAction(nameof(GetMetadata), new { id = metadata.Id }, metadata);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var (file, _) = await _service.DownloadAsync(id, GetTenant(), ct);
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    [HttpGet("{id:guid}/metadata")]
    public async Task<IActionResult> GetMetadata(Guid id, CancellationToken ct)
    {
        var meta = await _service.GetMetadataAsync(id, GetTenant(), ct);
        return meta is null ? NotFound() : Ok(meta);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);
        var items = await _service.ListAsync(GetTenant(), skip, take, ct);
        return Ok(items);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, GetTenant(), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/url")]
    public async Task<IActionResult> GetPresignedUrl(
        Guid id,
        [FromQuery] int validitySeconds = 900,
        CancellationToken ct = default)
    {
        validitySeconds = Math.Clamp(validitySeconds, 30, 7 * 24 * 3600);
        var url = await _service.GetDownloadUrlAsync(id, GetTenant(), TimeSpan.FromSeconds(validitySeconds), ct);
        return url is null
            ? StatusCode(StatusCodes.Status501NotImplemented,
                new { error = "Presigned URLs are not supported by the current storage backend." })
            : Ok(new { url });
    }

    private string GetTenant()
        => User.FindFirst(ApiKeyAuthHandler.TenantClaim)?.Value
           ?? throw new UnauthorizedAccessException("Tenant claim missing.");

    private string GetCallerName()
        => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
}
