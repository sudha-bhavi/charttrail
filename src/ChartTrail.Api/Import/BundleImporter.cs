using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Npgsql;
using NpgsqlTypes;

namespace ChartTrail.Api.Import;

public record ImportResult(int Files, int Patients, int Encounters, int MedicationRequests, int Observations, int Failed);

public class BundleImporter(NpgsqlDataSource db, ILogger<BundleImporter> logger)
{
    private static readonly FhirJsonParser Parser =
        new(new ParserSettings { AcceptUnknownMembers = true, AllowUnrecognizedEnums = true });
    private static readonly FhirJsonSerializer Serializer = new();

    public async Task<ImportResult> ImportFolderAsync(string folder, CancellationToken ct = default)
    {
        var files = Directory.EnumerateFiles(folder, "*.json")
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return !name.StartsWith("hospitalInformation") && !name.StartsWith("practitionerInformation");
            })
            .Order()
            .ToList();

        int done = 0, patients = 0, encounters = 0, meds = 0, observations = 0, failed = 0;

        foreach (var file in files)
        {
            try
            {
                var (p, e, m, o) = await ImportFileAsync(file, ct);
                patients += p; encounters += e; meds += m; observations += o;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                logger.LogError(ex, "Failed to import {File}", Path.GetFileName(file));
            }

            done++;
            if (done % 25 == 0) Console.WriteLine($"Imported {done}/{files.Count} files...");
        }

        return new ImportResult(files.Count, patients, encounters, meds, observations, failed);
    }

    private async Task<(int Patients, int Encounters, int Meds, int Observations)> ImportFileAsync(
        string path, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        var bundle = Parser.Parse<Bundle>(json);
        var resources = bundle.Entry.Select(e => e.Resource).Where(r => r?.Id is not null).ToList();

        var patient = resources.OfType<Patient>().Single();
        var encounters = resources.OfType<Encounter>().ToList();
        var meds = resources.OfType<MedicationRequest>().ToList();
        var observations = resources.OfType<Observation>().ToList();

        await using var conn = await db.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await using var batch = new NpgsqlBatch(conn, tx);

        var pr = ResourceMapper.Map(patient);
        batch.BatchCommands.Add(Cmd("""
            INSERT INTO patients (id, family_name, given_name, gender, birth_date, resource)
            VALUES (@id, @family, @given, @gender, @birth, @resource)
            ON CONFLICT (id) DO UPDATE SET family_name = EXCLUDED.family_name,
                given_name = EXCLUDED.given_name, gender = EXCLUDED.gender,
                birth_date = EXCLUDED.birth_date, resource = EXCLUDED.resource
            """,
            P("id", pr.Id), P("family", pr.FamilyName), P("given", pr.GivenName),
            P("gender", pr.Gender), P("birth", pr.BirthDate), Json(patient)));

        foreach (var e in encounters)
        {
            var r = ResourceMapper.Map(e);
            batch.BatchCommands.Add(Cmd("""
                INSERT INTO encounters (id, patient_id, class_code, type_display, period_start, period_end, resource)
                VALUES (@id, @patient, @class, @type, @start, @end, @resource)
                ON CONFLICT (id) DO UPDATE SET patient_id = EXCLUDED.patient_id, class_code = EXCLUDED.class_code,
                    type_display = EXCLUDED.type_display, period_start = EXCLUDED.period_start,
                    period_end = EXCLUDED.period_end, resource = EXCLUDED.resource
                """,
                P("id", r.Id), P("patient", r.PatientId), P("class", r.ClassCode), P("type", r.TypeDisplay),
                P("start", r.PeriodStart), P("end", r.PeriodEnd), Json(e)));
        }

        foreach (var m in meds)
        {
            var r = ResourceMapper.Map(m);
            batch.BatchCommands.Add(Cmd("""
                INSERT INTO medication_requests (id, patient_id, code, display, status, authored_on, resource)
                VALUES (@id, @patient, @code, @display, @status, @authored, @resource)
                ON CONFLICT (id) DO UPDATE SET patient_id = EXCLUDED.patient_id, code = EXCLUDED.code,
                    display = EXCLUDED.display, status = EXCLUDED.status,
                    authored_on = EXCLUDED.authored_on, resource = EXCLUDED.resource
                """,
                P("id", r.Id), P("patient", r.PatientId), P("code", r.Code), P("display", r.Display),
                P("status", r.Status), P("authored", r.AuthoredOn), Json(m)));
        }

        foreach (var o in observations)
        {
            var r = ResourceMapper.Map(o);
            batch.BatchCommands.Add(Cmd("""
                INSERT INTO observations (id, patient_id, category, code, display, value_number, value_unit, value_text, effective_at, resource)
                VALUES (@id, @patient, @category, @code, @display, @number, @unit, @text, @effective, @resource)
                ON CONFLICT (id) DO UPDATE SET patient_id = EXCLUDED.patient_id, category = EXCLUDED.category,
                    code = EXCLUDED.code, display = EXCLUDED.display, value_number = EXCLUDED.value_number,
                    value_unit = EXCLUDED.value_unit, value_text = EXCLUDED.value_text,
                    effective_at = EXCLUDED.effective_at, resource = EXCLUDED.resource
                """,
                P("id", r.Id), P("patient", r.PatientId), P("category", r.Category), P("code", r.Code),
                P("display", r.Display), P("number", r.ValueNumber), P("unit", r.ValueUnit),
                P("text", r.ValueText), P("effective", r.EffectiveAt), Json(o)));
        }

        await batch.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);

        return (1, encounters.Count, meds.Count, observations.Count);
    }

    private static NpgsqlBatchCommand Cmd(string sql, params NpgsqlParameter[] parameters)
    {
        var command = new NpgsqlBatchCommand(sql);
        foreach (var p in parameters) command.Parameters.Add(p);
        return command;
    }

    private static NpgsqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);

    private static NpgsqlParameter Json(Resource resource) =>
        new("resource", NpgsqlDbType.Jsonb) { Value = Serializer.SerializeToString(resource) };
}