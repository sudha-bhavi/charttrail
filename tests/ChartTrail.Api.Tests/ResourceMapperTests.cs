using ChartTrail.Api.Import;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace ChartTrail.Api.Tests;

public class ResourceMapperTests
{
    private static readonly FhirJsonParser Parser = new();

    [Fact]
    public void Maps_a_numeric_lab_observation()
    {
        var obs = Parser.Parse<Observation>("""
            {
              "resourceType": "Observation",
              "id": "obs-1",
              "status": "final",
              "category": [ { "coding": [ { "code": "laboratory" } ] } ],
              "code": { "coding": [ { "system": "http://loinc.org", "code": "2339-0", "display": "Glucose" } ] },
              "subject": { "reference": "urn:uuid:patient-1" },
              "effectiveDateTime": "2020-03-01T10:30:00-05:00",
              "valueQuantity": { "value": 95.5, "unit": "mg/dL" }
            }
            """);

        var row = ResourceMapper.Map(obs);

        Assert.Equal("patient-1", row.PatientId);
        Assert.Equal("laboratory", row.Category);
        Assert.Equal("2339-0", row.Code);
        Assert.Equal(95.5m, row.ValueNumber);
        Assert.Equal("mg/dL", row.ValueUnit);
        Assert.Equal(new DateTimeOffset(2020, 3, 1, 15, 30, 0, TimeSpan.Zero), row.EffectiveAt);
    }

    [Fact]
    public void Maps_a_text_observation_with_a_standard_patient_reference()
    {
        var obs = Parser.Parse<Observation>("""
            {
              "resourceType": "Observation",
              "id": "obs-2",
              "status": "final",
              "code": { "coding": [ { "code": "72166-2", "display": "Tobacco smoking status" } ] },
              "subject": { "reference": "Patient/patient-2" },
              "valueCodeableConcept": { "text": "Never smoker" }
            }
            """);

        var row = ResourceMapper.Map(obs);

        Assert.Equal("patient-2", row.PatientId);
        Assert.Null(row.ValueNumber);
        Assert.Equal("Never smoker", row.ValueText);
        Assert.Null(row.EffectiveAt);
    }

    [Fact]
    public void Maps_patient_name_gender_and_birth_date()
    {
        var patient = Parser.Parse<Patient>("""
            {
              "resourceType": "Patient",
              "id": "patient-3",
              "name": [ { "family": "Testerson", "given": [ "Ada" ] } ],
              "gender": "female",
              "birthDate": "1985-04-12"
            }
            """);

        var row = ResourceMapper.Map(patient);

        Assert.Equal("Testerson", row.FamilyName);
        Assert.Equal("Ada", row.GivenName);
        Assert.Equal("female", row.Gender);
        Assert.Equal(new DateOnly(1985, 4, 12), row.BirthDate);
    }

    [Fact]
    public void Throws_when_an_observation_has_no_subject()
    {
        var obs = Parser.Parse<Observation>("""
            { "resourceType": "Observation", "id": "obs-3", "status": "final", "code": { "text": "x" } }
            """);

        Assert.Throws<InvalidOperationException>(() => ResourceMapper.Map(obs));
    }
}