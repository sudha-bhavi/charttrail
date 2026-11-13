using ChartTrail.Api.Db;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

if (args.FirstOrDefault() == "migrate")
{
    await SchemaInitializer.ApplyAsync(app.Services.GetRequiredService<NpgsqlDataSource>());
    Console.WriteLine("Schema applied.");
    return;
}

app.MapGet("/health", async (NpgsqlDataSource db, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        await using var command = db.CreateCommand("SELECT 1");
        await command.ExecuteScalarAsync(ct);
        return Results.Ok(new { status = "healthy", database = "up" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Health check failed");
        return Results.Json(
            new { status = "unhealthy", database = "down" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

public partial class Program;