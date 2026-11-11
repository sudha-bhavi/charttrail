using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace ChartTrail.Api.Tests;

public class PatientParsingTests
{
    // Synthetic patient, written by hand. No real person.
    private const string PatientJson = """
        {
          "resourceType": "Patient",
          "id": "test-patient-1",
          "name": [ { "family": "Testerson", "given": [ "Ada" ] } ],
          "gender": "female",
          "birthDate": "1985-04-12"
        }
        """;

    [Fact]
    public void Parses_patient_json_into_typed_resource()
    {
        var parser = new FhirJsonParser();

        var patient = parser.Parse<Patient>(PatientJson);

        Assert.Equal("test-patient-1", patient.Id);
        Assert.Equal("Testerson", patient.Name[0].Family);
        Assert.Equal("Ada", patient.Name[0].Given.First());
        Assert.Equal(AdministrativeGender.Female, patient.Gender);
        Assert.Equal("1985-04-12", patient.BirthDate);
    }
}