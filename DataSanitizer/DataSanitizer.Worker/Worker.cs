using DataSanitizer.Domain.Services;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _services;

    public Worker(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<SanitationManager>();

            // tabela de teste inicial
            await manager.ExecuteAsync("sample_logs");

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
