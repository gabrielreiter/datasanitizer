namespace DataSanitizer.Domain.Models;

public class ExecutionHistory
{
    public int Id { get; set; }
    public string TableName { get; set; } = "";
    public int DeletedCount { get; set; }
    public string LogFilePath { get; set; } = "";
    public DateTime ExecutedAt { get; set; }
}
