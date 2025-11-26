using Xunit;
using Moq;
using DataSanitizer.Domain.Interfaces;
using DataSanitizer.Domain.Services;
using DataSanitizer.Domain.Models;

public class SanitationManagerTests
{
    private readonly Mock<ITableCleaner> _cleaner = new();
    private readonly Mock<ILogExporter> _logger = new();
    private readonly Mock<IExecutionHistoryRepository> _historyRepo = new();

    private SanitationManager CreateManager() =>
        new SanitationManager(_cleaner.Object, _logger.Object, _historyRepo.Object);

    // 1 — Cenário de sucesso
    [Fact]
    public async Task Deve_Executar_Com_Sucesso()
    {
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("logs"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _cleaner.Setup(x => x.CleanAsync("logs"))
            .ReturnsAsync(10);

        _logger.Setup(x => x.ExportAsync("logs", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("logs/log_1.json");

        var manager = CreateManager();
        var result = await manager.ExecuteAsync("logs");

        Assert.Equal(10, result.DeletedCount);
        Assert.Equal("logs/log_1.json", result.LogFilePath);
    }

    // 2 — Tabela inválida → erro
    [Fact]
    public async Task Deve_Gerar_Erro_Quando_Nome_Tabela_Eh_Vazio()
    {
        var manager = CreateManager();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.ExecuteAsync(""));
    }

    // 3 — Verifica se registrar histórico foi chamado
    [Fact]
    public async Task Deve_Registrar_Historico_Apos_Execucao()
    {
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _cleaner.Setup(x => x.CleanAsync("t"))
            .ReturnsAsync(1);

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("logs/t.json");

        var manager = CreateManager();
        await manager.ExecuteAsync("t");

        _historyRepo.Verify(x => x.SaveAsync(It.IsAny<ExecutionHistory>()), Times.Once);
    }

    // 4 — TableCleaner falha → exceção propagada
    [Fact]
    public async Task Deve_Propagar_Erro_Do_TableCleaner()
    {
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .ThrowsAsync(new Exception("Erro no SELECT"));

        var manager = CreateManager();

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            manager.ExecuteAsync("t"));

        Assert.Equal("Erro no SELECT", ex.Message);
    }

    // 5 — LogExporter falha → exceção propagada
    [Fact]
    public async Task Deve_Propagar_Erro_Do_LogExporter()
    {
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ThrowsAsync(new IOException("Falha ao gravar log"));

        var manager = CreateManager();

        var ex = await Assert.ThrowsAsync<IOException>(() =>
            manager.ExecuteAsync("t"));

        Assert.Equal("Falha ao gravar log", ex.Message);
    }

    // 6 — CleanAsync lança erro → exceção propagada
    [Fact]
    public async Task Deve_Propagar_Erro_Do_CleanAsync()
    {
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t"))
            .ThrowsAsync(new Exception("Erro ao deletar"));

        var manager = CreateManager();

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            manager.ExecuteAsync("t"));

        Assert.Equal("Erro ao deletar", ex.Message);
    }

    // 7 — Quando não há linhas → log e delete funcionam
    [Fact]
    public async Task Deve_Retornar_Zero_Quando_Nao_Ha_Linhas()
    {
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t")).ReturnsAsync(0);

        var manager = CreateManager();
        var result = await manager.ExecuteAsync("t");

        Assert.Equal(0, result.DeletedCount);
    }

    // 8 — Deve exportar log mesmo com linhas
    [Fact]
    public async Task Deve_Exportar_Log_Com_Linhas()
    {
        var linhas = new List<Dictionary<string, object>>
        {
            new() { ["id"] = 1, ["msg"] = "teste" }
        };

        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(linhas));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t"))
            .ReturnsAsync(1);

        var manager = CreateManager();
        var result = await manager.ExecuteAsync("t");

        Assert.Equal("ok", result.LogFilePath);
    }

    // 9 — Deve chamar CleanAsync exatamente 1 vez
    [Fact]
    public async Task Deve_Chamar_CleanAsync_Uma_Vez()
    {
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t"))
            .ReturnsAsync(5);

        var manager = CreateManager();
        await manager.ExecuteAsync("t");

        _cleaner.Verify(x => x.CleanAsync("t"), Times.Once);
    }

    // 10 — Deve chamar GetRowsBeforeDeleteAsync exatamente 1 vez
    [Fact]
    public async Task Deve_Chamar_GetRowsBeforeDeleteAsync_Uma_Vez()
    {
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t"))
            .ReturnsAsync(1);

        var manager = CreateManager();
        await manager.ExecuteAsync("t");

        _cleaner.Verify(x => x.GetRowsBeforeDeleteAsync("t"), Times.Once);
    }
}
