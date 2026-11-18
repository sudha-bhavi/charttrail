using Task = System.Threading.Tasks.Task;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace ChartTrail.Api.Tests;

[Collection("api")]
public class SearchApiTests(ApiFixture api)
{
    private static readonly FhirJsonDeserializer Deserializer = new();

    private async Task<T> GetAsync<T>(string url) where T : Base
    {
        var response = await api.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Deserializer.Deserialize<T>(await response.Content.ReadAsStringAsync());
    }

    private static decimal[] Values(Bundle bundle) =>
        bundle.Entry.Select(e => ((Quantity)((Observation)e.Resource).Value).Value!.Value).ToArray();

    [Fact]
    public async Task Patient_search_matches_a_name_prefix_ignoring_case()
    {
        var byFamily = await GetAsync<Bundle>("/Patient?name=TES");
        var byGiven = await GetAsync<Bundle>("/Patient?name=ada");

        Assert.Equal(1, byFamily.Total);
        Assert.Equal("test-patient-1", byFamily.Entry.Single().Resource.Id);
        Assert.Equal(1, byGiven.Total);
    }

    [Fact]
    public async Task Patient_search_with_no_match_returns_an_empty_bundle()
    {
        var bundle = await GetAsync<Bundle>("/Patient?name=zzzz");

        Assert.Equal(0, bundle.Total);
        Assert.Empty(bundle.Entry);
    }

    [Fact]
    public async Task Patient_read_returns_the_stored_patient()
    {
        var patient = await GetAsync<Patient>("/Patient/test-patient-1");

        Assert.Equal("Testerson", patient.Name[0].Family);
        Assert.Equal("1985-04-12", patient.BirthDate);
    }

    [Fact]
    public async Task Patient_read_of_an_unknown_id_returns_404_with_an_operation_outcome()
    {
        var response = await api.Client.GetAsync("/Patient/does-not-exist");
        var outcome = Deserializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/fhir+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(OperationOutcome.IssueType.NotFound, outcome.Issue.Single().Code);
    }

    [Fact]
    public async Task Observation_search_filters_by_patient_and_code_and_sorts_by_date()
    {
        var oldestFirst = await GetAsync<Bundle>("/Observation?patient=test-patient-1&code=2339-0&_sort=date");
        var newestFirst = await GetAsync<Bundle>("/Observation?patient=test-patient-1&code=2339-0");

        Assert.Equal(2, oldestFirst.Total);
        Assert.Equal(new[] { 95m, 110m }, Values(oldestFirst));
        Assert.Equal(new[] { 110m, 95m }, Values(newestFirst));
    }

    [Fact]
    public async Task Observation_search_accepts_the_fhir_reference_and_token_forms()
    {
        var token = Uri.EscapeDataString("http://loinc.org|2339-0");

        var bundle = await GetAsync<Bundle>($"/Observation?patient=Patient/test-patient-1&code={token}");

        Assert.Equal(2, bundle.Total);
    }

    [Fact]
    public async Task Observation_search_for_another_code_finds_nothing()
    {
        var bundle = await GetAsync<Bundle>("/Observation?patient=test-patient-1&code=9999-9");

        Assert.Equal(0, bundle.Total);
    }

    [Fact]
    public async Task Observation_search_with_a_bad_sort_returns_400()
    {
        var response = await api.Client.GetAsync("/Observation?_sort=nonsense");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Metadata_returns_the_capability_statement()
    {
        var statement = await GetAsync<CapabilityStatement>("/metadata");

        Assert.Equal(FHIRVersion.N4_0_1, statement.FhirVersion);
    }

    [Fact]
    public async Task Health_reports_the_database_is_up()
    {
        var response = await api.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("healthy", await response.Content.ReadAsStringAsync());
    }
}