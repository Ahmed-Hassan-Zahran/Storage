using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Yoxel.Storage.Core.Models;

namespace Yoxel.Storage.Infrastructure.Persistence;

public sealed class StorageDbContext : DbContext
{
    public StorageDbContext(DbContextOptions<StorageDbContext> options) : base(options)
    {
    }

    public DbSet<FileMetadata> Files => Set<FileMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FileMetadata>();

        entity.ToTable("files");
        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.TenantId);
        entity.HasIndex(x => x.Sha256);
        entity.HasIndex(x => new { x.TenantId, x.CreatedAt });

        entity.Property(x => x.TenantId).HasMaxLength(128).IsRequired();
        entity.Property(x => x.FileName).HasMaxLength(512).IsRequired();
        entity.Property(x => x.ContentType).HasMaxLength(255).IsRequired();
        entity.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        entity.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        entity.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(2048);

        var tagsComparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v.Aggregate(0, (acc, kv) => HashCode.Combine(acc, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            v => v.ToDictionary(kv => kv.Key, kv => kv.Value));

        entity.Property(x => x.Tags)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(tagsComparer);
    }
}
