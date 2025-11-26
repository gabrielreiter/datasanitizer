using DataSanitizer.Domain.Interfaces;
using DataSanitizer.Domain.Models;

namespace DataSanitizer.Domain.Services;

public class SanitationManager
{
    private readonly ITableCleaner _cleaner;
    private readonly ILogExporter _logger;
    private readonly IExecutionHistoryRepository _historyRepo;

    public SanitationManager(
        ITableCleaner cleaner,
        ILogExporter logger,
        IExecutionHistoryRepository historyRepo)
    {
        _cleaner = cleaner;
        _logger = logger;
        _historyRepo = historyRepo;
    }

    public async Task<ExecutionResult> ExecuteAsync(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be empty.");

        // 1 — Buscar registros antes de deletar
        var rows = await _cleaner.GetRowsBeforeDeleteAsync(tableName);

        // 2 — Exportar log
        var logPath = await _logger.ExportAsync(tableName, rows);

        // 3 — Limpar tabela
        var deleted = await _cleaner.CleanAsync(tableName);

        // 4 — Registrar histórico
        var history = new ExecutionHistory
        {
            TableName = tableName,
            DeletedCount = deleted,
            LogFilePath = logPath,
            ExecutedAt = DateTime.UtcNow
        };

        await _historyRepo.SaveAsync(history);

        return new ExecutionResult(deleted, logPath);
    }
}
