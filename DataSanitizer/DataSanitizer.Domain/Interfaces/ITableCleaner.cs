using DataSanitizer.Domain.Models;

namespace DataSanitizer.Domain.Interfaces;

public interface ITableCleaner
{
    Task<List<Dictionary<string, object>>> GetRowsBeforeDeleteAsync(string tableName);
    Task<int> CleanAsync(string tableName);
}
