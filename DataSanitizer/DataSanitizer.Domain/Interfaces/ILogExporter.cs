namespace DataSanitizer.Domain.Interfaces;

public interface ILogExporter
{
    Task<string> ExportAsync(string tableName, IEnumerable<object> rows);
}
