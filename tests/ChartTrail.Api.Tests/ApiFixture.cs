using ChartTrail.Api.Db;
using ChartTrail.Api.Import;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ChartTrail.Api.Tests;

public class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:17").Build();
    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var connectionString = _postgres.GetConnectionString();

        await using (var db = NpgsqlDataSource.Create(connectionString))
        {
            await SchemaInitializer.ApplyAsync(db);

            var folder = Directory.CreateTempSubdirectory("charttrail-tests").FullName;
            try
            {
                await File.WriteAllTextAsync(Path.Combine(folder, "test-patient.json"), TestBundle.Json);
                var result = await new BundleImporter(db, NullLogger<BundleImporter>.Instance)
                    .ImportFolderAsync(folder);
                if (result.Failed > 0 || result.Patients != 1)
                    throw new InvalidOperationException($"Test data did not import cleanly: {result}");
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        // Environment variables override user-secrets, so the API uses the test container.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);
        _factory = new WebApplicationFactory<Program>();
        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;