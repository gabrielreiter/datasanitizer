using DataSanitizer.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataSanitizer.Infrastructure.Database;

public class SanitizerDbContext : DbContext
{
    public SanitizerDbContext(DbContextOptions<SanitizerDbContext> options)
        : base(options) { }

    public DbSet<ExecutionHistory> ExecutionHistory { get; set; }
}
