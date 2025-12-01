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

    // ------------------------------------------------------------
    // 1 — Cenário de sucesso
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Executar_Com_Sucesso()
    {
        #region Arrange
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("logs"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _cleaner.Setup(x => x.CleanAsync("logs"))
            .ReturnsAsync(10);

        _logger.Setup(x => x.ExportAsync("logs", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("logs/log_1.json");

        var manager = CreateManager();
        #endregion

        #region Act
        var result = await manager.ExecuteAsync("logs");
        #endregion

        #region Assert
        Assert.Equal(10, result.DeletedCount);
        Assert.Equal("logs/log_1.json", result.LogFilePath);
        #endregion
    }

    // ------------------------------------------------------------
    // 2 — Nome de tabela inválido
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Gerar_Erro_Quando_Nome_Tabela_Eh_Vazio()
    {
        #region Arrange
        var manager = CreateManager();
        #endregion

        #region Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.ExecuteAsync(""));
        #endregion
    }

    // ------------------------------------------------------------
    // 3 — Histórico deve ser registrado
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Registrar_Historico_Apos_Execucao()
    {
        #region Arrange
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _cleaner.Setup(x => x.CleanAsync("t"))
            .ReturnsAsync(1);

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("logs/t.json");

        var manager = CreateManager();
        #endregion

        #region Act
        await manager.ExecuteAsync("t");
        #endregion

        #region Assert
        _historyRepo.Verify(x => x.SaveAsync(It.IsAny<ExecutionHistory>()), Times.Once);
        #endregion
    }

    // ------------------------------------------------------------
    // 4 — TableCleaner falha → exceção
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Propagar_Erro_Do_TableCleaner()
    {
        #region Arrange
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .ThrowsAsync(new Exception("Erro no SELECT"));

        var manager = CreateManager();
        #endregion

        #region Act
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            manager.ExecuteAsync("t"));
        #endregion

        #region Assert
        Assert.Equal("Erro no SELECT", ex.Message);
        #endregion
    }

    // ------------------------------------------------------------
    // 5 — LogExporter falha → exceção
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Propagar_Erro_Do_LogExporter()
    {
        #region Arrange
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ThrowsAsync(new IOException("Falha ao gravar log"));

        var manager = CreateManager();
        #endregion

        #region Act
        var ex = await Assert.ThrowsAsync<IOException>(() =>
            manager.ExecuteAsync("t"));
        #endregion

        #region Assert
        Assert.Equal("Falha ao gravar log", ex.Message);
        #endregion
    }

    // ------------------------------------------------------------
    // 6 — CleanAsync falha → exceção
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Propagar_Erro_Do_CleanAsync()
    {
        #region Arrange
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t"))
            .ThrowsAsync(new Exception("Erro ao deletar"));

        var manager = CreateManager();
        #endregion

        #region Act
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            manager.ExecuteAsync("t"));
        #endregion

        #region Assert
        Assert.Equal("Erro ao deletar", ex.Message);
        #endregion
    }

    // ------------------------------------------------------------
    // 7 — Quando não há linhas
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Retornar_Zero_Quando_Nao_Ha_Linhas()
    {
        #region Arrange
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t")).ReturnsAsync(0);

        var manager = CreateManager();
        #endregion

        #region Act
        var result = await manager.ExecuteAsync("t");
        #endregion

        #region Assert
        Assert.Equal(0, result.DeletedCount);
        #endregion
    }

    // ------------------------------------------------------------
    // 8 — Deve exportar log quando há linhas
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Exportar_Log_Com_Linhas()
    {
        #region Arrange
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
        #endregion

        #region Act
        var result = await manager.ExecuteAsync("t");
        #endregion

        #region Assert
        Assert.Equal("ok", result.LogFilePath);
        #endregion
    }

    // ------------------------------------------------------------
    // 9 — CleanAsync deve ser chamado uma vez
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Chamar_CleanAsync_Uma_Vez()
    {
        #region Arrange
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t"))
            .ReturnsAsync(5);

        var manager = CreateManager();
        #endregion

        #region Act
        await manager.ExecuteAsync("t");
        #endregion

        #region Assert
        _cleaner.Verify(x => x.CleanAsync("t"), Times.Once);
        #endregion
    }

    // ------------------------------------------------------------
    // 10 — GetRowsBeforeDeleteAsync deve ser chamado uma vez
    // ------------------------------------------------------------
    [Fact]
    public async Task Deve_Chamar_GetRowsBeforeDeleteAsync_Uma_Vez()
    {
        #region Arrange
        _cleaner.Setup(x => x.GetRowsBeforeDeleteAsync("t"))
            .Returns(Task.FromResult(new List<Dictionary<string, object>>()));

        _logger.Setup(x => x.ExportAsync("t", It.IsAny<IEnumerable<object>>()))
            .ReturnsAsync("ok");

        _cleaner.Setup(x => x.CleanAsync("t")).ReturnsAsync(1);

        var manager = CreateManager();
        #endregion

        #region Act
        await manager.ExecuteAsync("t");
        #endregion

        #region Assert
        _cleaner.Verify(x => x.GetRowsBeforeDeleteAsync("t"), Times.Once);
        #endregion
    }
}
