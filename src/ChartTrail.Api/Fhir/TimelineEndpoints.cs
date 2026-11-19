using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ChartTrail.Api.Fhir;

public static class TimelineEndpoints
{
    public static void MapTimelineEndpoints(this WebApplication app)
    {
        app.MapGet("/Encounter", EncountersAsync);
        app.MapGet("/MedicationRequest", MedicationRequestsAsync);
    }

    private static Task<IResult> EncountersAsync(
        NpgsqlDataSource db, HttpRequest request, string? patient,
        [FromQuery(Name = "_sort")] string? sort, [FromQuery(Name = "_count")] int? count,
        CancellationToken ct) =>
        PatientScopedSearch.RunAsync<Encounter>(db, request, "encounters", "period_start", patient, sort, count, ct);

    private static Task<IResult> MedicationRequestsAsync(
        NpgsqlDataSource db, HttpRequest request, string? patient,
        [FromQuery(Name = "_sort")] string? sort, [FromQuery(Name = "_count")] int? count,
        CancellationToken ct) =>
        PatientScopedSearch.RunAsync<MedicationRequest>(db, request, "medication_requests", "authored_on", patient, sort, count, ct);
}