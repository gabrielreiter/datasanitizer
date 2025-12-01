using DataSanitizer.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataSanitizer.Infrastructure.Database;

public class SanitizerDbContext : DbContext
{
    public SanitizerDbContext(DbContextOptions<SanitizerDbContext> options)
        : base(options) { }

    public DbSet<ExecutionHistory> ExecutionHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExecutionHistory>(entity =>
        {
            entity.ToTable("executionhistory");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TableName).HasColumnName("tablename");
            entity.Property(e => e.DeletedCount).HasColumnName("deletedcount");
            entity.Property(e => e.LogFilePath).HasColumnName("logfilepath");
            entity.Property(e => e.ExecutedAt).HasColumnName("executedat");
        });
    }
}
