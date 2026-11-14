using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace ChartTrail.Api.Import;

public record PatientRow(string Id, string? FamilyName, string? GivenName, string? Gender, DateOnly? BirthDate);
public record EncounterRow(string Id, string PatientId, string? ClassCode, string? TypeDisplay, DateTimeOffset? PeriodStart, DateTimeOffset? PeriodEnd);
public record MedicationRequestRow(string Id, string PatientId, string? Code, string? Display, string? Status, DateTimeOffset? AuthoredOn);
public record ObservationRow(string Id, string PatientId, string? Category, string? Code, string? Display,
    decimal? ValueNumber, string? ValueUnit, string? ValueText, DateTimeOffset? EffectiveAt);

public static class ResourceMapper
{
    public static PatientRow Map(Patient p)
    {
        var name = p.Name.FirstOrDefault();
        var given = name is not null && name.Given.Any() ? string.Join(' ', name.Given) : null;
        return new PatientRow(p.Id, name?.Family, given, p.Gender?.GetLiteral(),
            DateOnly.TryParse(p.BirthDate, out var birth) ? birth : null);
    }

    public static EncounterRow Map(Encounter e) => new(
        e.Id,
        PatientIdOf(e.Subject, "Encounter", e.Id),
        e.Class?.Code,
        e.Type.FirstOrDefault()?.Coding.FirstOrDefault()?.Display ?? e.Type.FirstOrDefault()?.Text,
        ToUtc(e.Period?.StartElement),
        ToUtc(e.Period?.EndElement));

    public static MedicationRequestRow Map(MedicationRequest m)
    {
        var concept = m.Medication as CodeableConcept;
        return new MedicationRequestRow(
            m.Id,
            PatientIdOf(m.Subject, "MedicationRequest", m.Id),
            concept?.Coding.FirstOrDefault()?.Code,
            concept?.Coding.FirstOrDefault()?.Display ?? concept?.Text,
            m.Status?.GetLiteral(),
            ToUtc(m.AuthoredOnElement));
    }

    public static ObservationRow Map(Observation o)
    {
        decimal? number = null;
        string? unit = null;
        string? text = null;
        switch (o.Value)
        {
            case Quantity q:
                number = q.Value;
                unit = q.Unit ?? q.Code;
                break;
            case CodeableConcept cc:
                text = cc.Text ?? cc.Coding.FirstOrDefault()?.Display;
                break;
            case FhirString s:
                text = s.Value;
                break;
        }

        var effective = o.Effective switch
        {
            FhirDateTime d => ToUtc(d),
            Period p => ToUtc(p.StartElement),
            _ => null
        };

        return new ObservationRow(
            o.Id,
            PatientIdOf(o.Subject, "Observation", o.Id),
            o.Category.FirstOrDefault()?.Coding.FirstOrDefault()?.Code,
            o.Code?.Coding.FirstOrDefault()?.Code,
            o.Code?.Coding.FirstOrDefault()?.Display ?? o.Code?.Text,
            number, unit, text, effective);
    }

    // Synthea references look like "urn:uuid:<id>"; standard FHIR uses "Patient/<id>".
    public static string? IdFromReference(ResourceReference? reference)
    {
        var text = reference?.Reference;
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (text.StartsWith("urn:uuid:")) return text["urn:uuid:".Length..];
        var slash = text.LastIndexOf('/');
        return slash >= 0 ? text[(slash + 1)..] : text;
    }

    private static string PatientIdOf(ResourceReference? subject, string type, string id) =>
        IdFromReference(subject)
        ?? throw new InvalidOperationException($"{type} {id} has no subject reference");

    // Npgsql only accepts UTC offsets for timestamptz, so every time is converted to UTC.
    private static DateTimeOffset? ToUtc(FhirDateTime? value) =>
        value is null ? null : value.ToDateTimeOffset(TimeSpan.Zero).ToUniversalTime();
}