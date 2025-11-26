using DataSanitizer.Domain.Models;

namespace DataSanitizer.Domain.Interfaces;

public interface IExecutionHistoryRepository
{
    Task SaveAsync(ExecutionHistory history);
}
