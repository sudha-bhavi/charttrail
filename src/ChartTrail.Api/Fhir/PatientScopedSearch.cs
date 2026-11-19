using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Npgsql;

namespace ChartTrail.Api.Fhir;

public static class PatientScopedSearch
{
    private const int DefaultCount = 100;
    private const int MaxCount = 1000;

    // table and dateColumn are fixed names passed by our own endpoints, never user input.
    public static async Task<IResult> RunAsync<T>(
        NpgsqlDataSource db, HttpRequest request, string table, string dateColumn,
        string? patient, string? sort, int? count, CancellationToken ct) where T : Resource
    {
        if (sort is not (null or "date" or "-date"))
            return FhirHttp.Error(StatusCodes.Status400BadRequest, OperationOutcome.IssueType.Invalid,
                "_sort must be 'date' or '-date'");

        var limit = Math.Clamp(count ?? DefaultCount, 1, MaxCount);
        var direction = sort == "date" ? "ASC" : "DESC";

        var hasPatient = !string.IsNullOrWhiteSpace(patient);
        var patientId = hasPatient ? patient!.Split('/').Last() : null;
        var where = hasPatient ? "WHERE patient_id = @patient" : "";

        await using var countCommand = db.CreateCommand($"SELECT count(*) FROM {table} {where}");
        if (hasPatient) countCommand.Parameters.AddWithValue("patient", patientId!);
        var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(ct));

        await using var command = db.CreateCommand($"""
            SELECT resource::text FROM {table}
            {where}
            ORDER BY {dateColumn} {direction} NULLS LAST, id
            LIMIT @limit
            """);
        if (hasPatient) command.Parameters.AddWithValue("patient", patientId!);
        command.Parameters.AddWithValue("limit", limit);

        var resources = new List<Resource>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            resources.Add(FhirHttp.Parser.Deserialize<T>(reader.GetString(0)));

        var baseUrl = $"{request.Scheme}://{request.Host}";
        return FhirHttp.Resource(FhirHttp.SearchSet(resources, total, baseUrl));
    }
}