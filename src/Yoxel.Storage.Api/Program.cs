using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Yoxel.Storage.Api.Authentication;
using Yoxel.Storage.Api.Middleware;
using Yoxel.Storage.Infrastructure;
using Yoxel.Storage.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Yoxel Storage API",
        Version = "v1",
        Description = "Centralized storage hub for Yoxel microservices.",
    });

    var apiKeyScheme = new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey",
        In = ParameterLocation.Header,
        Description = "API key issued per microservice. Each key is bound to a single tenant.",
        Reference = new OpenApiReference { Id = "ApiKey", Type = ReferenceType.SecurityScheme },
    };
    c.AddSecurityDefinition("ApiKey", apiKeyScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { apiKeyScheme, Array.Empty<string>() } });
});

// Health
builder.Services.AddHealthChecks();

// Multipart upload limits (5 GB)
const long MaxUploadBytes = 5L * 1024 * 1024 * 1024;
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = MaxUploadBytes;
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = MaxUploadBytes;
});

// Domain + adapters
builder.Services.AddStorageInfrastructure(builder.Configuration);

// Auth
builder.Services
    .AddAuthentication(ApiKeyAuthHandler.SchemeName)
    .AddScheme<ApiKeyOptions, ApiKeyAuthHandler>(ApiKeyAuthHandler.SchemeName, options =>
    {
        builder.Configuration.GetSection("Authentication:ApiKeys").Bind(options.Keys);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Convenience: ensure schema exists in dev. In production use migrations.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapControllers();

app.Run();
