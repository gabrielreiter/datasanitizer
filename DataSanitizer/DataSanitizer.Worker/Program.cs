using DataSanitizer.Domain.Interfaces;
using DataSanitizer.Domain.Services;
using DataSanitizer.Infrastructure.Database;
using DataSanitizer.Infrastructure.Repositories;
using DataSanitizer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<SanitizerDbContext>(options =>
    options.UseNpgsql(connectionString));

// Table cleaner usa NpgsqlConnection direto
builder.Services.AddScoped<ITableCleaner>(sp =>
    new TableCleanerRepository(connectionString));

builder.Services.AddScoped<IExecutionHistoryRepository, ExecutionHistoryRepository>();
builder.Services.AddScoped<ILogExporter, LogExporterService>();
builder.Services.AddScoped<SanitationManager>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
