using Microsoft.EntityFrameworkCore;
using SmartFlow.Api.Models;

namespace SmartFlow.Api.Data;

/// <summary>
/// EF Core database context for SmartFlow (SQLite).
/// </summary>
public sealed class SmartFlowDbContext(DbContextOptions<SmartFlowDbContext> options) : DbContext(options)
{
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var request = modelBuilder.Entity<SupportRequest>();

        request.HasKey(r => r.Id);

        // Same limits as the API validation rules. SQLite does not enforce text length,
        // but the limits document the schema and apply if we move to another database later.
        request.Property(r => r.OriginalText).HasMaxLength(5000);
        request.Property(r => r.SenderEmail).HasMaxLength(254);
        request.Property(r => r.Summary).HasMaxLength(300);
        request.Property(r => r.SuggestedAction).HasMaxLength(500);
        request.Property(r => r.ModelName).HasMaxLength(100);

        // Store enums as readable text ("Billing") instead of numbers.
        request.Property(r => r.Category).HasConversion<string>().HasMaxLength(20);
        request.Property(r => r.Priority).HasConversion<string>().HasMaxLength(20);

        // SQLite stores no time zone. We only ever write UTC, so mark values read back as UTC;
        // otherwise they come back as "Unspecified" and the JSON loses its trailing "Z".
        request.Property(r => r.CreatedAtUtc)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // The list endpoint filters by these columns and sorts by creation time.
        request.HasIndex(r => r.CreatedAtUtc);
        request.HasIndex(r => r.Category);
        request.HasIndex(r => r.Priority);
    }
}
