using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ChartTrail.Api.Fhir;

public static class PatientEndpoints
{
    private const int DefaultCount = 20;
    private const int MaxCount = 100;

    public static void MapPatientEndpoints(this WebApplication app)
    {
        app.MapGet("/Patient", SearchAsync);
        app.MapGet("/Patient/{id}", ReadAsync);
    }

    private static async Task<IResult> SearchAsync(
        NpgsqlDataSource db,
        HttpRequest request,
        string? name,
        [FromQuery(Name = "_count")] int? count,
        CancellationToken ct)
    {
        var limit = Math.Clamp(count ?? DefaultCount, 1, MaxCount);
        var pattern = string.IsNullOrWhiteSpace(name) ? "%" : EscapeLike(name.Trim().ToLowerInvariant()) + "%";

        await using var countCommand = db.CreateCommand(
            "SELECT count(*) FROM patients WHERE lower(family_name) LIKE @p OR lower(given_name) LIKE @p");
        countCommand.Parameters.AddWithValue("p", pattern);
        var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(ct));

        await using var command = db.CreateCommand("""
            SELECT resource::text FROM patients
            WHERE lower(family_name) LIKE @p OR lower(given_name) LIKE @p
            ORDER BY family_name, given_name, id
            LIMIT @limit
            """);
        command.Parameters.AddWithValue("p", pattern);
        command.Parameters.AddWithValue("limit", limit);

        var baseUrl = $"{request.Scheme}://{request.Host}";
        var bundle = new Bundle { Type = Bundle.BundleType.Searchset, Total = total };

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var patient = FhirHttp.Parser.Parse<Patient>(reader.GetString(0));
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}/Patient/{patient.Id}",
                Resource = patient,
                Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
            });
        }

        return FhirHttp.Resource(bundle);
    }

    private static async Task<IResult> ReadAsync(NpgsqlDataSource db, string id, CancellationToken ct)
    {
        await using var command = db.CreateCommand("SELECT resource::text FROM patients WHERE id = @id");
        command.Parameters.AddWithValue("id", id);

        var json = await command.ExecuteScalarAsync(ct) as string;
        return json is null
            ? FhirHttp.Error(StatusCodes.Status404NotFound, OperationOutcome.IssueType.NotFound,
                $"Patient/{id} was not found")
            : FhirHttp.Resource(FhirHttp.Parser.Parse<Patient>(json));
    }

    // Default LIKE escape character is a backslash, so a user typing % or _ matches literally.
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}