using ChartTrail.Api.Fhir;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace ChartTrail.Api.Tests;

public class ServerCapabilitiesTests
{
    [Fact]
    public void Declares_fhir_r4_and_the_supported_resources()
    {
        var statement = ServerCapabilities.Build();

        Assert.Equal(FHIRVersion.N4_0_1, statement.FhirVersion);

        var resources = statement.Rest.Single().Resource;
        Assert.Contains(resources, r => r.Type == "Patient");
        Assert.Contains(resources, r => r.Type == "Observation");
    }

    [Fact]
    public void Observation_advertises_the_patient_and_code_search_parameters()
    {
        var observation = ServerCapabilities.Build().Rest.Single().Resource
            .Single(r => r.Type == "Observation");

        var names = observation.SearchParam.Select(p => p.Name).ToList();
        Assert.Contains("patient", names);
        Assert.Contains("code", names);
    }

    [Fact]
    public void Serializes_to_json_that_parses_back_as_a_capability_statement()
    {
        var json = new FhirJsonSerializer().SerializeToString(ServerCapabilities.Build());

        var parsed = new FhirJsonDeserializer().Deserialize<CapabilityStatement>(json);

        Assert.Equal("ChartTrail", parsed.Software.Name);
    }
}