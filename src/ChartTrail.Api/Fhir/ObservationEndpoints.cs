using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ChartTrail.Api.Fhir;

public static class ObservationEndpoints
{
    private const int DefaultCount = 100;
    private const int MaxCount = 1000;

    public static void MapObservationEndpoints(this WebApplication app) =>
        app.MapGet("/Observation", SearchAsync);

    private static async Task<IResult> SearchAsync(
        NpgsqlDataSource db,
        HttpRequest request,
        string? patient,
        string? code,
        string? category,
        [FromQuery(Name = "_sort")] string? sort,
        [FromQuery(Name = "_count")] int? count,
        CancellationToken ct)
    {
        if (sort is not (null or "date" or "-date"))
            return FhirHttp.Error(StatusCodes.Status400BadRequest, OperationOutcome.IssueType.Invalid,
                "_sort must be 'date' or '-date'");

        var limit = Math.Clamp(count ?? DefaultCount, 1, MaxCount);
        var direction = sort == "date" ? "ASC" : "DESC";

        // Only fixed SQL fragments are concatenated; every value goes in as a parameter.
        var filters = new List<(string Sql, string Name, string Value)>();
        if (!string.IsNullOrWhiteSpace(patient))
            filters.Add(("patient_id = @patient", "patient", patient.Split('/').Last()));
        if (!string.IsNullOrWhiteSpace(code))
            filters.Add(("code = @code", "code", code.Split('|').Last()));
        if (!string.IsNullOrWhiteSpace(category))
            filters.Add(("category = @category", "category", category));

        var where = filters.Count == 0 ? "" : "WHERE " + string.Join(" AND ", filters.Select(f => f.Sql));

        await using var countCommand = db.CreateCommand($"SELECT count(*) FROM observations {where}");
        foreach (var f in filters) countCommand.Parameters.AddWithValue(f.Name, f.Value);
        var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(ct));

        await using var command = db.CreateCommand($"""
            SELECT resource::text FROM observations
            {where}
            ORDER BY effective_at {direction} NULLS LAST, id
            LIMIT @limit
            """);
        foreach (var f in filters) command.Parameters.AddWithValue(f.Name, f.Value);
        command.Parameters.AddWithValue("limit", limit);

        var observations = new List<Resource>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            observations.Add(FhirHttp.Parser.Deserialize<Observation>(reader.GetString(0)));

        var baseUrl = $"{request.Scheme}://{request.Host}";
        return FhirHttp.Resource(FhirHttp.SearchSet(observations, total, baseUrl));
    }
}