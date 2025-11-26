using Xunit;
using Microsoft.EntityFrameworkCore;
using DataSanitizer.Infrastructure.Database;
using DataSanitizer.Infrastructure.Repositories;
using DataSanitizer.Domain.Models;

public class ExecutionHistoryRepositoryTests
{
    [Fact]
    public async Task Deve_Salvar_Historico_No_Banco()
    {
        var options = new DbContextOptionsBuilder<SanitizerDbContext>()
            .UseInMemoryDatabase("db_test")
            .Options;

        using var db = new SanitizerDbContext(options);
        var repo = new ExecutionHistoryRepository(db);

        var history = new ExecutionHistory
        {
            TableName = "tabela",
            DeletedCount = 5,
            LogFilePath = "arquivo.json",
            ExecutedAt = DateTime.UtcNow
        };

        await repo.SaveAsync(history);

        Assert.Equal(1, db.ExecutionHistory.Count());
    }
}
