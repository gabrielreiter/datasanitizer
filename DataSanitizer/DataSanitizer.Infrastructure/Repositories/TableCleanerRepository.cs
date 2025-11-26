using DataSanitizer.Domain.Interfaces;
using Npgsql;

namespace DataSanitizer.Infrastructure.Repositories;

public class TableCleanerRepository : ITableCleaner
{
    private readonly string _connectionString;

    public TableCleanerRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<Dictionary<string, object>>> GetRowsBeforeDeleteAsync(string tableName)
    {
        var rows = new List<Dictionary<string, object>>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = $"SELECT * FROM {tableName}";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var dict = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
                dict[reader.GetName(i)] = reader.GetValue(i);

            rows.Add(dict);
        }

        return rows;
    }

    public async Task<int> CleanAsync(string tableName)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = $"DELETE FROM {tableName}";
        await using var cmd = new NpgsqlCommand(sql, conn);

        return await cmd.ExecuteNonQueryAsync();
    }
}
