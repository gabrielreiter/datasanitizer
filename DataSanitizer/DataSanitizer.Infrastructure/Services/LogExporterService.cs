using System.Text.Json;
using DataSanitizer.Domain.Interfaces;

namespace DataSanitizer.Infrastructure.Services;

public class LogExporterService : ILogExporter
{
    public async Task<string> ExportAsync(string tableName, IEnumerable<object> rows)
    {
        Directory.CreateDirectory("logs");

        var fileName = $"{tableName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine("logs", fileName);

        var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);

        return filePath;
    }
}
