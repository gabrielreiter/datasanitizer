namespace DataSanitizer.Domain.Models;

public class ExecutionResult
{
    public int DeletedCount { get; }
    public string LogFilePath { get; }

    public ExecutionResult(int deletedCount, string logFilePath)
    {
        DeletedCount = deletedCount;
        LogFilePath = logFilePath;
    }
}
