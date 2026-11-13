using System.Reflection;
using Npgsql;

namespace ChartTrail.Api.Db;

public static class SchemaInitializer
{
    public static async Task ApplyAsync(NpgsqlDataSource db, CancellationToken ct = default)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("schema.sql")
            ?? throw new InvalidOperationException("schema.sql is not embedded in the assembly");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(ct);

        await using var command = db.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(ct);
    }
}