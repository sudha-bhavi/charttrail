using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Task = System.Threading.Tasks.Task;

namespace ChartTrail.Api.Tests;

[Collection("api")]
public class TimelineApiTests(ApiFixture api)
{
    private static readonly FhirJsonDeserializer Deserializer = new();

    private async Task<Bundle> GetBundleAsync(string url)
    {
        var response = await api.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Deserializer.Deserialize<Bundle>(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Encounter_search_returns_the_patients_visits()
    {
        var bundle = await GetBundleAsync("/Encounter?patient=test-patient-1");

        Assert.Equal(1, bundle.Total);
        var visit = Assert.IsType<Encounter>(bundle.Entry.Single().Resource);
        Assert.Equal("test-enc-1", visit.Id);
        Assert.Equal("Well child visit", visit.Type.Single().Coding.Single().Display);
    }

    [Fact]
    public async Task MedicationRequest_search_returns_the_patients_medications()
    {
        var bundle = await GetBundleAsync("/MedicationRequest?patient=Patient/test-patient-1&_sort=date");

        Assert.Equal(1, bundle.Total);
        var medication = Assert.IsType<MedicationRequest>(bundle.Entry.Single().Resource);
        var concept = Assert.IsType<CodeableConcept>(medication.Medication);
        Assert.Equal("Amlodipine 5 MG Oral Tablet", concept.Coding.Single().Display);
    }

    [Fact]
    public async Task Timeline_searches_for_an_unknown_patient_return_nothing()
    {
        Assert.Equal(0, (await GetBundleAsync("/Encounter?patient=nobody")).Total);
        Assert.Equal(0, (await GetBundleAsync("/MedicationRequest?patient=nobody")).Total);
    }

    [Fact]
    public async Task Timeline_searches_reject_a_bad_sort()
    {
        var response = await api.Client.GetAsync("/Encounter?_sort=nonsense");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Metadata_lists_the_timeline_resources()
    {
        var response = await api.Client.GetAsync("/metadata");
        var statement = Deserializer.Deserialize<CapabilityStatement>(await response.Content.ReadAsStringAsync());

        var types = statement.Rest.Single().Resource.Select(r => r.Type).ToList();
        Assert.Contains("Encounter", types);
        Assert.Contains("MedicationRequest", types);
    }
}