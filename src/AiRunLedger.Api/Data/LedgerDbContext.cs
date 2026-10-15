using AiRunLedger.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiRunLedger.Api.Data;

public class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options)
{
    public DbSet<RunRecord> Runs => Set<RunRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<RunRecord>(e =>
        {
            e.Property(r => r.Model).HasMaxLength(100).IsRequired();
            e.Property(r => r.Status).HasConversion<string>();
            e.Property(r => r.Source).HasConversion<string>();
            // SQLite has no real decimal type. EF stores these as TEXT, which keeps exact values
            // but means SQL cannot SUM them. Summaries are therefore computed in C#.
        });
    }
}
