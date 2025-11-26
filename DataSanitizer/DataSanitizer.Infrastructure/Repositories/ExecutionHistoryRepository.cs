using DataSanitizer.Domain.Interfaces;
using DataSanitizer.Domain.Models;
using DataSanitizer.Infrastructure.Database;

namespace DataSanitizer.Infrastructure.Repositories;

public class ExecutionHistoryRepository : IExecutionHistoryRepository
{
    private readonly SanitizerDbContext _db;

    public ExecutionHistoryRepository(SanitizerDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(ExecutionHistory history)
    {
        await _db.ExecutionHistory.AddAsync(history);
        await _db.SaveChangesAsync();
    }
}
